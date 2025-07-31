using Core.DTOs.Package;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListPackageService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    IPickListPackageEligibilityService eligibilityService,
    IPackageService packageService,
    IPickListValidationService validationService,
    IPickListPackageOperationsService packageOperations,
    IPickListPackageClosureService closureService,
    ISettings settings,
    ILogger<PickListPackageService> logger) : IPickListPackageService {
    public async Task<PickListPackageResponse> AddPackageAsync(PickListAddPackageRequest request, SessionInfo sessionInfo) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            // 1. Validate package for full picking
            var (package, validationError) = await packageOperations.ValidatePackageForFullPicking(request);
            if (validationError != null) {
                return validationError;
            }

            // 5. Get pick list details for the specific Type and Entry
            var detailParams = new Dictionary<string, object> {
                { "@AbsEntry", request.ID },
                { "@Type", request.Type },
                { "@Entry", request.Entry }
            };

            var pickingDetails = await adapter.GetPickingDetailItems(detailParams);
            if (!pickingDetails.Any()) {
                return PickListPackageResponse.ErrorResponse("No items found for the specified pick list entry");
            }

            // 6. Calculate open quantities accounting for already picked items
            var itemOpenQuantities = await validationService.CalculateOpenQuantitiesForPickList(request.ID, pickingDetails);

            // 7. Validate package eligibility
            if (!eligibilityService.ValidatePackageForPicking(
                    package.Contents.ToList(),
                    itemOpenQuantities,
                    out var errorMessage)) {
                return PickListPackageResponse.ErrorResponse(errorMessage ?? "Package cannot be fully picked");
            }

            // New: Load New Picking Package if applies
            Package? pickingPackage = null;
            if (request.PickingPackageId != null) {
                 pickingPackage = await db.Packages.Include(v => v.Contents).FirstOrDefaultAsync(v => v.Id == request.PickingPackageId.Value);
                if (pickingPackage == null) {
                    throw new KeyNotFoundException($"Picking package {request.PickingPackageId} not found");
                }
            }

            // 8. Create PickList entries for each package content
            var pickListIds = new List<Guid>();

            foreach (var content in package!.Contents) {
                var itemRequest = new PickListAddItemRequest {
                    ID = request.ID,
                    Type = request.Type,
                    Entry = request.Entry,
                    ItemCode = content.ItemCode,
                    Quantity = (int)content.Quantity,
                    BinEntry = request.BinEntry ?? package.BinEntry,
                    Unit = UnitType.Unit,
                };

                (var isValid, errorMessage, var validationResult) = await validationService.ValidateItemForPicking(itemRequest);
                if (!isValid || validationResult == null) {
                    throw new Exception($"Validation failed for item code: {content.ItemCode} in package {request.PackageId}: {errorMessage}");
                }

                int binOnHand = await validationService.CalculateBinOnHandQuantity(content.ItemCode, request.BinEntry, validationResult);

                var (quantityValid, quantityError, selectedValidation) = await validationService.ValidateQuantityAgainstPickList(
                    request.ID, content.ItemCode, (int)content.Quantity, [validationResult]);
                
                if (!quantityValid || selectedValidation == null) {
                    throw new Exception($"Quantity validation failed for item code: {content.ItemCode} in package {request.PackageId}: {quantityError}");
                }

                if (content.Quantity > binOnHand) {
                    throw new Exception($"Quantity exceeds bin available stock for item code: {content.ItemCode} in package {request.PackageId}");
                }

                var pickEntryToUse = selectedValidation.PickEntry ?? request.Entry;

                // Set common pick entry for the package (should be same for all items)

                var pickList = new PickList {
                    Id = Guid.NewGuid(),
                    AbsEntry = request.ID,
                    PickEntry = pickEntryToUse,
                    ItemCode = content.ItemCode,
                    Quantity = (int)content.Quantity,
                    BinEntry = request.BinEntry ?? package.BinEntry,
                    Unit = UnitType.Unit,
                    Status = ObjectStatus.Open,
                    SyncStatus = SyncStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = sessionInfo.Guid
                };

                db.PickLists.Add(pickList);
                pickListIds.Add(pickList.Id);

                // Update committed quantity
                content.CommittedQuantity += content.Quantity;
                db.PackageContents.Update(content);

                // Create package commitment
                var commitment = new PackageCommitment {
                    Id = Guid.NewGuid(),
                    PackageId = request.PackageId,
                    ItemCode = content.ItemCode,
                    Quantity = content.Quantity,
                    SourceOperationType = ObjectType.Picking,
                    SourceOperationId = pickList.Id,
                    TargetPackageId = request.PickingPackageId,
                    CommittedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = sessionInfo.Guid
                };

                db.PackageCommitments.Add(commitment);
                
                // Add content to New Picking Package if applies
                if (request.PickingPackageId != null) {
                    await packageOperations.AddOrUpdatePackageContent(
                        sessionInfo, 
                        request.PickingPackageId.Value, 
                        content.ItemCode, 
                        (int)content.Quantity, 
                        request.BinEntry ?? package.BinEntry,
                        request.ID,
                        request.Type,
                        request.Entry, 
                        pickList.Id);
                }

            }

            // 9. Create PickListPackage record
            var pickListPackage = new PickListPackage {
                Id = Guid.NewGuid(),
                AbsEntry = request.ID,
                PickEntry = -1,
                PackageId = request.PackageId,
                Type = SourceTarget.Source, // Pick lists only have source
                BinEntry = request.BinEntry ?? package.BinEntry,
                AddedAt = DateTime.UtcNow,
                AddedByUserId = sessionInfo.Guid,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = sessionInfo.Guid
            };

            db.PickListPackages.Add(pickListPackage);
            
            await db.SaveChangesAsync();

            // Prepare response
            var packageContents = await Task.WhenAll(
                package.Contents.Select(async c => await c.ToDto(adapter))
            );

            var response = new PickListPackageResponse {
                Status = ResponseStatus.Ok,
                PackageId = package.Id,
                PickListIds = pickListIds.ToArray(),
                PackageContents = packageContents.ToList()
            };

            await transaction.CommitAsync();
            return response;
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error adding package to pick list");
            return PickListPackageResponse.ErrorResponse($"Error adding package: {ex.Message}");
        }
    }

    public async Task ClearPickListCommitmentsAsync(int absEntry, Guid userId) {
        await closureService.ClearPickListCommitmentsAsync(absEntry, userId);
    }

    public async Task ProcessPickListClosureAsync(int absEntry, PickListClosureInfo closureInfo, Guid userId) {
        await closureService.ProcessPickListClosureAsync(absEntry, closureInfo, userId);
    }


    public async Task<PackageDto> CreatePackageAsync(int absEntry, SessionInfo sessionInfo) {
        if (!settings.Filters.StagingBinEntry.HasValue) {
            throw new Exception("Staging bin entry is not configured");       
        }

        var binEntry = settings.Filters.StagingBinEntry.Value;
        var request = new CreatePackageRequest {
            BinEntry = binEntry,
            SourceOperationType = ObjectType.Picking,
        };

        var package = await packageService.CreatePackageAsync(sessionInfo, request);
        var userId = sessionInfo.Guid;
        var pickListPackage = new PickListPackage {
            CreatedByUserId = userId,
            AbsEntry = absEntry,
            PackageId = package.Id,
            Type = SourceTarget.Target,
            BinEntry = binEntry,
            AddedAt = DateTime.UtcNow,
            AddedByUserId = userId,
        };
        await db.PickListPackages.AddAsync(pickListPackage);
        
        package.SourceOperationId = pickListPackage.Id;
        await db.SaveChangesAsync();

        var response = await package.ToDto(adapter, settings);
        response.PickListPackageId = pickListPackage.Id;
        return response;
    }
}