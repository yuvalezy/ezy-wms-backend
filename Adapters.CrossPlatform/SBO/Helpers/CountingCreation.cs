using System.Text.Json.Serialization;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.InventoryCounting;
using Core.Enums;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class CountingCreation(
    SboCompany sboCompany,
    int countingNumber,
    string whsCode,
    int series,
    Dictionary<string, InventoryCountingCreationDataResponse> data,
    ILoggerFactory loggerFactory) : IDisposable {
    
    private readonly ILogger<CountingCreation> logger = loggerFactory.CreateLogger<CountingCreation>();

    public (int Entry, int Number) NewEntry { get; private set; }

    public async Task<ProcessInventoryCountingResponse> Execute() {
        var response = new ProcessInventoryCountingResponse();
        
        logger.LogInformation("Starting inventory counting creation for WMS counting {CountingNumber} in warehouse {Warehouse}", 
            countingNumber, whsCode);

        try {
            var countingData = CreateCountingData();
            
            logger.LogInformation("Calling Service Layer InventoryCountings POST with {LineCount} lines...", countingData.TotalLines);
            var (success, errorMessage, result) = await sboCompany.PostAsync<InventoryCountingResponse>("InventoryCountings", countingData.Data);
            
            if (success && result != null) {
                NewEntry = (result.DocumentEntry, result.DocumentNumber);
                
                response.Success = true;
                response.ExternalEntry = NewEntry.Entry;
                response.ExternalNumber = NewEntry.Number;
                response.Status = ResponseStatus.Ok;
                
                logger.LogInformation("Successfully created SAP B1 inventory counting {DocNumber} (Entry: {DocEntry}) for WMS counting {CountingNumber}", 
                    NewEntry.Number, NewEntry.Entry, countingNumber);
            } else {
                response.Success = false;
                response.ErrorMessage = errorMessage ?? "Failed to create inventory counting";
                response.Status = ResponseStatus.Error;
                
                logger.LogError("Failed to create inventory counting for WMS counting {CountingNumber}: {ErrorMessage}", 
                    countingNumber, errorMessage);
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to create inventory counting for WMS counting {CountingNumber}", countingNumber);
            
            response.Success = false;
            response.ErrorMessage = ex.Message;
            response.Status = ResponseStatus.Error;
        }

        return response;
    }

    private (object Data, int TotalLines) CreateCountingData() {
        
        var lines = new List<object>();
        int totalLines = 0;
        
        foreach (var value in data) {
            if (value.Value.CountedBins.Count > 0) {
                logger.LogDebug("Processing item {ItemCode} with {BinCount} bins", 
                    value.Value.ItemCode, value.Value.CountedBins.Count);
                
                foreach (var countedBin in value.Value.CountedBins) {
                    lines.Add(new {
                        ItemCode = value.Value.ItemCode,
                        WarehouseCode = whsCode,
                        BinEntry = countedBin.BinEntry,
                        Counted = "tYES",
                        CountedQuantity = countedBin.CountedQuantity
                    });
                    totalLines++;
                    
                    logger.LogDebug("Added counting line for item {ItemCode} in bin {BinEntry} with quantity {Quantity} (system: {SystemQuantity})", 
                        value.Value.ItemCode, countedBin.BinEntry, countedBin.CountedQuantity, countedBin.SystemQuantity);
                }
            }
            else {
                
                lines.Add(new {
                    ItemCode = value.Value.ItemCode,
                    WarehouseCode = whsCode,
                    Counted = "tYES",
                    CountedQuantity = value.Value.CountedQuantity
                });
                totalLines++;
                
                logger.LogDebug("Added counting line for item {ItemCode} with quantity {Quantity} (system: {SystemQuantity}, variance: {Variance})", 
                    value.Value.ItemCode, value.Value.CountedQuantity, value.Value.SystemQuantity, value.Value.Variance);
            }
        }

        var countingData = new {
            Series = series,
            Reference2 = countingNumber.ToString(),
            InventoryCountingLines = lines
        };
        return (countingData, totalLines);
    }

    public void Dispose() {
    }
    
    private class InventoryCountingResponse {
        public int DocumentEntry { get; set; }
        public int DocumentNumber { get; set; }
    }
}