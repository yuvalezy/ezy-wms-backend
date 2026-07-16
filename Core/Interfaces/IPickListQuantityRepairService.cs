using Core.Entities;

namespace Core.Interfaces;

public interface IPickListQuantityRepairService {
    /// <summary>
    /// Re-derives each line's base quantity from its scanned count and the item's <b>current</b> SAP
    /// conversion factor, persisting any line whose stored base no longer matches.
    /// </summary>
    /// <remarks>
    /// Picking freezes the factor into <see cref="PickList.Quantity"/> at scan time, so a factor corrected
    /// in SAP — or the Options.ForceDoz override being toggled — leaves open lines holding a stale base.
    /// Unlike goods receipt, picking cannot re-derive on every read: its quantities are consumed by
    /// database-side aggregates (stock reservation, open-quantity validation) that cannot reach SAP's
    /// factors. Repairing at the point a pick list is opened lets those aggregates keep working unchanged.
    ///
    /// Lines with no scanned count (legacy rows created before the column existed) have no source of truth
    /// to re-derive from and are left exactly as they are.
    /// </remarks>
    /// <param name="lines">Already-materialised open lines to repair; mutated in place.</param>
    /// <returns>The number of lines whose stored quantity was corrected.</returns>
    Task<int> RepairAsync(IReadOnlyCollection<PickList> lines);
}
