using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <inheritdoc cref="IInventoryCountingQuantityRepairService"/>
public class InventoryCountingQuantityRepairService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ILogger<InventoryCountingQuantityRepairService> logger)
    : QuantityRepairServiceBase(db, adapter, logger), IInventoryCountingQuantityRepairService {
    public async Task<int> RepairAsync(Guid countingId) {
        // No LineStatus filter: GetCountingContent and the summary report sum every line regardless of status,
        // so restating a narrower set would leave those reads mixing restated and stale quantities. Soft-deleted
        // rows are already excluded by the context's global Deleted filter.
        var lines = await Db.InventoryCountingLines
            .Where(line => line.InventoryCountingId == countingId && line.ScannedQuantity > 0m)
            .ToArrayAsync();

        if (lines.Length == 0)
            return 0;

        var items = await TryLoadItemsAsync(lines.Select(line => line.ItemCode));
        if (items == null)
            return 0;

        // applyFactor: false — counting has never applied PurFactor1..4 (ValidateAddItemResult carries no
        // factor fields), so restating with them would silently multiply every line on the first read.
        int repaired = QuantityRestatement.Restate(lines, items, applyFactor: false, Logger);

        if (repaired > 0) {
            logger.LogInformation("Counting {CountingId}: restated {Count} line(s) after a conversion factor change", countingId, repaired);
            await Db.SaveChangesAsync();
        }

        return repaired;
    }
}
