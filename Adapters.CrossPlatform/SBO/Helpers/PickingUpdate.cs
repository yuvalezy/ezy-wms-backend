using System.Text.Json.Serialization;
using Adapters.CrossPlatform.SBO.Services;
using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class PickingUpdate(
    int            absEntry,
    List<PickList> data,
    SboCompany     sboCompany,
    ILoggerFactory loggerFactory) : IDisposable {
    private readonly ILogger<PickingUpdate> logger = loggerFactory.CreateLogger<PickingUpdate>();
    private          PickListResponse?      pickListResponse;

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
        pickListResponse = await sboCompany.GetAsync<PickListResponse>($"PickLists({absEntry})");
        if (pickListResponse == null) {
            logger.LogError("Could not find Pick List {AbsEntry}", absEntry);
            throw new Exception($"Could not find Pick List {absEntry}");
        }
        if (pickListResponse.Status == "ps_Closed") {
            logger.LogWarning("Cannot process pick list {AbsEntry} because status is closed", absEntry);
            throw new Exception("Cannot process document if the Status is closed");
        }
    }

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

    private async Task ProcessPickList() {
        
        // Group data by pick entry
        var lines = data.GroupBy(v => v.PickEntry)
            .Select(a => new {
                PickEntry = a.Key,
                Quantity  = a.Sum(b => b.Quantity),
                Bins = a.GroupBy(b => b.BinEntry)
                    .Select(c => new { BinEntry = c.Key, Quantity = c.Sum(d => d.Quantity) })
                    .ToList()
            }).ToList();

        foreach (var pickLine in pickListResponse.PickListsLines) {
            var matchingData = lines.FirstOrDefault(v => v.PickEntry == pickLine.LineNumber);
            if (matchingData == null) {
                continue;
            }

            double pickedQuantity = matchingData.Quantity;
            logger.LogDebug("Processing pick line {LineNumber} with quantity {Quantity} for pick list {AbsEntry}", 
                pickLine.LineNumber, pickedQuantity, absEntry);
            
            pickLine.PreviouslyReleasedQuantity =  pickLine.ReleasedQuantity;
            if (pickLine.PickedQuantity == 0) {
                pickLine.ReleasedQuantity = pickedQuantity;
                pickLine.PickedQuantity   = pickedQuantity;
            }
            else {
                pickLine.ReleasedQuantity += pickedQuantity;
                pickLine.PickedQuantity   += pickedQuantity;
            }

            pickLine.PickStatus = "ps_Picked";

            logger.LogDebug("Processing {BinCount} bin allocations for pick line {LineNumber}", 
                matchingData.Bins.Count, pickLine.LineNumber);

            foreach (var bin in matchingData.Bins) {
                bool found = false;
                foreach (var allocation in pickLine.DocumentLinesBinAllocations) {
                    if (allocation.BinAbsEntry == bin.BinEntry) {
                        allocation.Quantity += bin.Quantity;
                        found               =  true;
                        logger.LogDebug("Updated existing bin allocation for BinEntry {BinEntry} with quantity {Quantity}", 
                            bin.BinEntry, bin.Quantity);
                        break;
                    }
                }

                if (!found) {
                    pickLine.DocumentLinesBinAllocations.Add(new PickListLineBinAllocation {
                        BaseLineNumber = pickLine.LineNumber,
                        BinAbsEntry    = bin.BinEntry!.Value,
                        Quantity       = bin.Quantity,
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

    private class PickListResponse {
        [JsonPropertyName("Absoluteentry")]
        public int AbsoluteEntry { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("OwnerCode")]
        public int OwnerCode { get; set; }

        [JsonPropertyName("OwnerName")]
        public string? OwnerName { get; set; }

        [JsonPropertyName("PickDate")]
        public DateTime PickDate { get; set; }

        [JsonPropertyName("Remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("Status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("ObjectType")]
        public string ObjectType { get; set; } = string.Empty;

        [JsonPropertyName("UseBaseUnits")]
        public string UseBaseUnits { get; set; } = string.Empty;

        public PickListLine[] PickListsLines { get; set; }
    }

    private class PickListLine {
        [JsonPropertyName("AbsoluteEntry")]
        public int AbsoluteEntry { get; set; }

        [JsonPropertyName("LineNumber")]
        public int LineNumber { get; set; }

        [JsonPropertyName("OrderEntry")]
        public int OrderEntry { get; set; }

        [JsonPropertyName("OrderRowID")]
        public int OrderRowID { get; set; }

        [JsonPropertyName("PickedQuantity")]
        public double PickedQuantity { get; set; }

        [JsonPropertyName("PickStatus")]
        public string PickStatus { get; set; } = string.Empty;

        [JsonPropertyName("ReleasedQuantity")]
        public double ReleasedQuantity { get; set; }

        [JsonPropertyName("PreviouslyReleasedQuantity")]
        public double PreviouslyReleasedQuantity { get; set; }

        [JsonPropertyName("BaseObjectType")]
        public int BaseObjectType { get; set; }

        public ICollection<PickListLineBinAllocation> DocumentLinesBinAllocations { get; set; }
    }

    private class PickListLineBinAllocation {
        [JsonPropertyName("AllowNegativeQuantity")]
        public string AllowNegativeQuantity { get; set; } = "tNO";

        [JsonPropertyName("BaseLineNumber")]
        public int BaseLineNumber { get; set; }

        [JsonPropertyName("BinAbsEntry")]
        public int BinAbsEntry { get; set; }

        [JsonPropertyName("Quantity")]
        public double Quantity { get; set; }

        [JsonPropertyName("SerialAndBatchNumbersBaseLine")]
        public int SerialAndBatchNumbersBaseLine { get; set; } = -1;
    }
}