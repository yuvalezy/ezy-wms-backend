namespace Core.Interfaces;

public interface IInventoryCountingQuantityRepairService {
    /// <summary>
    /// Re-derives every counted line's base quantity from its scanned count and the item's <b>current</b> SAP
    /// conversion factor, persisting any line whose stored base no longer matches.
    /// </summary>
    /// <remarks>
    /// Counting freezes the factor into the stored quantity at scan time, so a factor corrected in SAP — or
    /// the Options.ForceDoz override being toggled — leaves lines holding a stale base. Call this at the top
    /// of every read that sums counted quantities, before its own query, so the result does not depend on
    /// which screen the operator happened to open first.
    ///
    /// Lines with no scanned count (rows created before the column existed) have no source of truth to
    /// re-derive from and are left exactly as they are.
    /// </remarks>
    /// <param name="countingId">The counting whose lines to restate.</param>
    /// <returns>The number of lines whose stored quantity was corrected.</returns>
    Task<int> RepairAsync(Guid countingId);
}
