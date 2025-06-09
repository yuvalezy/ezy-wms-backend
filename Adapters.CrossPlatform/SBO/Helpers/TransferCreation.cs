using System.Text.Json.Serialization;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Transfer;
using Core.Enums;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class TransferCreation(
    SboCompany                                       sboCompany,
    int                                              transferNumber,
    string                                           whsCode,
    string?                                          comments,
    int                                              series,
    Dictionary<string, TransferCreationDataResponse> data,
    ILoggerFactory                                   loggerFactory) : IDisposable {
    
    private readonly ILogger<TransferCreation> logger = loggerFactory.CreateLogger<TransferCreation>();
    
    public int Entry  { get; private set; }
    public int Number { get; private set; }

    public async Task<ProcessTransferResponse> Execute() {
        var response = new ProcessTransferResponse();

        logger.LogInformation("Starting transfer creation for WMS transfer {TransferNumber} in warehouse {Warehouse}", 
            transferNumber, whsCode);
        logger.LogDebug("Transfer data contains {ItemCount} items with series {Series}", data.Count, series);

        try {
            var transferData = CreateTransferData();
            
            logger.LogInformation("Calling Service Layer StockTransfers POST with {LineCount} lines...", data.Count);
            var (success, errorMessage, result) = await sboCompany.PostAsync<StockTransferResponse>("StockTransfers", transferData);
            
            if (success && result != null) {
                Entry = result.DocEntry;
                Number = result.DocNum;
                
                response.Success = true;
                response.ExternalEntry = Entry;
                response.ExternalNumber = Number;
                response.Status = ResponseStatus.Ok;
                
                logger.LogInformation("Successfully created SAP B1 transfer {DocNumber} (Entry: {DocEntry}) for WMS transfer {TransferNumber}", 
                    Number, Entry, transferNumber);
            } else {
                response.Success = false;
                response.ErrorMessage = errorMessage ?? "Failed to create transfer";
                response.Status = ResponseStatus.Error;
                
                logger.LogError("Failed to create transfer for WMS transfer {TransferNumber}: {ErrorMessage}", 
                    transferNumber, errorMessage);
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to create transfer for WMS transfer {TransferNumber}", transferNumber);
            
            response.Success = false;
            response.ErrorMessage = ex.Message;
            response.Status = ResponseStatus.Error;
        }

        return response;
    }

    private object CreateTransferData() {
        logger.LogDebug("Creating transfer data for Service Layer...");
        
        var lines = new List<object>();
        
        foreach (var pair in data) {
            logger.LogDebug("Adding line for item {ItemCode} with quantity {Quantity}", 
                pair.Value.ItemCode, pair.Value.Quantity);
            
            var value = pair.Value;
            var binAllocations = new List<object>();
            
            // Add source bin allocations
            if (value.SourceBins.Any()) {
                logger.LogDebug("Adding {Count} source bin allocations for item {ItemCode}", 
                    value.SourceBins.Count, value.ItemCode);
                
                foreach (var source in value.SourceBins) {
                    binAllocations.Add(new {
                        BinActionType = "batFromWarehouse",
                        BinAbsEntry = source.BinEntry,
                        Quantity = source.Quantity
                    });
                    
                    logger.LogDebug("Added source bin {BinEntry} with quantity {Quantity}", 
                        source.BinEntry, source.Quantity);
                }
            }
            
            // Add target bin allocations
            if (value.TargetBins.Any()) {
                logger.LogDebug("Adding {Count} target bin allocations for item {ItemCode}", 
                    value.TargetBins.Count, value.ItemCode);
                
                foreach (var target in value.TargetBins) {
                    binAllocations.Add(new {
                        BinActionType = "batToWarehouse",
                        BinAbsEntry = target.BinEntry,
                        Quantity = target.Quantity
                    });
                    
                    logger.LogDebug("Added target bin {BinEntry} with quantity {Quantity}", 
                        target.BinEntry, target.Quantity);
                }
            }
            
            lines.Add(new {
                ItemCode = value.ItemCode,
                FromWarehouseCode = whsCode,
                WarehouseCode = whsCode,
                Quantity = value.Quantity,
                UseBaseUnits = "tYES",
                StockTransferLinesBinAllocations = binAllocations
            });
        }

        var transferData = new {
            DocDate = DateTime.Now.ToString("yyyy-MM-dd"),
            Series = series,
            Comments = comments ?? "",
            Reference2 = transferNumber.ToString(),
            StockTransferLines = lines
        };
        
        logger.LogDebug("Created transfer data with {LineCount} lines", lines.Count);
        return transferData;
    }

    public void Dispose() {
        logger.LogDebug("Disposing TransferCreation resources...");
    }
    
    private class StockTransferResponse {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
    }
}