using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListLineService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ILogger<PickListService> logger,
    IPickListValidationService validationService,
    IPickingPackageLabelService packageLabelService) : IPickListLineService {
    public async Task<PickListAddItemResponse> AddItem(SessionInfo sessionInfo, PickListAddItemRequest request) {
        if (request.Quantity <= 0) {
            return PickListAddItemResponse.Error("Quantity must be greater than zero");
        }

        var scannedQuantity = request.Quantity;
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            if (request.PickingPackageLabelId.HasValue) {
                try {
                    await packageLabelService.ValidateForPickListAsync(request.PickingPackageLabelId.Value, request.ID, sessionInfo.Warehouse);
                }
                catch (InvalidOperationException ex) {
                    await transaction.RollbackAsync();
                    logger.LogWarning(ex, "Invalid picking package label for pick list {AbsEntry}", request.ID);
                    return PickListAddItemResponse.Error(ex.Message);
                }
            }

            var items = await adapter.ItemCheckAsync(request.ItemCode, null);
            var item = items.FirstOrDefault();
            if (request.Unit != UnitType.Unit) {
                if (item == null) {
                    throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, BarCode = (string?)null });
                }

                request.Quantity *= item.NumInBuy * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
            }

            if (item != null) {
                request.Quantity *= item.Factor1 * item.Factor2 * item.Factor3 * item.Factor4;
            }

            // Validate the add item request
            var (isValid, errorMessage, validationResult) = await validationService.ValidateItemForPicking(request);
            if (!isValid || validationResult == null) {
                return PickListAddItemResponse.Error(errorMessage!);
            }


            decimal itemStock = sessionInfo.EnableBinLocations ? validationResult.BinOnHand : validationResult.OnHand;
            decimal openQuantity = validationResult.OpenQuantity;
            (itemStock, openQuantity) = await validationService.CalculateBinOnHandQuantity(request.ItemCode, request.BinEntry, itemStock, openQuantity);

            var (quantityValid, quantityError, selectedValidation) = await validationService.ValidateQuantityAgainstPickList(
                request.ID, request.ItemCode, request.Quantity, [validationResult]);

            if (!quantityValid || selectedValidation == null) {
                return PickListAddItemResponse.Error(quantityError!);
            }

            if (request.Quantity > itemStock) {
                return PickListAddItemResponse.Error("Quantity exceeds bin available stock");
            }

            if (request.Quantity > openQuantity) {
                return PickListAddItemResponse.Error("Quantity exceeds open quantity");
            }

            var pickList = new PickList {
                Id = Guid.NewGuid(),
                AbsEntry = request.ID,
                PickEntry = selectedValidation.PickEntry ?? request.PickEntry ?? 0,
                ItemCode = request.ItemCode,
                Quantity = request.Quantity,
                ScannedQuantity = scannedQuantity,
                PickingPackageLabelId = request.PickingPackageLabelId,
                BinEntry = request.BinEntry,
                Unit = request.Unit,
                Status = ObjectStatus.Open,
                SyncStatus = SyncStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = sessionInfo.Guid
            };

            await db.PickLists.AddAsync(pickList);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return PickListAddItemResponse.OkResponse;
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error adding item to pick list");
            throw;
        }
    }
}
