using Core.DTOs.PickList;
using Core.DTOs.Transfer;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListProcessService(
    SystemDbContext                 db,
    ITransferService                transferService,
    ITransferLineService            transferLineService,
    IExternalSystemAdapter          adapter,
    ISettings                       settings,
    ILogger<PickListProcessService> logger) : IPickListProcessService {
    public async Task<ProcessPickListResponse> ProcessPickList(int absEntry, Guid userId) {
        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Load local pick list data
            var pickLists = await db.PickLists
                .Where(p => p.AbsEntry == absEntry && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
                .ToListAsync();

            if (!pickLists.Any()) {
                throw new InvalidOperationException($"No open pick list items found for AbsEntry {absEntry}");
            }

            // Update status to Processing and SyncStatus to Processing
            foreach (var pickList in pickLists) {
                pickList.Status          = ObjectStatus.Processing;
                pickList.SyncStatus      = SyncStatus.Processing;
                pickList.UpdatedAt       = DateTime.UtcNow;
                pickList.UpdatedByUserId = userId;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed to mark pick lists as processing for AbsEntry {AbsEntry}", absEntry);
            return new ProcessPickListResponse {
                Status       = ResponseStatus.Error,
                Message      = "Failed to process pick list",
                ErrorMessage = ex.Message
            };
        }

        try {
            // Prepare data for SAP B1
            var pickingData = await db.PickLists
                .Where(p => p.AbsEntry == absEntry && p.Status == ObjectStatus.Processing && p.SyncStatus == SyncStatus.Processing)
                .ToListAsync();

            // Call external system to process the pick list
            var result = await adapter.ProcessPickList(absEntry, pickingData);

            if (result.Success) {
                // Update pick lists to Closed and Synced
                await UpdatePickListsSyncStatus(absEntry, SyncStatus.Synced, null, userId);

                return new ProcessPickListResponse {
                    Status         = ResponseStatus.Ok,
                    DocumentNumber = result.DocumentNumber
                };
            }

            // Update pick lists to Failed
            await UpdatePickListsSyncStatus(absEntry, SyncStatus.Failed, result.ErrorMessage, userId);

            return new ProcessPickListResponse {
                Status       = ResponseStatus.Error,
                Message      = "Failed to process pick list",
                ErrorMessage = result.ErrorMessage
            };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to process pick list for AbsEntry {AbsEntry}", absEntry);

            // Update pick lists to Failed
            await UpdatePickListsSyncStatus(absEntry, SyncStatus.Failed, ex.Message, userId);

            return new ProcessPickListResponse {
                Status       = ResponseStatus.Error,
                Message      = "Failed to process pick list",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task SyncPendingPickLists() {
        try {
            // Get distinct pick entries with pending or failed sync status
            var pendingPickEntries = await db.PickLists
                .Where(p => (p.SyncStatus == SyncStatus.Pending || p.SyncStatus == SyncStatus.Failed) &&
                            (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
                .Select(p => p.AbsEntry)
                .Distinct()
                .ToArrayAsync();

            if (pendingPickEntries.Length == 0) {
                logger.LogDebug("No pending pick lists to sync");
                return;
            }

            logger.LogInformation("Found {Count} pick lists to sync", pendingPickEntries.Length);

            // Process each pick entry sequentially
            foreach (var absEntry in pendingPickEntries) {
                logger.LogInformation("Processing pick list AbsEntry {AbsEntry}", absEntry);

                // Check if pick lists are still pending/failed before processing
                var hasPendingItems = await db.PickLists
                    .AnyAsync(p => p.AbsEntry == absEntry &&
                                   (p.SyncStatus == SyncStatus.Pending || p.SyncStatus == SyncStatus.Failed) &&
                                   (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing));

                if (!hasPendingItems) {
                    logger.LogDebug("No pending pick lists found for AbsEntry {AbsEntry}", absEntry);
                    continue;
                }

                // Use the existing ProcessPickList method with system user ID
                var result = await ProcessPickList(absEntry, DatabaseExtensions.SystemUserId);

                if (result.Status == ResponseStatus.Ok) {
                    logger.LogInformation("Successfully synced pick list AbsEntry {AbsEntry}", absEntry);
                }
                else {
                    logger.LogWarning("Failed to sync pick list AbsEntry {AbsEntry}: {Error}", absEntry, result.ErrorMessage);
                }
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during SyncPendingPickLists");
            throw;
        }
    }

    private async Task UpdatePickListsSyncStatus(int absEntry, SyncStatus syncStatus, string? errorMessage, Guid? userId) {
        var transaction = await db.Database.BeginTransactionAsync();
        try {
            var pickLists = await db.PickLists
                .Where(p => p.AbsEntry == absEntry && p.SyncStatus == SyncStatus.Processing)
                .ToListAsync();

            foreach (var pickList in pickLists) {
                pickList.SyncStatus = syncStatus;
                pickList.UpdatedAt  = DateTime.UtcNow;

                if (syncStatus == SyncStatus.Synced) {
                    pickList.Status    = ObjectStatus.Closed;
                    pickList.SyncedAt  = DateTime.UtcNow;
                    pickList.SyncError = null;
                }
                else if (syncStatus == SyncStatus.Failed) {
                    pickList.Status    = ObjectStatus.Open; // Revert to Open on failure
                    pickList.SyncError = errorMessage;
                }

                if (userId.HasValue) {
                    pickList.UpdatedByUserId = userId.Value;
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed to update sync status for AbsEntry {AbsEntry}", absEntry);
            throw;
        }
    }

    public async Task<ProcessPickListCancelResponse> CancelPickList(int absEntry, SessionInfo sessionInfo) {
        //Process the picking in case something has not been synced into sbo
        var response = await ProcessPickList(absEntry, sessionInfo.Guid);
        if (response.Status != ResponseStatus.Ok && response.ErrorMessage != $"No open pick list items found for AbsEntry {absEntry}") {
            return response.ToDto();
        }

        //Get a picked selection for bin locations
        int cancelBinEntry = settings.Filters.CancelPickingBinEntry;
        if (cancelBinEntry == 0) {
            throw new Exception($"Initial Counting Bin Entry is not set in the Settings.Filters.CancelPickingBinEntry");
        }

        //Get current picked data
        var selection = (await adapter.GetPickingSelection(absEntry)).ToArray();

        //Cancel Pick List
        response = await adapter.CancelPickList(absEntry, selection, sessionInfo.Warehouse, cancelBinEntry);
        if (selection.Length == 0)
            return response.ToDto();

        //Create a new transfer with source selection for cancelled pick list
        var transfer = await transferService.CreateTransfer(new CreateTransferRequest {
            Name     = $"Cancelación Picking {absEntry}", //TODO: multi language
            Comments = "Reubicación de artículos de picking"
        }, sessionInfo);

        //Add items into transfer
        var items = selection
            .GroupBy(v => new { v.ItemCode, BarCode                                                  = v.CodeBars, v.NumInBuy, v.PackUn })
            .Select(v => new { v.Key.ItemCode, v.Key.BarCode, v.Key.NumInBuy, v.Key.PackUn, Quantity = v.Sum(w => w.Quantity) });
        foreach (var item in items) {
            var addRequest = new TransferAddItemRequest {
                //TODO: add additional validations, don't know which yet
                ID       = transfer.Id,
                ItemCode = item.ItemCode,
                BarCode  = item.BarCode,
                Type     = SourceTarget.Source,
            };

            decimal quantity = item.Quantity;
            decimal numInBuy = item.NumInBuy;
            decimal packUn   = item.PackUn;
            //Calculate packs
            int packs = (int)Math.Floor(quantity / (numInBuy * packUn));

            //Calculate dozens
            int remainderAfterPacks = (int)(quantity - packs * numInBuy * packUn);
            int dozens              = (int)Math.Floor(remainderAfterPacks / numInBuy);

            //Calculate units
            int units = (int)(remainderAfterPacks % numInBuy);

            addRequest.BinEntry = cancelBinEntry;
            if (packs > 0) {
                addRequest.Quantity = packs;
                addRequest.Unit     = UnitType.Pack;
                await transferLineService.AddItem(sessionInfo, addRequest);
            }

            if (dozens > 0) {
                addRequest.Quantity = dozens;
                addRequest.Unit     = UnitType.Dozen;
                await transferLineService.AddItem(sessionInfo, addRequest);
            }

            if (units > 0) {
                addRequest.Quantity = units;
                addRequest.Unit     = UnitType.Unit;
                await transferLineService.AddItem(sessionInfo, addRequest);
            }
        }

        return response.ToDto(transfer.Id);
    }
}