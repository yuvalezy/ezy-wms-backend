namespace Core.Interfaces;

public interface ITransferQuantityRepairService {
    /// <summary>
    /// Re-derives every open transfer line's base quantity from its scanned count and the item's <b>current</b>
    /// SAP conversion factor, persisting any line whose stored base no longer matches.
    /// </summary>
    /// <remarks>
    /// Works per item across <b>both</b> source and target together, and never in a way that breaks the
    /// invariant that a transfer's source and target quantities match: under Options.ForceDoz a Dozen line is
    /// raised twelvefold while a Pack line stays put, so an item scanned Pack on one side and Dozen on the
    /// other would desync if each line were restated on its own. Such an item is left alone and logged —
    /// re-scan it to pick up the new factor.
    ///
    /// Call this only from reads that see both sides. Restating from a read filtered to one
    /// <see cref="Core.Enums.SourceTarget"/> guarantees the desync it is meant to prevent.
    ///
    /// Lines with no scanned count are left exactly as they are: rows created before the column existed, and
    /// lines whose base was typed straight into the target-detail screen, have no scan to re-derive from.
    /// </remarks>
    /// <param name="transferId">The transfer whose lines to restate.</param>
    /// <returns>The number of lines whose stored quantity was corrected.</returns>
    Task<int> RepairAsync(Guid transferId);
}
