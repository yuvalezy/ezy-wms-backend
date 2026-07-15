using Core.DTOs.Items;
using Core.Entities;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <inheritdoc cref="IPickListQuantityRepairService"/>
public class PickListQuantityRepairService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ILogger<PickListQuantityRepairService> logger) : IPickListQuantityRepairService {
    public async Task<int> RepairAsync(IReadOnlyCollection<PickList> lines) {
        // Only lines with a scanned count can be re-derived; legacy rows keep their frozen base.
        var repairable = lines.Where(line => line.ScannedQuantity is > 0m).ToArray();
        if (repairable.Length == 0)
            return 0;

        // One bulk lookup rather than a SAP round-trip per line.
        string[] itemCodes = repairable.Select(line => line.ItemCode).Distinct().ToArray();
        Dictionary<string, ItemCheckResponse> items;
        try {
            items = await adapter.BulkItemCheckAsync(itemCodes);
        }
        catch (Exception ex) {
            // Opening a pick list must not fail because SAP is briefly unavailable; the stored base is the
            // same one every read used before this repair existed.
            logger.LogWarning(ex, "Could not load item factors to repair pick list quantities; leaving stored quantities unchanged");
            return 0;
        }

        int repaired = 0;
        foreach (var line in repairable) {
            if (!items.TryGetValue(line.ItemCode, out var item))
                continue;

            decimal effective = GoodsReceiptQuantityCalculator.ToBaseQuantity(line.ScannedQuantity!.Value, line.Unit, item);
            if (effective == line.Quantity)
                continue;

            logger.LogInformation(
                "Pick list {AbsEntry} line {LineId} ({ItemCode}): conversion factor changed, {Scanned} {Unit} restated from {OldQuantity} to {NewQuantity} base",
                line.AbsEntry, line.Id, line.ItemCode, line.ScannedQuantity, line.Unit, line.Quantity, effective);

            line.Quantity = effective;
            repaired++;
        }

        if (repaired > 0)
            await db.SaveChangesAsync();

        return repaired;
    }
}
