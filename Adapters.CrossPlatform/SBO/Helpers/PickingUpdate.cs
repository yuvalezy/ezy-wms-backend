using System.Data;
using System.Text.Json.Serialization;
using Adapters.Common.SBO.Services;
using Adapters.CrossPlatform.SBO.Models;
using Adapters.CrossPlatform.SBO.Services;
using Core.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class PickingUpdate(
    int absEntry,
    List<PickList> data,
    SboCompany sboCompany,
    SboDatabaseService databaseService,
    ILoggerFactory loggerFactory) : IDisposable {
    private readonly ILogger<PickingUpdate> logger = loggerFactory.CreateLogger<PickingUpdate>();
    private PickListSboResponse? pickListResponse;

    public async Task Execute() {
        try {
            logger.LogInformation("Starting pick list update execution for AbsEntry {AbsEntry}", absEntry);
            await LoadPickList();
            await PreparePickList();
            await ProcessPickList();

            logger.LogInformation("Successfully completed pick list update for AbsEntry {AbsEntry}", absEntry);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to execute pick list update for AbsEntry {AbsEntry}", absEntry);
            throw;
        }
    }

    private async Task LoadPickList() {
        pickListResponse = await sboCompany.GetAsync<PickListSboResponse>($"PickLists({absEntry})");
        if (pickListResponse == null) {
            logger.LogError("Could not find Pick List {AbsEntry}", absEntry);
            throw new Exception($"Could not find Pick List {absEntry}");
        }

        if (pickListResponse.Status == "ps_Closed") {
            logger.LogWarning("Cannot process pick list {AbsEntry} because status is closed", absEntry);
            throw new Exception("Cannot process document if the Status is closed");
        }
    }
    // private async Task PreparePickList() {
    //     if (pickListResponse.PickListsLines.All(v => v.PickedQuantity == 0)) {
    //         foreach (var line in pickListResponse.PickListsLines) {
    //             line.DocumentLinesBinAllocations = [];
    //         }
    //     }
    //
    //     (bool success, string? errorMessage) = await sboCompany.PostAsync("PickListsService_UpdateReleasedAllocation", new {
    //         PickList = pickListResponse,
    //     });
    //
    //     if (!success) {
    //         logger.LogError("Failed to prepare pick list {AbsEntry}: {ErrorMessage}", absEntry, errorMessage);
    //         throw new Exception($"Failed to prepare pick list: {errorMessage}");
    //     }
    // }
    //

    private async Task PreparePickList() {
        if (pickListResponse.PickListsLines.Any(v => v.PickedQuantity > 0)) {
            return;
        }

        // Clear all bin allocations first
        foreach (var line in pickListResponse.PickListsLines) {
            line.DocumentLinesBinAllocations = [];
        }

        (bool success, string? errorMessage) = await sboCompany.PostAsync("PickListsService_UpdateReleasedAllocation", new {
            PickList = pickListResponse,
        });

        if (!success) {
            logger.LogError("Failed to prepare pick list {AbsEntry}: {ErrorMessage}", absEntry, errorMessage);
            throw new Exception($"Failed to prepare pick list: {errorMessage}");
        }
    }

    private record SourceMeasureData(int DocEntry, int LineNum, int ObjType, int NumPerMeasure);

    private async Task<SourceMeasureData[]> GetSourceMeasureData() {
        const string query =
        """
        select T0."OrderEntry", T0."OrderLine", T0."BaseObject", COALESCE(T1."NumPerMsr", T2."NumPerMsr", T3."NumPerMsr") "NumPerMsr"
        from PKL1 T0
                 left outer join RDR1 T1 on T1."DocEntry" = T0."OrderEntry" and T1."ObjType" = T0."BaseObject" and T1."LineNum" = T0."OrderLine"
                 left outer join PCH1 T2 on T2."DocEntry" = T0."OrderEntry" and T2."ObjType" = T0."BaseObject" and T2."LineNum" = T0."OrderLine"
                 left outer join WTQ1 T3 on T3."DocEntry" = T0."OrderEntry" and T3."ObjType" = T0."BaseObject" and T3."LineNum" = T0."OrderLine"
        where T0."AbsEntry" = @AbsEntry
        """;

        var sourceData = await databaseService.QueryAsync(query, [new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = absEntry }], reader => new SourceMeasureData(
            (int)reader["OrderEntry"],
            (int)reader["OrderLine"],
            Convert.ToInt32(reader["BaseObject"]),
            Convert.ToInt32(reader["NumPerMsr"])
        ));

        return sourceData.ToArray();
    }

    private async Task ProcessPickList() {
        // Group data by pick entry
        var lines = data.GroupBy(v => v.PickEntry)
        .Select(a => new {
            PickEntry = a.Key,
            Quantity = a.Sum(b => b.Quantity),
            Bins = a.GroupBy(b => b.BinEntry)
            .Select(c => new { BinEntry = c.Key, Quantity = c.Sum(d => d.Quantity) })
            .ToList()
        }).ToList();

        bool isBin = pickListResponse.PickListsLines.Any(v => v.DocumentLinesBinAllocations.Count > 0);
        var sourceData = !isBin ? await GetSourceMeasureData() : [];

        foreach (var pickLine in pickListResponse.PickListsLines) {
            var line = lines.FirstOrDefault(v => v.PickEntry == pickLine.LineNumber);
            if (line == null) {
                continue;
            }

            double pickedQuantity = line.Quantity;
            logger.LogDebug("Processing pick line {LineNumber} with quantity {Quantity} for pick list {AbsEntry}", pickLine.LineNumber, pickedQuantity, absEntry);

            int measureUnit = isBin
            ? 1
            : sourceData.FirstOrDefault(v => v.DocEntry == pickLine.OrderEntry && v.LineNum == pickLine.OrderRowID && v.ObjType == pickLine.BaseObjectType)?.NumPerMeasure ??
              throw new Exception($"Num per measure not found for pick entry {pickLine.LineNumber} in pick {absEntry}");

            pickLine.PreviouslyReleasedQuantity = pickLine.ReleasedQuantity;
            if (pickLine.PickedQuantity == 0) {
                pickLine.ReleasedQuantity = pickedQuantity;
                pickLine.PickedQuantity = pickedQuantity / measureUnit;
            }
            else {
                pickLine.ReleasedQuantity += pickedQuantity;
                pickLine.PickedQuantity += pickedQuantity / measureUnit;
            }

            pickLine.PickStatus = pickLine.PickedQuantity == 0   ? "ps_Released" :
            pickLine.PickedQuantity == pickLine.ReleasedQuantity ? "ps_Picked" :
                                                                   "ps_PartiallyPicked";

            logger.LogDebug("Processing {BinCount} bin allocations for pick line {LineNumber}", line.Bins.Count, pickLine.LineNumber);

            foreach (var bin in line.Bins) {
                if (bin.BinEntry == null) {
                    continue;
                }

                bool found = false;
                foreach (var allocation in pickLine.DocumentLinesBinAllocations) {
                    if (allocation.BinAbsEntry == bin.BinEntry) {
                        allocation.Quantity += bin.Quantity;
                        found = true;
                        logger.LogDebug("Updated existing bin allocation for BinEntry {BinEntry} with quantity {Quantity}", bin.BinEntry, bin.Quantity);

                        break;
                    }
                }

                if (!found) {
                    pickLine.DocumentLinesBinAllocations.Add(new PickListLineSboBinAllocation {
                        BaseLineNumber = pickLine.LineNumber,
                        BinAbsEntry = bin.BinEntry!.Value,
                        Quantity = bin.Quantity,
                    });

                    logger.LogDebug("Added new bin allocation for BinEntry {BinEntry} with quantity {Quantity}",
                        bin.BinEntry, bin.Quantity);
                }
            }
        }

        (bool success, string? errorMessage) = await sboCompany.PutAsync($"PickLists({absEntry})", pickListResponse);

        if (!success) {
            logger.LogError("Could not update Pick List {AbsEntry}: {ErrorMessage}", absEntry, errorMessage);
            throw new Exception($"Could not update Pick List: {errorMessage}");
        }
    }

    public void Dispose() {
    }
}