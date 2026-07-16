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
    ILogger<PickListQuantityRepairService> logger)
    : QuantityRepairServiceBase(db, adapter, logger), IPickListQuantityRepairService {
    public async Task<int> RepairAsync(IReadOnlyCollection<PickList> lines) {
        // Only lines with a scanned count can be re-derived; legacy rows keep their frozen base.
        var repairable = lines.Where(line => line.ScannedQuantity is > 0m).ToArray();
        if (repairable.Length == 0)
            return 0;

        var items = await TryLoadItemsAsync(repairable.Select(line => line.ItemCode));
        if (items == null)
            return 0;

        // PickListLineService scans through the calculator with PurFactor1..4 applied; restate the same way,
        // or reopening a pick list would restate lines whose factor never moved.
        int repaired = QuantityRestatement.Restate(repairable, items, applyFactor: true, Logger);

        if (repaired > 0)
            await Db.SaveChangesAsync();

        return repaired;
    }
}
