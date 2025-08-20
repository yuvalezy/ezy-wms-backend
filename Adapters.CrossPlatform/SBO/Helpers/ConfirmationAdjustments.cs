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

            int? entry = null, exit = null;

            if (negativeItems.Count > 0) {
                logger.LogInformation("Creating inventory goods issue for {Count} negative items", negativeItems.Count);
                (bool issueSuccess, string? issueError, exit) = await CreateInventoryGoodsIssue();
                if (!issueSuccess) {
                    logger.LogError("Failed to create inventory goods issue: {Error}", issueError);
                    return ConfirmationAdjustmentsResponse.Error(issueError ?? "Failed to create inventory goods issue");
                    ;
                }

                logger.LogInformation("Successfully created inventory goods issue");
            }

            if (positiveItems.Count > 0) {
                logger.LogInformation("Creating inventory goods receipt for {Count} positive items", positiveItems.Count);
                (bool receiptSuccess, string? receiptError, entry) = await CreateInventoryGoodsReceipt();
                if (!receiptSuccess) {
                    logger.LogError("Failed to create inventory goods receipt: {Error}", receiptError);
                    return ConfirmationAdjustmentsResponse.Error(receiptError ?? "Failed to create inventory goods receipt");
                    ;
                }

                logger.LogInformation("Successfully created inventory goods receipt");
            }

            if (negativeItems.Count == 0 && positiveItems.Count == 0) {
                logger.LogInformation("No inventory adjustments needed for confirmation {Number}", number);
                return ConfirmationAdjustmentsResponse.Ok();
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

    private async Task<(bool success, string? errorMessage, int? exit)> CreateInventoryGoodsIssue() {
        try {
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

            logger.LogDebug("Posting inventory goods issue with {LineCount} lines to SAP Service Layer",
                negativeItems.Count);

            var (success, errorMessage) = await sboCompany.PostAsync("InventoryGenExits", goodsIssueData);

            if (success) {
                logger.LogInformation("Successfully created inventory goods issue for confirmation {Number}", number);
            }
            else {
                logger.LogError("Failed to create inventory goods issue: {Error}", errorMessage);
            }

            return (success, errorMessage, null);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Exception creating inventory goods issue: {Error}", ex.Message);
            return (false, $"Exception creating inventory goods issue: {ex.Message}", null);
        }
    }

    private async Task<(bool success, string? errorMessage, int? entry)> CreateInventoryGoodsReceipt() {
        try {
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

            logger.LogDebug("Posting inventory goods receipt with {LineCount} lines to SAP Service Layer",
                positiveItems.Count);

            var (success, errorMessage) = await sboCompany.PostAsync("InventoryGenEntries", goodsReceiptData);

            if (success) {
                logger.LogInformation("Successfully created inventory goods receipt for confirmation {Number}", number);
            }
            else {
                logger.LogError("Failed to create inventory goods receipt: {Error}", errorMessage);
            }

            return (success, errorMessage, null);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Exception creating inventory goods receipt: {Error}", ex.Message);
            return (false, $"Exception creating inventory goods receipt: {ex.Message}", null);
        }
    }
}