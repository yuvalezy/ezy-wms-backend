using Adapters.CrossPlatform.SBO.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Adapters.Common.SBO.Models;
using Core.DTOs.GoodsReceipt;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class ConfirmationAdjustments(
    int number,
    string warehouse,
    bool enableBinLocation,
    int? defaultBinLocation,
    List<(string ItemCode, decimal Quantity)> negativeItems,
    List<(string ItemCode, decimal Quantity)> positiveItems,
    int entrySeries,
    int exitSeries,
    SboCompany sboCompany,
    ILoggerFactory loggerFactory) {
    private readonly ILogger<ConfirmationAdjustments> logger = loggerFactory.CreateLogger<ConfirmationAdjustments>();

    public async Task<ConfirmationAdjustmentsResponse> Execute() {
        logger.LogInformation("Starting confirmation adjustments for confirmation {Number} in warehouse {Warehouse}",
            number, warehouse);

        try {
            await sboCompany.ConnectCompany();

            // Prepare batch operations
            var operations = new List<SboCompany.BatchOperation>();
            
            if (negativeItems.Count > 0) {
                logger.LogInformation("Preparing inventory goods issue for {Count} negative items", negativeItems.Count);
                operations.Add(CreateInventoryGoodsIssueOperation());
            }

            if (positiveItems.Count > 0) {
                logger.LogInformation("Preparing inventory goods receipt for {Count} positive items", positiveItems.Count);
                operations.Add(CreateInventoryGoodsReceiptOperation());
            }

            if (operations.Count == 0) {
                logger.LogInformation("No inventory adjustments needed for confirmation {Number}", number);
                return ConfirmationAdjustmentsResponse.Ok();
            }

            // Execute batch operations
            logger.LogInformation("Executing batch operation with {Count} operations for confirmation {Number}", 
                operations.Count, number);
            (bool success, string? errorMessage, var responses) = await sboCompany.ExecuteBatchAsync(operations.ToArray());
            
            if (!success) {
                logger.LogError("Failed to execute batch operation for confirmation adjustments. Error: {ErrorMessage}", errorMessage);
                return ConfirmationAdjustmentsResponse.Error(errorMessage ?? "Failed to process confirmation adjustments");
            }

            // Extract document entries from responses
            int? entry = null, exit = null;
            int responseIndex = 0;
            
            if (negativeItems.Count > 0 && responseIndex < responses.Count) {
                var issueResponse = responses[responseIndex];
                if (issueResponse.StatusCode == 201 && issueResponse.Body != null) {
                    try {
                        using var doc = JsonDocument.Parse(issueResponse.Body);
                        if (doc.RootElement.TryGetProperty("DocEntry", out var docEntryElement)) {
                            exit = docEntryElement.GetInt32();
                            logger.LogInformation("Successfully created inventory goods issue with DocEntry {DocEntry}", exit);
                        }
                    }
                    catch (Exception ex) {
                        logger.LogWarning("Failed to parse goods issue response: {Error}", ex.Message);
                    }
                }
                responseIndex++;
            }
            
            if (positiveItems.Count > 0 && responseIndex < responses.Count) {
                var receiptResponse = responses[responseIndex];
                if (receiptResponse.StatusCode == 201 && receiptResponse.Body != null) {
                    try {
                        using var doc = JsonDocument.Parse(receiptResponse.Body);
                        if (doc.RootElement.TryGetProperty("DocEntry", out var docEntryElement)) {
                            entry = docEntryElement.GetInt32();
                            logger.LogInformation("Successfully created inventory goods receipt with DocEntry {DocEntry}", entry);
                        }
                    }
                    catch (Exception ex) {
                        logger.LogWarning("Failed to parse goods receipt response: {Error}", ex.Message);
                    }
                }
            }

            logger.LogInformation("Successfully completed all confirmation adjustments for confirmation {Number}", number);
            return ConfirmationAdjustmentsResponse.Ok(entry, exit);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error processing confirmation adjustments for confirmation {Number}: {Error}",
                number, ex.Message);

            return ConfirmationAdjustmentsResponse.Error($"Error processing confirmation adjustments: {ex.Message}");
        }
    }

    private SboCompany.BatchOperation CreateInventoryGoodsIssueOperation() {
        var goodsIssueData = new {
            Series = exitSeries,
            DocDate = DateTime.Now.ToString("yyyy-MM-dd"),
            DocDueDate = DateTime.Now.ToString("yyyy-MM-dd"),
            Comments = $"Ajuste de inventario para confirmación de WMS {number} - Salida de mercancías",
            DocumentLines = negativeItems.Select((item, index) => new {
                ItemCode = item.ItemCode,
                Quantity = Math.Abs(item.Quantity),
                WarehouseCode = warehouse,
                UseBaseUnits = "tYES",
                DocumentLinesBinAllocations = enableBinLocation && defaultBinLocation.HasValue
                ? new[] {
                    new {
                        BinAbsEntry = defaultBinLocation.Value,
                        Quantity = Math.Abs(item.Quantity),
                        AllowNegativeQuantity = "tNO",
                        SerialAndBatchNumbersBaseLine = -1,
                        BaseLineNumber = index
                    }
                }
                : Array.Empty<object>()
            })
        };

        logger.LogDebug("Prepared inventory goods issue operation with {LineCount} lines", negativeItems.Count);

        return new SboCompany.BatchOperation {
            Method = "POST",
            Endpoint = "InventoryGenExits",
            Body = JsonSerializer.Serialize(goodsIssueData)
        };
    }

    private SboCompany.BatchOperation CreateInventoryGoodsReceiptOperation() {
        var goodsReceiptData = new {
            Series = entrySeries,
            DocDate = DateTime.Now.ToString("yyyy-MM-dd"),
            DocDueDate = DateTime.Now.ToString("yyyy-MM-dd"),
            Comments = $"Ajuste de inventario para confirmación de WMS {number} - Entrada de mercancías",
            DocumentLines = positiveItems.Select((item, index) => new {
                ItemCode = item.ItemCode,
                Quantity = item.Quantity,
                WarehouseCode = warehouse,
                UseBaseUnits = "tYES",
                DocumentLinesBinAllocations = enableBinLocation && defaultBinLocation.HasValue
                ? new[] {
                    new {
                        BinAbsEntry = defaultBinLocation.Value,
                        Quantity = item.Quantity,
                        AllowNegativeQuantity = "tNO",
                        SerialAndBatchNumbersBaseLine = -1,
                        BaseLineNumber = index
                    }
                }
                : Array.Empty<object>()
            })
        };

        logger.LogDebug("Prepared inventory goods receipt operation with {LineCount} lines", positiveItems.Count);

        return new SboCompany.BatchOperation {
            Method = "POST",
            Endpoint = "InventoryGenEntries",
            Body = JsonSerializer.Serialize(goodsReceiptData)
        };
    }
    // AdjustmentResponse class removed - no longer needed with batch operations
}
