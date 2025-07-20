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
    SystemDbContext                   db,
    IExternalSystemAdapter            adapter,
    PickListPackageEligibilityService eligibilityService,
    ILogger<PickListPackageService>   logger) : IPickListPackageService {
    public async Task<PickListPackageResponse> AddPackageAsync(PickListAddPackageRequest request, SessionInfo sessionInfo) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            // 1. Load package with contents
            var package = await db.Packages
                .Include(p => p.Contents)
                .FirstOrDefaultAsync(p => p.Id == request.PackageId);

            if (package == null) {
                return PickListPackageResponse.ErrorResponse($"Package {request.PackageId} not found");
            }

            // 2. Validate package status
            if (package.Status == PackageStatus.Locked) {
                return PickListPackageResponse.ErrorResponse("Package is locked");
            }

            if (package.Status != PackageStatus.Active) {
                return PickListPackageResponse.ErrorResponse("Package is not active");
            }

            // 3. Validate bin location if specified
            if (request.BinEntry.HasValue && package.BinEntry != request.BinEntry.Value) {
                return PickListPackageResponse.ErrorResponse("Package is not in the specified bin location");
            }

            // 4. Check if package already added to this pick list
            var existingPackage = await db.PickListPackages
                .AnyAsync(plp => plp.AbsEntry == request.ID &&
                                 plp.PackageId == request.PackageId);

            if (existingPackage) {
                return PickListPackageResponse.ErrorResponse("Package already added to this pick list");
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
            var dbPicked = await db.PickLists
                .Where(p => p.AbsEntry == request.ID &&
                            (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
                .GroupBy(p => p.ItemCode)
                .Select(g => new { ItemCode = g.Key, PickedQty = g.Sum(p => p.Quantity) })
                .ToDictionaryAsync(x => x.ItemCode, x => x.PickedQty);

            var itemOpenQuantities = new Dictionary<string, int>();
            foreach (var item in pickingDetails) {
                var pickedQty = dbPicked.TryGetValue(item.ItemCode, out var qty) ? qty : 0;
                var openQty   = item.OpenQuantity - pickedQty;

                if (itemOpenQuantities.ContainsKey(item.ItemCode)) {
                    itemOpenQuantities[item.ItemCode] += openQty;
                }
                else {
                    itemOpenQuantities[item.ItemCode] = openQty;
                }
            }

            // 7. Validate package eligibility
            if (!eligibilityService.ValidatePackageForPicking(
                    package.Contents.ToList(),
                    itemOpenQuantities,
                    out var errorMessage)) {
                return PickListPackageResponse.ErrorResponse(errorMessage ?? "Package cannot be fully picked");
            }

            // 8. Create PickList entries for each package content
            var pickListIds = new List<Guid>();

            foreach (var content in package.Contents) {
                var validationResults = await adapter.ValidatePickingAddItem(new PickListAddItemRequest {
                    ID       = request.ID,
                    Type     = request.Type,
                    Entry    = request.Entry,
                    ItemCode = content.ItemCode,
                    Quantity = (int)content.Quantity,
                    BinEntry = request.BinEntry ?? package.BinEntry,
                    Unit     = UnitType.Unit,
                });
                if (validationResults.Length == 0) {
                    throw new Exception($"Validation results are empty for item code: {content.ItemCode} in package {request.PackageId}");
                }

                if (!validationResults[0].IsValid)
                    throw new Exception($"Validation failed for item code: {content.ItemCode} in package {request.PackageId}: {validationResults[0].ErrorMessage}");

                int result = db.PickLists
                    .Where(p => p.ItemCode == content.ItemCode && p.BinEntry == request.BinEntry && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
                    .Select(p => p.Quantity)
                    .Concat(
                        db.TransferLines
                            .Where(t => t.ItemCode == content.ItemCode && t.BinEntry == request.BinEntry && (t.LineStatus == LineStatus.Open || t.LineStatus == LineStatus.Processing))
                            .Select(t => t.Quantity)
                    )
                    .Sum();

                int binOnHand = validationResults.First().BinOnHand - result;

                var dbPickedQuantity = await db.PickLists
                    .Where(v => v.AbsEntry == request.ID && v.ItemCode == content.ItemCode && (v.Status == ObjectStatus.Open || v.Status == ObjectStatus.Processing))
                    .GroupBy(v => v.PickEntry)
                    .Select(v => new { PickEntry = v.Key, Quantity = v.Sum(vv => vv.Quantity) })
                    .ToArrayAsync();

                var check = (from v in validationResults.Where(a => a.IsValid)
                        join p in dbPickedQuantity on v.PickEntry equals p.PickEntry into gj
                        from sub in gj.DefaultIfEmpty()
                        where v.OpenQuantity - (sub?.Quantity ?? 0) >= 0
                        select new { ValidationResult = v, PickedQuantity = sub?.Quantity ?? 0 })
                    .FirstOrDefault();
                if (check == null) {
                    throw new Exception($"Quantity exceeds open quantity for item code: {content.ItemCode} in package {request.PackageId}");
                }

                check.ValidationResult.OpenQuantity -= check.PickedQuantity;

                if (content.Quantity > binOnHand) {
                    throw new Exception($"Quantity exceeds bin available stock for item code: {content.ItemCode} in package {request.PackageId}");
                }

                var pickEntryToUse = check.ValidationResult.PickEntry ?? request.Entry;

                // Set common pick entry for the package (should be same for all items)

                var pickList = new PickList {
                    Id              = Guid.NewGuid(),
                    AbsEntry        = request.ID,
                    PickEntry       = pickEntryToUse,
                    ItemCode        = content.ItemCode,
                    Quantity        = (int)content.Quantity,
                    BinEntry        = request.BinEntry ?? package.BinEntry,
                    Unit            = UnitType.Unit,
                    Status          = ObjectStatus.Open,
                    SyncStatus      = SyncStatus.Pending,
                    CreatedAt       = DateTime.UtcNow,
                    CreatedByUserId = sessionInfo.Guid
                };

                db.PickLists.Add(pickList);
                pickListIds.Add(pickList.Id);

                // Update committed quantity
                content.CommittedQuantity += content.Quantity;
                db.PackageContents.Update(content);

                // Create package commitment
                var commitment = new PackageCommitment {
                    Id                  = Guid.NewGuid(),
                    PackageId           = request.PackageId,
                    ItemCode            = content.ItemCode,
                    Quantity            = content.Quantity,
                    SourceOperationType = ObjectType.Picking,
                    SourceOperationId   = pickList.Id,
                    CommittedAt         = DateTime.UtcNow,
                    CreatedAt           = DateTime.UtcNow,
                    CreatedByUserId     = sessionInfo.Guid
                };

                db.PackageCommitments.Add(commitment);
            }

            // 9. Create PickListPackage record
            var pickListPackage = new PickListPackage {
                Id              = Guid.NewGuid(),
                AbsEntry        = request.ID,
                PickEntry       = -1,
                PackageId       = request.PackageId,
                Type            = SourceTarget.Source, // Pick lists only have source
                BinEntry        = request.BinEntry ?? package.BinEntry,
                AddedAt         = DateTime.UtcNow,
                AddedByUserId   = sessionInfo.Guid,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = sessionInfo.Guid
            };

            db.PickListPackages.Add(pickListPackage);

            await db.SaveChangesAsync();

            // Prepare response
            var packageContents = await Task.WhenAll(
                package.Contents.Select(async c => await c.ToDto(adapter))
            );

            var response = new PickListPackageResponse {
                Status          = ResponseStatus.Ok,
                PackageId       = package.Id,
                PickListIds     = pickListIds.ToArray(),
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

    public async Task ClearPickListCommitmentsAsync(int absEntry, SessionInfo sessionInfo) {
        // Get all package commitments for this pick list
        var pickListIds = await db.PickLists
            .Where(p => p.AbsEntry == absEntry)
            .Select(p => p.Id)
            .ToListAsync();

        var packageCommitments = await db.PackageCommitments
            .Where(pc => pc.SourceOperationType == ObjectType.Picking &&
                         pickListIds.Contains(pc.SourceOperationId))
            .ToListAsync();

        foreach (var commitment in packageCommitments) {
            // Find the corresponding package content and reduce committed quantity
            var packageContent = await db.PackageContents
                .FirstOrDefaultAsync(pc => pc.PackageId == commitment.PackageId &&
                                           pc.ItemCode == commitment.ItemCode);

            if (packageContent != null) {
                packageContent.CommittedQuantity -= commitment.Quantity;
                db.PackageContents.Update(packageContent);
            }

            // Remove the commitment record
            db.PackageCommitments.Remove(commitment);
        }

        await db.SaveChangesAsync();
    }
}