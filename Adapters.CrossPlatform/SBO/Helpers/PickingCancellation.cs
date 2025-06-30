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
            var closePickListData = new {
                PickList = new {
                    Absoluteentry = absEntry
                }
            };

            //Close Pick List
            (bool success, string? errorMessage) = await sboCompany.PostAsync("PickListsService_Close", closePickListData);
            if (!success) {
                logger.LogError("Failed to close pick list {AbsEntry} via Service Layer. Error: {ErrorMessage}", absEntry, errorMessage);
                return Error(errorMessage ?? "Failed to close pick list");
            }

            //Transfer to cancelBinEntry
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
            (success, errorMessage) = await sboCompany.PostAsync("StockTransfers", transferData);
            if (!success) {
                logger.LogError("Failed to transfer items to cancel bin. Error: {ErrorMessage}", errorMessage);
                return Error(errorMessage ?? "Failed to transfer items to cancel bin");           
            }


            logger.LogInformation("Successfully cancelled pick list {AbsEntry}", absEntry);
            return new ProcessPickListResponse { Status = ResponseStatus.Ok };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error cancelling pick list {AbsEntry}: {ErrorMessage}", absEntry, ex.Message);
            return Error(ex.Message);
        }
    }


    private ProcessPickListResponse Error(string errorMessage) => new() { Status = ResponseStatus.Error, ErrorMessage = errorMessage };
}