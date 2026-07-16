using Core.DTOs.Items;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <inheritdoc cref="ITransferQuantityRepairService"/>
public class TransferQuantityRepairService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ILogger<TransferQuantityRepairService> logger)
    : QuantityRepairServiceBase(db, adapter, logger), ITransferQuantityRepairService {
    public async Task<int> RepairAsync(Guid transferId) {
        // Both sides, filtered exactly as TransferDocumentService.GetProcessInfo filters them — the balance
        // check below has to be the same arithmetic the database will do when it decides IsComplete.
        var lines = await Db.TransferLines
            .Where(line => line.TransferId == transferId && line.LineStatus != LineStatus.Closed)
            .ToArrayAsync();

        var repairable = lines.Where(line => line.ScannedQuantity is > 0m).ToArray();
        if (repairable.Length == 0)
            return 0;

        var items = await TryLoadItemsAsync(repairable.Select(line => line.ItemCode));
        if (items == null)
            return 0;

        int repaired = RestateBalanced(transferId, lines, items, Logger);

        if (repaired > 0) {
            logger.LogInformation("Transfer {TransferId}: restated {Count} line(s) after a conversion factor change", transferId, repaired);
            await Db.SaveChangesAsync();
        }

        return repaired;
    }

    /// <summary>
    /// Restates every line it can, item by item, but never at the cost of the source/target balance.
    /// Mutates <paramref name="lines"/> in place and returns how many actually changed.
    /// </summary>
    /// <remarks>
    /// Pure: no database, no SAP. Split out from <see cref="RepairAsync"/> so the balance guard — the part with
    /// the interesting failure mode — can be tested on its own.
    /// </remarks>
    /// <param name="lines">All non-closed lines of the transfer, <b>both</b> source and target.</param>
    public static int RestateBalanced(
        Guid transferId,
        IReadOnlyCollection<TransferLine> lines,
        IReadOnlyDictionary<string, ItemCheckResponse> items,
        ILogger logger) {
        int repaired = 0;

        foreach (var itemGroup in lines.GroupBy(line => line.ItemCode)) {
            var group = itemGroup.ToArray();

            // Snapshot before touching anything: the whole item moves together or not at all.
            var storedQuantities = group.ToDictionary(line => line.Id, line => line.Quantity);
            bool wasBalanced = IsBalanced(group);

            // The whole group, not just the repairable lines — a line with no scanned count still counts toward
            // the balance. Restate skips it.
            int changed = QuantityRestatement.Restate(group, items, applyFactor: true, logger);
            if (changed == 0)
                continue;

            if (wasBalanced && !IsBalanced(group)) {
                foreach (var line in group)
                    line.Quantity = storedQuantities[line.Id];

                logger.LogWarning(
                    "Transfer {TransferId} item {ItemCode}: skipped restating {Count} line(s) — the current factor would leave source and target out of balance, most likely because one side was scanned in packs and the other in dozens. The lines keep their stored quantities and the transfer stays processable; re-scan the item to adopt the new factor.",
                    transferId, itemGroup.Key, changed);
                continue;
            }

            repaired += changed;
        }

        return repaired;
    }

    /// <summary>
    /// The invariant enforced in SQL by <c>TransferDocumentService.GetProcessInfo</c>: per item, what left the
    /// source must equal what reached the target. A transfer that fails it cannot be processed.
    /// </summary>
    private static bool IsBalanced(IReadOnlyCollection<TransferLine> lines) =>
        lines.Where(line => line.Type == SourceTarget.Source).Sum(line => line.Quantity) ==
        lines.Where(line => line.Type == SourceTarget.Target).Sum(line => line.Quantity);
}
