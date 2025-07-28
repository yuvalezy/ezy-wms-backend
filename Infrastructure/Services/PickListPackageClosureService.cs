using Core.DTOs.Package;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListPackageClosureService(SystemDbContext db, IPackageContentService packageContentService, ILogger<PickListPackageClosureService> logger) {
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
            if (remainingContents.Any(pc => pc.Quantity > 0))
                continue;

            var package = await db.Packages.FindAsync(packageId);
            if (package == null)
                continue;

            package.Status = PackageStatus.Closed;
            package.UpdatedAt = DateTime.UtcNow;
            db.Packages.Update(package);
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

    private static string GetDocumentTypeName(int documentType) {
        return documentType switch {
            15 => "Delivery",
            16 => "Return",
            13 => "Invoice",
            14 => "Credit Note",
            67 => "Inventory Transfer",
            _ => $"Document Type {documentType}"
        };
    }
}