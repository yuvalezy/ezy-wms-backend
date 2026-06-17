using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListProcessService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    IPickingPostProcessorFactory postProcessorFactory,
    IServiceProvider serviceProvider,
    IExternalSystemAlertService alertService,
    ISettings settings,
    ILogger<PickListProcessService> logger) : IPickListProcessService {
    public async Task<ProcessPickListResponse> ProcessPickList(int absEntry, Guid userId) {
        if (!await IsPostPickRepackReadyAsync(absEntry)) {
            return new ProcessPickListResponse {
                Status = ResponseStatus.Error,
                Message = "Failed to process pick list",
                ErrorMessage = "Post-pick repack must be completed before syncing this pick list"
            };
        }

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Load local pick list data
            var pickLists = await db.PickLists
            .Where(p => p.AbsEntry == absEntry && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing) && p.SyncStatus != SyncStatus.ExternalCancel)
            .ToListAsync();

            if (!pickLists.Any()) {
                throw new InvalidOperationException($"No open pick list items found for AbsEntry {absEntry}");
            }

            // Update status to Processing and SyncStatus to Processing
            foreach (var pickList in pickLists) {
                pickList.Status = ObjectStatus.Processing;
                pickList.SyncStatus = SyncStatus.Processing;
                pickList.UpdatedAt = DateTime.UtcNow;
                pickList.UpdatedByUserId = userId;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed to mark pick lists as processing for AbsEntry {AbsEntry}", absEntry);
            return new ProcessPickListResponse {
                Status = ResponseStatus.Error,
                Message = "Failed to process pick list",
                ErrorMessage = ex.Message
            };
        }

        try {
            // Prepare data for SAP B1
            var pickingData = await db.PickLists
            .Include(p => p.PickingPackageLabel)
            .Where(p => p.AbsEntry == absEntry && p.Status == ObjectStatus.Processing && p.SyncStatus == SyncStatus.Processing)
            .ToListAsync();

            // Get alert recipients
            var alertRecipients = await alertService.GetAlertRecipientsAsync(AlertableObjectType.PickList);

            // Call external system to process the pick list
            var result = await adapter.ProcessPickList(absEntry, pickingData, alertRecipients);

            if (result.Success) {
                // Update pick lists to Closed and Synced
                await UpdatePickListsSyncStatus(absEntry, SyncStatus.Synced, null, userId);

                // Execute post-processing hooks
                await ExecutePostProcessors(absEntry, pickingData);

                return new ProcessPickListResponse {
                    Status = ResponseStatus.Ok,
                    DocumentNumber = result.DocumentNumber
                };
            }

            if (result.ErrorMessage == "Cannot process document if the Status is closed") {
                await UpdatePickListsSyncStatus(absEntry, SyncStatus.ExternalCancel, result.ErrorMessage, userId);
                return new ProcessPickListResponse {
                    Status = ResponseStatus.Error,
                    Message = "Failed to process pick list",
                    ErrorMessage = result.ErrorMessage
                };
            }

            // Update pick lists to Failed
            await UpdatePickListsSyncStatus(absEntry, SyncStatus.Failed, result.ErrorMessage, userId);

            return new ProcessPickListResponse {
                Status = ResponseStatus.Error,
                Message = "Failed to process pick list",
                ErrorMessage = result.ErrorMessage
            };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to process pick list for AbsEntry {AbsEntry}", absEntry);

            // Update pick lists to Failed
            await UpdatePickListsSyncStatus(absEntry, SyncStatus.Failed, ex.Message, userId);

            return new ProcessPickListResponse {
                Status = ResponseStatus.Error,
                Message = "Failed to process pick list",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task SyncPendingPickLists() {
        try {
            // Get distinct pick entries with pending or failed sync status
            var pendingPickEntries = await db.PickLists
            .Where(p => (p.SyncStatus == SyncStatus.Pending || p.SyncStatus == SyncStatus.Failed) &&
                        (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing)
                        && p.BinEntry != null)
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
                if (!await IsPostPickRepackReadyAsync(absEntry)) {
                    // Post-pick repack isn't complete, so we won't create the SAP document yet.
                    // But external cancellation must still be honoured: if the pick list was
                    // cancelled/closed in SAP, clear the local rows so their picked quantity
                    // stops consuming open quantity. Without this, enabling post-pick repack
                    // starves the cancellation detection that otherwise runs in ProcessPickList.
                    if (await IsExternallyCancelledAsync(absEntry)) {
                        await ClearExternallyCancelledPickListAsync(absEntry);
                        logger.LogInformation("Pick list AbsEntry {AbsEntry} cancelled/closed in SAP; cleared local rows as ExternalCancel", absEntry);
                    }
                    else {
                        logger.LogInformation("Skipping pick list AbsEntry {AbsEntry} because post-pick repack is not complete", absEntry);
                    }
                    continue;
                }

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

    private async Task<bool> IsExternallyCancelledAsync(int absEntry) {
        var statuses = await adapter.GetPickListStatuses([absEntry]);
        // GetPickListStatuses returns IsOpen=false when the SAP pick list is closed,
        // cancelled, or no longer exists.
        return statuses.TryGetValue(absEntry, out var isOpen) && !isOpen;
    }

    private async Task ClearExternallyCancelledPickListAsync(int absEntry) {
        var rows = await db.PickLists
            .Where(p => p.AbsEntry == absEntry &&
                        (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing) &&
                        p.SyncStatus != SyncStatus.ExternalCancel)
            .ToListAsync();

        if (rows.Count == 0) {
            return;
        }

        foreach (var row in rows) {
            row.SyncStatus = SyncStatus.ExternalCancel;
            row.SyncError = "Pick list cancelled or closed in SAP";
            row.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    private async Task<bool> IsPostPickRepackReadyAsync(int absEntry) {
        if (!settings.Options.EnablePostPickRepack) {
            return true;
        }

        // A partial pack is valid and may be synced: at least one picked row must be
        // assigned to a package label. Labels may be assigned during picking (pre-pack)
        // or afterwards via a post-pick repack session — either path satisfies this. The
        // repack session start/end is operational only and is never a sync precondition.
        return await db.PickLists
            .AnyAsync(p => p.AbsEntry == absEntry &&
                           p.PickingPackageLabelId != null &&
                           p.SyncStatus != SyncStatus.ExternalCancel &&
                           (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing));
    }

    private async Task UpdatePickListsSyncStatus(int absEntry, SyncStatus syncStatus, string? errorMessage, Guid? userId) {
        var transaction = await db.Database.BeginTransactionAsync();
        try {
            var pickLists = await db.PickLists
            .Where(p => p.AbsEntry == absEntry && p.SyncStatus == SyncStatus.Processing)
            .ToListAsync();

            foreach (var pickList in pickLists) {
                pickList.SyncStatus = syncStatus;
                pickList.UpdatedAt = DateTime.UtcNow;

                if (syncStatus == SyncStatus.Synced) {
                    pickList.Status = ObjectStatus.Closed;
                    pickList.SyncedAt = DateTime.UtcNow;
                    pickList.SyncError = null;
                }
                else if (syncStatus == SyncStatus.Failed) {
                    pickList.Status = ObjectStatus.Open; // Revert to Open on failure
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

    private async Task ExecutePostProcessors(int absEntry, List<Core.Entities.PickList> processedData) {
        try {
            var enabledProcessors = postProcessorFactory.GetEnabledProcessors();

            if (!enabledProcessors.Any()) {
                logger.LogDebug("No enabled post-processors found for pick list {AbsEntry}", absEntry);
                return;
            }

            foreach (var processor in enabledProcessors) {
                try {
                    logger.LogInformation("Executing post-processor {ProcessorId} for pick list {AbsEntry}", processor.Id, absEntry);

                    var context = new PickingPostProcessorContext {
                        AbsEntry = absEntry,
                        ProcessedData = processedData,
                        Configuration = GetProcessorConfiguration(processor.Id),
                        Logger = logger,
                        ServiceProvider = serviceProvider
                    };

                    await processor.ExecuteAsync(context);
                    logger.LogInformation("Successfully executed post-processor {ProcessorId} for pick list {AbsEntry}", processor.Id, absEntry);
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Failed to execute post-processor {ProcessorId} for pick list {AbsEntry}", processor.Id, absEntry);
                    // Continue with other processors even if one fails
                }
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during post-processor execution for pick list {AbsEntry}", absEntry);
            // Don't rethrow - post-processing failures shouldn't break the main flow
        }
    }

    private Dictionary<string, object>? GetProcessorConfiguration(string processorId) {
        var processorSettings = serviceProvider.GetRequiredService<ISettings>()
        .PickingPostProcessingProcessors
        .FirstOrDefault(p => p.Id == processorId);

        return processorSettings?.Configuration;
    }
}
