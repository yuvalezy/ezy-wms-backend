using Core.Constants;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickingRepackService(
    SystemDbContext db,
    ISettings settings,
    IExternalSystemAdapter adapter,
    IPickingPackageLabelService packageLabelService,
    ILogger<PickingRepackService> logger) : IPickingRepackService {
    public async Task<PickingRepackSummaryResponse> GetSummaryAsync(int absEntry, SessionInfo sessionInfo) {
        EnsureEnabled();
        return await BuildSummaryAsync(absEntry, sessionInfo.Warehouse);
    }

    public async Task<PickingRepackSummaryResponse> StartAsync(int absEntry, SessionInfo sessionInfo) {
        EnsureEnabled();
        await EnsureFullyPickedAsync(absEntry, sessionInfo.Warehouse);

        var existing = await GetActiveSession(absEntry, sessionInfo.Warehouse);
        if (existing != null) {
            await EnsureInitialLabelAsync(absEntry, sessionInfo);
            return await BuildSummaryAsync(absEntry, sessionInfo.Warehouse);
        }

        var session = new PickingRepackSession {
            Id = Guid.NewGuid(),
            AbsEntry = absEntry,
            WhsCode = sessionInfo.Warehouse,
            StartedByUserId = sessionInfo.Guid,
            StartedByUserName = sessionInfo.Name,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = sessionInfo.Guid
        };

        await db.PickingRepackSessions.AddAsync(session);
        await db.SaveChangesAsync();
        await EnsureInitialLabelAsync(absEntry, sessionInfo);

        return await BuildSummaryAsync(absEntry, sessionInfo.Warehouse);
    }

    public async Task<PickingRepackAssignResponse> AssignNextAsync(int absEntry, PickingRepackAssignRequest request, SessionInfo sessionInfo) {
        EnsureEnabled();
        await packageLabelService.ValidateForPickListAsync(request.PickingPackageLabelId, absEntry, sessionInfo.Warehouse);

        var session = await GetActiveSession(absEntry, sessionInfo.Warehouse);
        if (session == null) {
            return await Error(absEntry, sessionInfo.Warehouse, "Repack has not been started by a supervisor");
        }

        if (session is { IsCompleted: true }) {
            return await Error(absEntry, sessionInfo.Warehouse, "Repack is already completed");
        }

        var itemCode = request.ItemCode.Trim();
        var row = await db.PickLists
            .Where(p => p.AbsEntry == absEntry &&
                        p.ItemCode == itemCode &&
                        p.Unit == request.Unit &&
                        p.PickingPackageLabelId == null &&
                        p.SyncStatus != SyncStatus.ExternalCancel &&
                        (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .FirstOrDefaultAsync();

        if (row == null) {
            return await Error(absEntry, sessionInfo.Warehouse, $"No unassigned picked row found for item {itemCode} with unit {request.Unit}");
        }

        row.PickingPackageLabelId = request.PickingPackageLabelId;
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedByUserId = sessionInfo.Guid;
        await db.SaveChangesAsync();

        return new PickingRepackAssignResponse {
            Success = true,
            Summary = await BuildSummaryAsync(absEntry, sessionInfo.Warehouse)
        };
    }

    public async Task<PickingRepackSummaryResponse> CompleteAsync(int absEntry, SessionInfo sessionInfo) {
        EnsureEnabled();

        var session = await GetActiveSession(absEntry, sessionInfo.Warehouse);
        if (session == null) {
            throw new InvalidOperationException("No active repack session found");
        }

        var summary = await BuildSummaryAsync(absEntry, sessionInfo.Warehouse);
        if (summary.TotalLines == 0) {
            throw new InvalidOperationException("No picked rows found for repack");
        }

        if (summary.AssignedLines == 0) {
            throw new InvalidOperationException("At least one picked row must be assigned to a package label before completing repack");
        }

        session.IsCompleted = true;
        session.CompletedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedByUserId = sessionInfo.Guid;
        await db.SaveChangesAsync();

        logger.LogInformation("Picking repack completed for AbsEntry {AbsEntry} by {UserId}", absEntry, sessionInfo.Guid);
        return await BuildSummaryAsync(absEntry, sessionInfo.Warehouse);
    }

    public async Task<bool> IsReadyForSyncAsync(int absEntry, string warehouse) {
        if (!settings.Options.EnablePostPickRepack) {
            return true;
        }

        var sessionComplete = await db.PickingRepackSessions
            .AnyAsync(s => s.AbsEntry == absEntry &&
                           s.WhsCode == warehouse &&
                           s.IsCompleted &&
                           !s.IsCancelled &&
                           !s.Deleted);
        if (!sessionComplete) {
            return false;
        }

        return true;
    }

    private async Task<PickingRepackSummaryResponse> BuildSummaryAsync(int absEntry, string warehouse) {
        var session = await db.PickingRepackSessions
            .Where(s => s.AbsEntry == absEntry && s.WhsCode == warehouse && !s.Deleted)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

        var rows = await db.PickLists
            .Include(p => p.PickingPackageLabel)
            .Where(p => p.AbsEntry == absEntry &&
                        p.SyncStatus != SyncStatus.ExternalCancel &&
                        (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .ToArrayAsync();

        var labels = await packageLabelService.ListAsync(absEntry, warehouse);
        var assignedRows = rows.Where(p => p.PickingPackageLabelId != null).ToArray();

        return new PickingRepackSummaryResponse {
            PickListId = absEntry,
            Started = session != null && !session.IsCancelled,
            Completed = session?.IsCompleted == true,
            StartedAt = session?.StartedAt,
            StartedBy = session?.StartedByUserName,
            CompletedAt = session?.CompletedAt,
            TotalLines = rows.Length,
            AssignedLines = assignedRows.Length,
            TotalQuantity = rows.Sum(p => p.ScannedQuantity ?? p.Quantity),
            AssignedQuantity = assignedRows.Sum(p => p.ScannedQuantity ?? p.Quantity),
            Labels = labels.ToList(),
            Items = rows
                .GroupBy(p => new { p.ItemCode, p.Unit })
                .OrderBy(g => g.Key.ItemCode)
                .ThenBy(g => g.Key.Unit)
                .Select(g => {
                    var assigned = g.Where(p => p.PickingPackageLabelId != null).ToArray();
                    return new PickingRepackItemResponse {
                        ItemCode = g.Key.ItemCode,
                        Unit = g.Key.Unit,
                        TotalLines = g.Count(),
                        AssignedLines = assigned.Length,
                        TotalQuantity = g.Sum(p => p.ScannedQuantity ?? p.Quantity),
                        AssignedQuantity = assigned.Sum(p => p.ScannedQuantity ?? p.Quantity)
                    };
                })
                .ToList()
        };
    }

    private async Task<PickingRepackAssignResponse> Error(int absEntry, string warehouse, string message) => new() {
        Success = false,
        ErrorMessage = message,
        Summary = await BuildSummaryAsync(absEntry, warehouse)
    };

    private async Task<PickingRepackSession?> GetActiveSession(int absEntry, string warehouse) =>
        await db.PickingRepackSessions
            .OrderBy(s => s.IsCompleted)
            .ThenByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(s => s.AbsEntry == absEntry &&
                                      s.WhsCode == warehouse &&
                                      !s.IsCancelled &&
                                      !s.Deleted);

    private async Task EnsureFullyPickedAsync(int absEntry, string warehouse) {
        var pick = (await adapter.GetPickListsAsync(new PickListsRequest { ID = absEntry }, warehouse)).FirstOrDefault();
        if (pick == null) {
            throw new InvalidOperationException($"Pick list {absEntry} was not found");
        }

        var pickedQuantity = await db.PickLists
            .Where(p => p.AbsEntry == absEntry &&
                        p.SyncStatus != SyncStatus.ExternalCancel &&
                        (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .SumAsync(p => p.Quantity);

        var openQuantity = pick.OpenQuantity - pickedQuantity;
        if (openQuantity >= QuantityTolerances.Completed) {
            throw new InvalidOperationException("Repack can only be started after picking is fully completed");
        }
    }

    private async Task EnsureInitialLabelAsync(int absEntry, SessionInfo sessionInfo) {
        var labels = await packageLabelService.ListAsync(absEntry, sessionInfo.Warehouse);
        if (labels.Count == 0) {
            await packageLabelService.CreateNextAsync(absEntry, sessionInfo);
        }
    }

    private void EnsureEnabled() {
        if (!settings.Options.EnablePostPickRepack) {
            throw new InvalidOperationException("Post-pick repack is not enabled");
        }

        if (!settings.Options.EnablePickingPackageLabels) {
            throw new InvalidOperationException("Picking package labels must be enabled to use post-pick repack");
        }
    }
}
