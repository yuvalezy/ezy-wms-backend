using System.Data;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PickingPackageLabelService(SystemDbContext db, ISettings settings) : IPickingPackageLabelService {
    public async Task<IReadOnlyList<PickingPackageLabelResponse>> ListAsync(int absEntry, string warehouse) {
        EnsureEnabled();

        var labels = await db.PickingPackageLabels
            .Include(label => label.PickLists)
            .AsSplitQuery()
            .Where(label => label.AbsEntry == absEntry && label.WhsCode == warehouse)
            .OrderBy(label => label.Sequence)
            .ToListAsync();

        return labels.Select(ToResponse).ToList();
    }

    public async Task<PickingPackageLabelResponse> CreateNextAsync(int absEntry, SessionInfo sessionInfo) {
        EnsureEnabled();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var nextSequence = await db.PickingPackageLabels
            .Where(label => label.AbsEntry == absEntry && label.WhsCode == sessionInfo.Warehouse)
            .MaxAsync(label => (int?)label.Sequence) ?? 0;
        nextSequence++;

        var label = new PickingPackageLabel {
            Id = Guid.NewGuid(),
            AbsEntry = absEntry,
            WhsCode = sessionInfo.Warehouse,
            Code = $"{GetPrefix()}{nextSequence}",
            Sequence = nextSequence,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = sessionInfo.Guid
        };

        await db.PickingPackageLabels.AddAsync(label);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToResponse(label);
    }

    public async Task ValidateForPickListAsync(Guid labelId, int absEntry, string warehouse) {
        EnsureEnabled();

        var isValid = await db.PickingPackageLabels.AnyAsync(label =>
            label.Id == labelId &&
            label.AbsEntry == absEntry &&
            label.WhsCode == warehouse);

        if (!isValid) {
            throw new InvalidOperationException("Picking package label does not belong to this pick list and warehouse");
        }
    }

    private void EnsureEnabled() {
        if (!settings.Options.EnablePickingPackageLabels) {
            throw new InvalidOperationException("Picking package labels are not enabled");
        }
    }

    private string GetPrefix() =>
        string.IsNullOrWhiteSpace(settings.Options.PickingPackageLabelPrefix)
            ? "R"
            : settings.Options.PickingPackageLabelPrefix.Trim();

    private static PickingPackageLabelResponse ToResponse(PickingPackageLabel label) {
        var activeRows = label.PickLists
            .Where(p => (p.Status == Core.Enums.ObjectStatus.Open || p.Status == Core.Enums.ObjectStatus.Processing) &&
                        p.SyncStatus != Core.Enums.SyncStatus.ExternalCancel)
            .ToArray();

        return new PickingPackageLabelResponse {
            Id = label.Id,
            AbsEntry = label.AbsEntry,
            WhsCode = label.WhsCode,
            Code = label.Code,
            Sequence = label.Sequence,
            CreatedAt = label.CreatedAt,
            LineCount = activeRows.Length,
            TotalQuantity = activeRows.Sum(p => p.ScannedQuantity ?? p.Quantity),
            Items = activeRows
                .GroupBy(p => new { p.ItemCode, p.Unit, p.BinEntry })
                .OrderBy(g => g.Key.ItemCode)
                .Select(g => new PickingPackageLabelItemResponse {
                    ItemCode = g.Key.ItemCode,
                    Unit = g.Key.Unit,
                    BinEntry = g.Key.BinEntry,
                    LineCount = g.Count(),
                    ScannedQuantity = g.Sum(p => p.ScannedQuantity ?? p.Quantity),
                    BaseQuantity = g.Sum(p => p.Quantity)
                })
                .ToList()
        };
    }
}
