using System.Data;
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
    private PickListSboResponse pickListResponse = null!;

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
        var response = await sboCompany.GetAsync<PickListSboResponse>($"PickLists({absEntry})");
        if (response == null) {
            logger.LogError("Could not find Pick List {AbsEntry}", absEntry);
            throw new Exception($"Could not find Pick List {absEntry}");
        }

        if (response.Status == "ps_Closed") {
            logger.LogWarning("Cannot process pick list {AbsEntry} because status is closed", absEntry);
            throw new Exception("Cannot process document if the Status is closed");
        }

        pickListResponse = response;
    }

    private async Task PreparePickList() {
        if (pickListResponse.Ready == "Y") {
            return;
        }

        pickListResponse.Ready = "Y";

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

    private record SourceMeasureData(int DocEntry, int LineNum, int ObjType, int NumPerMeasure, double Quantity);

    private async Task<SourceMeasureData[]> GetSourceMeasureData() {
        const string query =
        """
        select T0."OrderEntry", T0."OrderLine", T0."BaseObject", COALESCE(T1."NumPerMsr", T2."NumPerMsr", T3."NumPerMsr") "NumPerMsr", COALESCE(T1."InvQty", T2."InvQty", T3."InvQty") "InvQty"
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
            Convert.ToInt32(reader["NumPerMsr"]),
            Convert.ToDouble(reader["InvQty"])
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

        var sourceData = await GetSourceMeasureData();

        foreach (var pickLine in pickListResponse.PickListsLines) {
            var line = lines.FirstOrDefault(v => v.PickEntry == pickLine.LineNumber);
            if (line == null) {
                continue;
            }

            double pickedQuantity = line.Quantity;
            logger.LogDebug("Processing pick line {LineNumber} with quantity {Quantity} for pick list {AbsEntry}", pickLine.LineNumber, pickedQuantity, absEntry);

            var sourceLine = sourceData.FirstOrDefault(v => v.DocEntry == pickLine.OrderEntry && v.LineNum == pickLine.OrderRowID && v.ObjType == pickLine.BaseObjectType);
            if (sourceLine == null) {
                throw new Exception($"Source measure data not found for pick entry {pickLine.LineNumber} in pick {absEntry}");
            }

            // Set what we are actually picking
            pickLine.PickedQuantity = pickedQuantity;
            // Increase the released quantity with what we're picking
            pickLine.ReleasedQuantity += pickedQuantity;
            
            // Update status although technically irrelevant
            if (pickLine.PickedQuantity == 0) {
                pickLine.PickStatus = "ps_Released";
            }
            else if (pickLine.PickedQuantity == sourceLine.Quantity) {
                pickLine.PickStatus = "ps_Picked";
            }
            else {
                pickLine.PickStatus = "ps_PartiallyPicked";
            }

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

                    logger.LogDebug("Added new bin allocation for BinEntry {BinEntry} with quantity {Quantity}", bin.BinEntry, bin.Quantity);
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