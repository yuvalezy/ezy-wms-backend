using Core.DTOs.PickList;
using Core.DTOs.Transfer;
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

public class PickListCancelService(
    SystemDbContext                     db,
    IPickListProcessService             pickListProcessService,
    ITransferService                    transferService,
    ITransferLineService                transferLineService,
    ITransferPackageService             transferPackageService,
    IExternalSystemAdapter              adapter,
    ISettings                           settings,
    ILogger<PickListCancelService>      logger) : IPickListCancelService {
    
    public async Task<ProcessPickListCancelResponse> CancelPickListAsync(int absEntry, SessionInfo sessionInfo) {
        // Process the picking in case something has not been synced into SAP B1
        var response = await pickListProcessService.ProcessPickList(absEntry, sessionInfo.Guid);
        if (response.Status != ResponseStatus.Ok && response.ErrorMessage != $"No open pick list items found for AbsEntry {absEntry}") {
            return response.ToDto();
        }

        // Get cancel bin entry from settings
        int cancelBinEntry = settings.Filters.CancelPickingBinEntry;
        if (cancelBinEntry == 0) {
            throw new Exception("Cancel Picking Bin Entry is not set in the Settings.Filters.CancelPickingBinEntry");
        }

        // Get current picked data from SAP
        var selection = (await adapter.GetPickingSelection(absEntry)).ToArray();

        // Cancel Pick List in SAP
        response = await adapter.CancelPickList(absEntry, selection, sessionInfo.Warehouse, cancelBinEntry);
        if (selection.Length == 0)
            return response.ToDto();

        // Create a new transfer for cancelled pick list items
        var transfer = await transferService.CreateTransfer(new CreateTransferRequest {
            Name = $"Cancelación Picking {absEntry}", // TODO: multi language
            Comments = "Reubicación de artículos de picking"
        }, sessionInfo);

        // Handle packages first, then regular items
        var processedItems = await HandlePackageCancellation(absEntry, transfer.Id, cancelBinEntry, sessionInfo);
        await HandleRegularItemCancellation(selection, processedItems, transfer.Id, cancelBinEntry, sessionInfo);

        return response.ToDto(transfer.Id);
    }

    private async Task<HashSet<string>> HandlePackageCancellation(int absEntry, Guid transferId, int cancelBinEntry, SessionInfo sessionInfo) {
        var processedItems = new HashSet<string>(); // Track items handled by packages

        if (!settings.Options.EnablePackages) {
            return processedItems;
        }

        try {
            var pickListPackages = await db.PickListPackages
                .Where(plp => plp.AbsEntry == absEntry)
                .Include(plp => plp.Package)
                .ThenInclude(p => p.Contents)
                .Include(plp => plp.Package.Commitments)
                .ToListAsync();

            // Group by PackageId to handle each package once
            var packageGroups = pickListPackages.GroupBy(plp => plp.PackageId);

            foreach (var packageGroup in packageGroups) {
                await ProcessPackageGroup(packageGroup, transferId, cancelBinEntry, absEntry, sessionInfo, processedItems);
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to load pick list packages for pick list cancellation {AbsEntry}, falling back to item-based processing", absEntry);
        }

        return processedItems;
    }

    private async Task ProcessPackageGroup(IGrouping<Guid, PickListPackage> packageGroup, Guid transferId, int cancelBinEntry, int absEntry, SessionInfo sessionInfo, HashSet<string> processedItems) {
        try {
            var packageId = packageGroup.Key;
            var package = packageGroup.First().Package;

            // Get picking commitments for this package
            var commitments = await GetPickingCommitments(packageId, absEntry);

            if (!commitments.Any()) {
                return;
            }

            if (IsFullPackageCommitment(package, commitments)) {
                await HandleFullPackageCommitment(packageId, transferId, cancelBinEntry, sessionInfo, package, processedItems, absEntry);
            }
            else {
                await HandlePartialPackageCommitment(commitments, transferId, cancelBinEntry, sessionInfo, processedItems, absEntry);
            }

            // Clean up package commitments
            await ClearPackageCommitments(commitments, sessionInfo);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to process package {PackageId} during pick list cancellation {AbsEntry}",
                packageGroup.Key, absEntry);
        }
    }

    private async Task HandleFullPackageCommitment(Guid packageId, Guid transferId, int cancelBinEntry, SessionInfo sessionInfo, Package package, HashSet<string> processedItems, int absEntry) {
        try {
            await transferPackageService.HandleSourcePackageScanAsync(new TransferAddSourcePackageRequest {
                TransferId = transferId,
                PackageId = packageId,
                BinEntry = cancelBinEntry
            }, sessionInfo);

            // Mark all package items as processed to avoid double-processing
            foreach (var content in package.Contents) {
                processedItems.Add(content.ItemCode);
            }

            logger.LogInformation("Added full package {PackageBarcode} to transfer for pick list cancellation {AbsEntry}",
                package.Barcode, absEntry);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to add package {PackageId} to transfer during pick list cancellation {AbsEntry}, will fall back to item-based processing",
                packageId, absEntry);
        }
    }

    private async Task HandlePartialPackageCommitment(IEnumerable<PackageCommitment> commitments, Guid transferId, int cancelBinEntry, SessionInfo sessionInfo, HashSet<string> processedItems, int absEntry) {
        foreach (var commitment in commitments) {
            try {
                // Calculate units based on committed quantity
                decimal quantity = commitment.Quantity;

                // For now, treat as units - could be enhanced to calculate packs/dozens
                var addRequest = new TransferAddItemRequest {
                    ID = transferId,
                    ItemCode = commitment.ItemCode,
                    BarCode = commitment.ItemCode,
                    Type = SourceTarget.Source,
                    BinEntry = cancelBinEntry,
                    Quantity = (int)quantity,
                    Unit = UnitType.Unit
                };

                await transferLineService.AddItem(sessionInfo, addRequest);
                processedItems.Add(commitment.ItemCode);

                logger.LogInformation("Added partial package item {ItemCode} quantity {Quantity} to transfer for pick list cancellation {AbsEntry}",
                    commitment.ItemCode, quantity, absEntry);
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Failed to add partial package item {ItemCode} to transfer during pick list cancellation {AbsEntry}",
                    commitment.ItemCode, absEntry);
            }
        }
    }

    private async Task HandleRegularItemCancellation(dynamic[] selection, HashSet<string> processedItems, Guid transferId, int cancelBinEntry, SessionInfo sessionInfo) {
        // Add items into transfer (only for items not handled by packages)
        var items = selection
            .Where(s => !processedItems.Contains(s.ItemCode)) // Skip items already handled by packages
            .GroupBy(v => new { v.ItemCode, BarCode = v.CodeBars, v.NumInBuy, v.PackUn })
            .Select(v => new { v.Key.ItemCode, v.Key.BarCode, v.Key.NumInBuy, v.Key.PackUn, Quantity = v.Sum(w => w.Quantity) });

        foreach (var item in items) {
            await ProcessRegularItem(item, transferId, cancelBinEntry, sessionInfo);
        }
    }

    private async Task ProcessRegularItem(dynamic item, Guid transferId, int cancelBinEntry, SessionInfo sessionInfo) {
        var addRequest = new TransferAddItemRequest {
            ID = transferId,
            ItemCode = item.ItemCode,
            BarCode = item.BarCode,
            Type = SourceTarget.Source,
        };

        decimal quantity = item.Quantity;
        decimal numInBuy = item.NumInBuy;
        decimal packUn = item.PackUn;

        // Calculate packs
        int packs = (int)Math.Floor(quantity / (numInBuy * packUn));

        // Calculate dozens
        int remainderAfterPacks = (int)(quantity - packs * numInBuy * packUn);
        int dozens = (int)Math.Floor(remainderAfterPacks / numInBuy);

        // Calculate units
        int units = (int)(remainderAfterPacks % numInBuy);

        addRequest.BinEntry = cancelBinEntry;

        if (packs > 0) {
            addRequest.Quantity = packs;
            addRequest.Unit = UnitType.Pack;
            await transferLineService.AddItem(sessionInfo, addRequest);
        }

        if (dozens > 0) {
            addRequest.Quantity = dozens;
            addRequest.Unit = UnitType.Dozen;
            await transferLineService.AddItem(sessionInfo, addRequest);
        }

        if (units > 0) {
            addRequest.Quantity = units;
            addRequest.Unit = UnitType.Unit;
            await transferLineService.AddItem(sessionInfo, addRequest);
        }
    }

    private bool IsFullPackageCommitment(Package package, IEnumerable<PackageCommitment> commitments) {
        // Check if all package content is committed to picking
        var totalCommittedQuantity = commitments.Sum(c => c.Quantity);
        var totalPackageQuantity = package.Contents.Sum(pc => pc.Quantity);

        return Math.Abs(totalCommittedQuantity - totalPackageQuantity) < 0.001m; // Use small epsilon for decimal comparison
    }

    private async Task ClearPackageCommitments(IEnumerable<PackageCommitment> commitments, SessionInfo sessionInfo) {
        if (!commitments.Any()) return;

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            foreach (var commitment in commitments) {
                // Find the corresponding package content and reduce committed quantity
                var packageContent = await db.PackageContents
                    .FirstOrDefaultAsync(pc => pc.PackageId == commitment.PackageId &&
                                              pc.ItemCode == commitment.ItemCode);

                if (packageContent != null) {
                    packageContent.CommittedQuantity -= commitment.Quantity;
                    packageContent.UpdatedAt = DateTime.UtcNow;
                    packageContent.UpdatedByUserId = sessionInfo.Guid;
                    db.PackageContents.Update(packageContent);
                }

                // Remove the commitment record
                db.PackageCommitments.Remove(commitment);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed to clear package commitments for {CommitmentCount} commitments", commitments.Count());
            throw;
        }
    }

    private async Task<IEnumerable<PackageCommitment>> GetPickingCommitments(Guid packageId, int absEntry) {
        // Get commitments for this package related to the specific pick operation
        // Note: We need to find a way to link absEntry to the actual pick operation ID
        // For now, we'll get all picking commitments for this package and let the caller filter
        return await db.PackageCommitments
            .Where(pc => pc.PackageId == packageId &&
                        pc.SourceOperationType == ObjectType.Picking)
            .ToListAsync();
    }
}