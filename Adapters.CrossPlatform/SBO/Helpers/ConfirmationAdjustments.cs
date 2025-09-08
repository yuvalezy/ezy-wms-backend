using Adapters.CrossPlatform.SBO.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Adapters.Common.SBO.Models;
using Core.DTOs.GoodsReceipt;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class ConfirmationAdjustments(
    ProcessConfirmationAdjustmentsParameters @params,
    int entrySeries,
    int exitSeries,
    SboCompany sboCompany,
    ILoggerFactory loggerFactory) {
    private readonly ILogger<ConfirmationAdjustments> logger = loggerFactory.CreateLogger<ConfirmationAdjustments>();

    public async Task<ConfirmationAdjustmentsResponse> Execute() {
        logger.LogInformation("Starting confirmation adjustments for confirmation {Number} in warehouse {Warehouse}", @params.Number, @params.Warehouse);

        try {
            await sboCompany.ConnectCompany();

            // Prepare batch operations
            var operations = new List<SboCompany.BatchOperation>();

            if (@params.NegativeItems.Count > 0) {
                logger.LogInformation("Preparing inventory goods issue for {Count} negative items", @params.NegativeItems.Count);
                operations.Add(CreateInventoryGoodsIssueOperation());
            }

            if (@params.PositiveItems.Count > 0) {
                logger.LogInformation("Preparing inventory goods receipt for {Count} positive items", @params.PositiveItems.Count);
                operations.Add(CreateInventoryGoodsReceiptOperation());
            }

            if (operations.Count == 0) {
                logger.LogInformation("No inventory adjustments needed for confirmation {Number}", @params.Number);
                return ConfirmationAdjustmentsResponse.Ok();
            }

            // Execute batch operations
            logger.LogInformation("Executing batch operation with {Count} operations for confirmation {Number}", operations.Count, @params.Number);

            (bool success, string? errorMessage, var responses) = await sboCompany.ExecuteBatchAsync(operations.ToArray());

            if (!success) {
                logger.LogError("Failed to execute batch operation for confirmation adjustments. Error: {ErrorMessage}", errorMessage);
                return ConfirmationAdjustmentsResponse.Error(errorMessage ?? "Failed to process confirmation adjustments");
            }

            // Extract document entries from responses
            int? entry = null, exit = null;
            int responseIndex = 0;

            if (@params.NegativeItems.Count > 0 && responseIndex < responses.Count) {
                var issueResponse = responses[responseIndex];
                if (issueResponse is { StatusCode: 201, Body: not null }) {
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

            if (@params.PositiveItems.Count > 0 && responseIndex < responses.Count) {
                var receiptResponse = responses[responseIndex];
                if (receiptResponse is { StatusCode: 201, Body: not null }) {
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

            logger.LogInformation("Successfully completed all confirmation adjustments for confirmation {Number}", @params.Number);
            return ConfirmationAdjustmentsResponse.Ok(entry, exit);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error processing confirmation adjustments for confirmation {Number}: {Error}", @params.Number, ex.Message);

            return ConfirmationAdjustmentsResponse.Error($"Error processing confirmation adjustments: {ex.Message}");
        }
    }

    private SboCompany.BatchOperation CreateInventoryGoodsIssueOperation() {
        var goodsIssueData = new {
            Series = exitSeries,
            DocDate = DateTime.Now.ToString("yyyy-MM-dd"),
            DocDueDate = DateTime.Now.ToString("yyyy-MM-dd"),
            Comments = $"Ajuste de inventario para confirmación de WMS {@params.Number} - Salida de mercancías",
            DocumentLines = @params.NegativeItems.Select((item, index) => new {
                ItemCode = item.ItemCode,
                Quantity = Math.Abs(item.Quantity),
                WarehouseCode = @params.Warehouse,
                UseBaseUnits = "tYES",
                LineTotal = GetLineTotal(item),
                DocumentLinesBinAllocations = @params is { EnableBinLocation: true, DefaultBinLocation: not null }
                ? new[] {
                    new {
                        BinAbsEntry = @params.DefaultBinLocation.Value,
                        Quantity = Math.Abs(item.Quantity),
                        AllowNegativeQuantity = "tNO",
                        SerialAndBatchNumbersBaseLine = -1,
                        BaseLineNumber = index
                    }
                }
                : Array.Empty<object>()
            })
        };

        logger.LogDebug("Prepared inventory goods issue operation with {LineCount} lines", @params.NegativeItems.Count);

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
            Comments = $"Ajuste de inventario para confirmación de WMS {@params.Number} - Entrada de mercancías",
            DocumentLines = @params.PositiveItems.Select((item, index) => new {
                ItemCode = item.ItemCode,
                Quantity = item.Quantity,
                WarehouseCode = @params.Warehouse,
                UseBaseUnits = "tYES",
                LineTotal = GetLineTotal(item),
                DocumentLinesBinAllocations = @params is { EnableBinLocation: true, DefaultBinLocation: not null }
                ? new[] {
                    new {
                        BinAbsEntry = @params.DefaultBinLocation.Value,
                        Quantity = item.Quantity,
                        AllowNegativeQuantity = "tNO",
                        SerialAndBatchNumbersBaseLine = -1,
                        BaseLineNumber = index
                    }
                }
                : Array.Empty<object>()
            })
        };

        logger.LogDebug("Prepared inventory goods receipt operation with {LineCount} lines", @params.PositiveItems.Count);

        return new SboCompany.BatchOperation {
            Method = "POST",
            Endpoint = "InventoryGenEntries",
            Body = JsonSerializer.Serialize(goodsReceiptData)
        };
    }

    private decimal? GetLineTotal((string ItemCode, decimal Quantity) item) => @params.ItemsCost?.TryGetValue(item.ItemCode, out decimal price) ?? false ? price * item.Quantity : null;
}