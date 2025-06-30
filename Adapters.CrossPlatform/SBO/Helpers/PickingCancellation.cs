using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.Enums;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class PickingCancellation(SboCompany sboCompany, int absEntry, PickingSelectionResponse[] selection, string warehouse, int cancelBinEntry, ILoggerFactory loggerFactory) {
    private readonly ILogger<PickingCancellation> logger = loggerFactory.CreateLogger<PickingCancellation>();

    public async Task<ProcessPickListResponse> Execute() {
        logger.LogInformation("Starting pick list cancellation for AbsEntry: {AbsEntry}", absEntry);
        try {
            // Prepare batch operations
            var closePickListData = new {
                PickList = new {
                    Absoluteentry = absEntry
                }
            };

            var transferData = new {
                DocDate       = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                FromWarehouse = warehouse,
                ToWarehouse   = warehouse,
                StockTransferLines = selection.GroupBy(v => v.ItemCode)
                    .Select(v => new {
                        ItemCode          = v.Key,
                        Quantity          = v.Sum(w => w.Quantity),
                        FromWarehouseCode = warehouse,
                        WarehouseCode     = warehouse,
                        StockTransferLinesBinAllocations = v
                            .Select(x => new {
                                BinAbsEntry   = x.BinEntry,
                                Quantity      = x.Quantity,
                                BinActionType = "batFromWarehouse"
                            })
                            .Concat([
                                new {
                                BinAbsEntry   = cancelBinEntry,
                                Quantity      = v.Sum(w => w.Quantity),
                                BinActionType = "batToWarehouse"
                            }
                            ])
                            .ToList()
                    })
                    .ToList()
            };

            // Execute both operations in a single batch transaction
            var closePickListOperation = new SboCompany.BatchOperation {
                Method = "POST",
                Endpoint = "PickListsService_Close",
                Body = System.Text.Json.JsonSerializer.Serialize(closePickListData)
            };

            var transferOperation = new SboCompany.BatchOperation {
                Method = "POST",
                Endpoint = "StockTransfers",
                Body = System.Text.Json.JsonSerializer.Serialize(transferData)
            };

            (bool success, string? errorMessage, var responses) = await sboCompany.ExecuteBatchAsync(closePickListOperation, transferOperation);
            
            if (!success) {
                logger.LogError("Failed to execute batch operation for pick list cancellation. Error: {ErrorMessage}", errorMessage);
                return Error(errorMessage ?? "Failed to cancel pick list");
            }

            // Extract the transfer document number from the second response if needed
            int? transferDocEntry = null;
            if (responses.Count > 1 && responses[1].StatusCode == 201 && responses[1].Body != null) {
                try {
                    using var doc = JsonDocument.Parse(responses[1].Body);
                    if (doc.RootElement.TryGetProperty("DocEntry", out var docEntryElement)) {
                        transferDocEntry = docEntryElement.GetInt32();
                    }
                }
                catch {
                    // Ignore parsing errors
                }
            }

            logger.LogInformation("Successfully cancelled pick list {AbsEntry} and created transfer {TransferDocEntry}", absEntry, transferDocEntry);
            return new ProcessPickListResponse { 
                Status = ResponseStatus.Ok,
                DocumentNumber = transferDocEntry
            };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error cancelling pick list {AbsEntry}: {ErrorMessage}", absEntry, ex.Message);
            return Error(ex.Message);
        }
    }


    private ProcessPickListResponse Error(string errorMessage) => new() { Status = ResponseStatus.Error, ErrorMessage = errorMessage };
}