using System.Data;
using System.Text.Json.Serialization;
using Adapters.CrossPlatform.SBO.Services;
using Core.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class PickingUpdate(
    int                absEntry,
    List<PickList>     data,
    SboDatabaseService dbService,
    SboCompany         sboCompany,
    string?            filtersPickReady,
    ILoggerFactory     loggerFactory) : IDisposable {
    private readonly ILogger<PickingUpdate>                                             logger         = loggerFactory.CreateLogger<PickingUpdate>();

    public async Task Execute() {
        logger.LogInformation("Starting pick list update for AbsEntry {AbsEntry} with {DataCount} pick entries",
            absEntry, data.Count);

        try {
            // await PreparePickList();
            await ProcessPickList();

            logger.LogInformation("Successfully completed pick list update for AbsEntry {AbsEntry}", absEntry);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to update pick list {AbsEntry}", absEntry);
            throw;
        }
    }

    // private async Task PreparePickList() {
    //     logger.LogDebug("Preparing pick list {AbsEntry}", absEntry);
    //     
    //     // Get current pick list
    //     var pickList = await sboCompany.GetAsync<PickListResponse>($"PickLists({absEntry})");
    //     if (pickList == null) {
    //         throw new Exception($"Could not find Pick List {absEntry}");
    //     }
    //     
    //     if (pickList.Status == "ps_Closed") {
    //         throw new Exception("Cannot process document if the Status is closed");
    //     }
    //     
    //     logger.LogDebug("Pick list {AbsEntry} found with status {Status}, {LineCount} lines", 
    //         absEntry, pickList.Status, pickList.PickListsLines?.Length ?? 0);
    //
    //     // Check and set filter if needed
    //     if (!string.IsNullOrWhiteSpace(filtersPickReady)) {
    //         logger.LogDebug("Checking filter {FilterName} for pick list {AbsEntry}", filtersPickReady, absEntry);
    //         
    //         // Note: Service Layer user-defined fields would need to be handled through UserFields
    //         // This is a simplified version - may need adjustment based on actual UDF structure
    //     }
    //
    //     // Clear all bin allocations first
    //     var prepareData = new {
    //         PickListsLines = pickList.PickListsLines?.Select(line => new {
    //             LineNumber                  = line.LineNumber,
    //             DocumentLinesBinAllocations = Array.Empty<object>() // Clear all bin allocations
    //         }).ToArray()
    //     };
    //
    //     // Set filter if specified
    //     if (!string.IsNullOrWhiteSpace(filtersPickReady)) {
    //         // This would need to be adjusted based on actual UDF structure in Service Layer
    //         logger.LogDebug("Setting filter {FilterName} to Y for pick list {AbsEntry}", filtersPickReady, absEntry);
    //     }
    //
    //     logger.LogDebug("Clearing bin allocations and updating pick list {AbsEntry}", absEntry);
    //     var (success, errorMessage) = await sboCompany.PatchAsync($"PickLists({absEntry})", prepareData);
    //     
    //     if (!success) {
    //         throw new Exception($"Failed to prepare pick list: {errorMessage}");
    //     }
    //     
    //     logger.LogDebug("Pick list {AbsEntry} prepared successfully", absEntry);
    // }

    private async Task ProcessPickList() {
        logger.LogDebug("Processing pick list {AbsEntry} with {DataCount} entries", absEntry, data.Count);

        // Group data by pick entry
        var lines = data.GroupBy(v => v.PickEntry)
            .Select(a => new {
                PickEntry = a.Key,
                Quantity  = a.Sum(b => b.Quantity),
                Bins = a.GroupBy(b => b.BinEntry)
                    .Select(c => new { BinEntry = c.Key, Quantity = c.Sum(d => d.Quantity) })
                    .ToList()
            }).ToList();

        logger.LogDebug("Grouped data into {LineCount} pick lines", lines.Count);

        // Get current pick list structure
        var pickList = await sboCompany.GetAsync<PickListResponse>($"PickLists({absEntry})");
        if (pickList?.PickListsLines == null) {
            throw new Exception($"Could not retrieve pick list lines for {absEntry}");
        }

        var updatedLines = new List<object>();

        foreach (var pickLine in pickList.PickListsLines) {
            var matchingData = lines.FirstOrDefault(v => v.PickEntry == pickLine.LineNumber);

            if (matchingData == null) {
                // Keep original line without changes
                updatedLines.Add(new {
                    LineNumber                  = pickLine.LineNumber,
                    // ReleasedQuantity            = pickLine.PickedQuantity,
                    DocumentLinesBinAllocations = pickLine.DocumentLinesBinAllocations ?? []
                });
                continue;
            }

            double pickedQuantity = matchingData.Quantity;

            // Create bin allocations
            var binAllocations = matchingData.Bins.Select(bin => new {
                BinAbsEntry = bin.BinEntry,
                Quantity    = (double)bin.Quantity
            }).ToArray();

            updatedLines.Add(new {
                LineNumber                  = pickLine.LineNumber,
                // ReleasedQuantity            = pickedQuantity,
                DocumentLinesBinAllocations = binAllocations
            });

            logger.LogDebug("Updated pick line {LineNumber}: PickedQuantity={PickedQuantity}, BinAllocations={BinCount}",
                pickLine.LineNumber, pickedQuantity, binAllocations.Length);
        }

        var updateData = new {
            PickListsLines = updatedLines.ToArray()
        };

        logger.LogInformation("Updating pick list {AbsEntry} with {LineCount} lines", absEntry, updatedLines.Count);
        var (success, errorMessage) = await sboCompany.PatchAsync($"PickLists({absEntry})", updateData);

        if (!success) {
            throw new Exception($"Could not update Pick List: {errorMessage}");
        }

        logger.LogInformation("Pick list {AbsEntry} updated successfully", absEntry);
    }

    public void Dispose() {
        logger.LogDebug("Disposing PickingUpdate resources for AbsEntry {AbsEntry}", absEntry);
    }

    private class PickListResponse {
        public int             AbsEntry       { get; set; }
        public string          Status         { get; set; } = string.Empty;
        public PickListLine[]? PickListsLines { get; set; }
    }

    private class PickListLine {
        public int       LineNumber                  { get; set; }
        public double    PickedQuantity              { get; set; }
        public object[]? DocumentLinesBinAllocations { get; set; }
    }
}