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
    PickListPackageEligibilityService eligibilityService,
    IPackageService packageService,
    IPackageContentService packageContentService,
    ISettings settings,
    ILogger<PickListPackageService> logger) : IPickListPackageService {
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
                var openQty = item.OpenQuantity - pickedQty;

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
                    ID = request.ID,
                    Type = request.Type,
                    Entry = request.Entry,
                    ItemCode = content.ItemCode,
                    Quantity = (int)content.Quantity,
                    BinEntry = request.BinEntry ?? package.BinEntry,
                    Unit = UnitType.Unit,
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
                    CommittedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = sessionInfo.Guid
                };

                db.PackageCommitments.Add(commitment);
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

    public async Task ProcessPickListClosureAsync(int absEntry, PickListClosureInfo closureInfo, Guid userId) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            logger.LogInformation("Processing pick list closure for AbsEntry {AbsEntry}, Reason: {Reason}",
                absEntry, closureInfo.ClosureReason);

            // If closure was due to follow-up documents, process package movements FIRST
            // We need to do this before clearing commitments so we know what was picked
            if (closureInfo.RequiresPackageMovement) {
                await ProcessPackageMovementsFromFollowUpDocuments(absEntry, closureInfo, userId);
            }

            // Clear commitments after processing movements
            await ClearPickListCommitmentsAsync(absEntry, userId);

            // Mark all PickListPackages as processed to avoid reprocessing
            var pickListPackages = await db.PickListPackages
            .Where(plp => plp.AbsEntry == absEntry && plp.ProcessedAt == null)
            .ToListAsync();

            foreach (var plp in pickListPackages) {
                plp.ProcessedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error processing pick list closure for AbsEntry {AbsEntry}", absEntry);
            throw;
        }
    }

    private async Task ProcessPackageMovementsFromFollowUpDocuments(int absEntry, PickListClosureInfo closureInfo, Guid userId) {
        // Get all PickList entries for this absEntry with their PickEntry values
        var pickListEntries = await db.PickLists
        .Where(p => p.AbsEntry == absEntry)
        .Select(p => new { p.Id, p.PickEntry, p.ItemCode })
        .ToListAsync();

        if (!pickListEntries.Any()) {
            logger.LogDebug("No pick list entries found for AbsEntry {AbsEntry}", absEntry);
            return;
        }

        // Create a lookup from PickList.Id to PickEntry for later use
        var pickListIdToPickEntry = pickListEntries.ToDictionary(p => p.Id, p => p.PickEntry);
        var pickListIds = pickListEntries.Select(p => p.Id).ToList();

        // Get all package commitments for this pick list
        var packageCommitments = await db.PackageCommitments
        .Where(pc => pc.SourceOperationType == ObjectType.Picking &&
                     pickListIds.Contains(pc.SourceOperationId))
        .ToListAsync();

        if (!packageCommitments.Any()) {
            logger.LogDebug("No package commitments found for pick list {AbsEntry}", absEntry);
            return;
        }

        // Create a lookup of commitments by PickEntry, PackageId, and ItemCode
        var commitmentLookup = new Dictionary<(int PickEntry, Guid PackageId, string ItemCode), decimal>();
        foreach (var commitment in packageCommitments) {
            if (!pickListIdToPickEntry.TryGetValue(commitment.SourceOperationId, out var pickEntry))
                continue;

            var key = (pickEntry, commitment.PackageId, commitment.ItemCode);
            if (commitmentLookup.ContainsKey(key)) {
                commitmentLookup[key] += commitment.Quantity;
            }
            else {
                commitmentLookup[key] = commitment.Quantity;
            }
        }

        // Get all unique package IDs
        var packageIds = packageCommitments.Select(pc => pc.PackageId).Distinct().ToList();

        // Load all packages at once with AsNoTracking to avoid EF conflicts
        var packages = await db.Packages
        .AsNoTracking()
        .Include(p => p.Contents)
        .Where(p => packageIds.Contains(p.Id))
        .ToListAsync();

        // Structure to accumulate all changes per package
        var packageChanges = new Dictionary<Guid, List<(string ItemCode, int Quantity, string Notes)>>();

        // Process movements based on follow-up documents
        foreach (var followUpDoc in closureInfo.FollowUpDocuments) {
            var pickEntry = followUpDoc.PickEntry;

            foreach (var docItem in followUpDoc.Items) {
                // Find packages that have commitments for this pick entry and item
                foreach (var package in packages) {
                    var commitmentKey = (pickEntry, package.Id, docItem.ItemCode);
                    if (!commitmentLookup.TryGetValue(commitmentKey, out var committedQty) || committedQty <= 0) {
                        continue;
                    }

                    // Find matching package content with bin location check
                    var packageContent = package.Contents
                    .FirstOrDefault(pc => pc.ItemCode == docItem.ItemCode &&
                                          (docItem.BinEntry == null || pc.BinEntry == docItem.BinEntry));

                    if (packageContent == null) {
                        continue;
                    }

                    // The quantity to reduce is the minimum of:
                    // 1. The quantity in the follow-up document for this item
                    // 2. The committed quantity for THIS SPECIFIC pick entry
                    // 3. The actual package content quantity (safety check)
                    var quantityToReduce = Math.Min(Math.Min(docItem.Quantity, (int)committedQty), (int)packageContent.Quantity);

                    if (quantityToReduce <= 0) {
                        continue;
                    }

                    // Accumulate changes for this package
                    if (!packageChanges.TryGetValue(package.Id, out var changes)) {
                        changes = new List<(string ItemCode, int Quantity, string Notes)>();
                        packageChanges[package.Id] = changes;
                    }

                    var notes = $"Pick list {absEntry} (PickEntry {pickEntry}) closed with {GetDocumentTypeName(followUpDoc.DocumentType)} #{followUpDoc.DocumentNumber}";
                    changes.Add((docItem.ItemCode, quantityToReduce, notes));

                    // Update the commitment lookup to track what we've processed
                    commitmentLookup[commitmentKey] -= quantityToReduce;

                    logger.LogDebug("Queued removal of {Quantity} units of {ItemCode} from package {PackageId} for PickEntry {PickEntry}, {DocumentType} #{DocumentNumber}",
                        quantityToReduce, docItem.ItemCode, package.Id, pickEntry,
                        GetDocumentTypeName(followUpDoc.DocumentType), followUpDoc.DocumentNumber);
                }
            }
        }

        // Now process all accumulated changes per package
        foreach (var (packageId, changes) in packageChanges) {
            // Group changes by item code to consolidate multiple removals
            var itemChanges = changes
            .GroupBy(c => c.ItemCode)
            .Select(g => new {
                ItemCode = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity),
                Notes = string.Join("; ", g.Select(x => x.Notes).Distinct())
            })
            .ToList();

            foreach (var itemChange in itemChanges) {
                var removeRequest = new RemoveItemFromPackageRequest {
                    PackageId = packageId,
                    ItemCode = itemChange.ItemCode,
                    Quantity = itemChange.TotalQuantity,
                    UnitType = UnitType.Unit,
                    UnitQuantity = itemChange.TotalQuantity,
                    SourceOperationType = ObjectType.PickingClosure,
                    SourceOperationId = Guid.NewGuid(), // Create new ID for this closure operation
                    Notes = itemChange.Notes
                };

                try {
                    // The PackageContentService will handle the transaction logging
                    await packageContentService.RemoveItemFromPackageAsync(removeRequest, userId);

                    logger.LogInformation("Successfully removed {Quantity} units of {ItemCode} from package {PackageId}",
                        itemChange.TotalQuantity, itemChange.ItemCode, packageId);
                }
                catch (InvalidOperationException ex) {
                    logger.LogWarning(ex, "Failed to remove item {ItemCode} from package {PackageId}: {Message}",
                        itemChange.ItemCode, packageId, ex.Message);
                    // Continue processing other items
                }
            }
        }

        // Update committed quantities in a single batch
        await UpdateCommittedQuantitiesInBatch(packageChanges.Keys.ToList());

        // Check all packages and update their status if empty
        foreach (var packageId in packageChanges.Keys) {
            var remainingContents = await packageContentService.GetPackageContentsAsync(packageId);
            if (!remainingContents.Any(pc => pc.Quantity > 0)) {
                var package = await db.Packages.FindAsync(packageId);
                if (package != null) {
                    package.Status = PackageStatus.Closed;
                    package.UpdatedAt = DateTime.UtcNow;
                    db.Packages.Update(package);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task UpdateCommittedQuantitiesInBatch(List<Guid> packageIds) {
        if (!packageIds.Any()) return;

        // Get fresh package contents to update committed quantities
        var packageContents = await db.PackageContents
        .Where(pc => packageIds.Contains(pc.PackageId))
        .ToListAsync();

        // Get all commitments for these packages
        var commitments = await db.PackageCommitments
        .Where(pc => packageIds.Contains(pc.PackageId) && pc.SourceOperationType == ObjectType.Picking)
        .GroupBy(pc => new { pc.PackageId, pc.ItemCode })
        .Select(g => new {
            g.Key.PackageId,
            g.Key.ItemCode,
            TotalCommitted = g.Sum(x => x.Quantity)
        })
        .ToListAsync();

        // Update committed quantities based on actual commitments
        foreach (var content in packageContents) {
            var commitment = commitments.FirstOrDefault(c =>
            c.PackageId == content.PackageId && c.ItemCode == content.ItemCode);

            content.CommittedQuantity = commitment?.TotalCommitted ?? 0;
            db.PackageContents.Update(content);
        }

        await db.SaveChangesAsync();
    }

    private string GetDocumentTypeName(int documentType) {
        return documentType switch {
            15 => "Delivery",
            16 => "Return",
            13 => "Invoice",
            14 => "Credit Note",
            67 => "Inventory Transfer",
            _ => $"Document Type {documentType}"
        };
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