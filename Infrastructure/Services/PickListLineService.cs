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
    IPickListPackageOperationsService packageOperations) : IPickListLineService {
    public async Task<PickListAddItemResponse> AddItem(SessionInfo sessionInfo, PickListAddItemRequest request) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            if (request.Unit != UnitType.Unit) {
                var items = await adapter.ItemCheckAsync(request.ItemCode, null);
                var item = items.FirstOrDefault();
                if (item == null) {
                    throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, BarCode = (string?)null });
                }

                request.Quantity *= item.NumInBuy * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
            }

            // Handle package-specific logic if PackageId is provided
            var (package, packageContent, packageValidation) = await packageOperations.ValidatePackageForItem(request);
            if (packageValidation != null) {
                return packageValidation;
            }

            // Validate the add item request
            var (isValid, errorMessage, validationResult) = await validationService.ValidateItemForPicking(request);
            if (!isValid || validationResult == null) {
                return PickListAddItemResponse.Error(errorMessage!);
            }

            int binOnHand = await validationService.CalculateBinOnHandQuantity(request.ItemCode, request.BinEntry, validationResult);

            var (quantityValid, quantityError, selectedValidation) = await validationService.ValidateQuantityAgainstPickList(
                request.ID, request.ItemCode, request.Quantity, [validationResult]);
            
            if (!quantityValid || selectedValidation == null) {
                return PickListAddItemResponse.Error(quantityError!);
            }

            if (request.Quantity > binOnHand) {
                return PickListAddItemResponse.Error("Quantity exceeds bin available stock");
            }

            var pickList = new PickList {
                Id = Guid.NewGuid(),
                AbsEntry = request.ID,
                PickEntry = selectedValidation.PickEntry ?? request.PickEntry ?? 0,
                ItemCode = request.ItemCode,
                Quantity = request.Quantity,
                BinEntry = request.BinEntry,
                Unit = request.Unit,
                Status = ObjectStatus.Open,
                SyncStatus = SyncStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = sessionInfo.Guid
            };

            await db.PickLists.AddAsync(pickList);

            await AddItemPackage(sessionInfo, pickList, package, packageContent, request);
        
            await AddNewPackageContent(sessionInfo, request, pickList.Id);

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

    private async Task AddItemPackage(SessionInfo sessionInfo, PickList pickList, Package? package, PackageContent? packageContent, PickListAddItemRequest request) {
        if (!request.PackageId.HasValue || packageContent == null || package == null) {
            return;
        }

        packageContent.CommittedQuantity += request.Quantity;
        db.PackageContents.Update(packageContent);

        // Create package commitment
        await packageOperations.CreatePackageCommitment(sessionInfo, pickList, request);

        // Create PickListPackage record if not exists
        await packageOperations.CreatePickListPackageIfNotExists(sessionInfo, pickList, request, package);
    }
    private async Task AddNewPackageContent(SessionInfo sessionInfo, PickListAddItemRequest request, Guid pickListId) {
        // If content is added to a new picking package
        if (request.PickingPackageId != null) {
            await packageOperations.AddOrUpdatePackageContent(
                sessionInfo, 
                request.PickingPackageId.Value, 
                request.ItemCode, 
                request.Quantity, 
                request.BinEntry,
                request.ID,
                request.Type,
                request.Entry, 
                pickListId);
        }
    }

}