using Core.DTOs.Items;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Core.Services;

/// <summary>
/// Restates document lines whose stored base quantity was frozen against a conversion factor that has since
/// changed — because SAP's item master was corrected, or because Options.ForceDoz was toggled.
/// </summary>
/// <remarks>
/// Pure: no database, no SAP. The caller loads the lines and their items, and decides when to persist.
///
/// Goods receipt re-derives on every read and never stores a frozen base. Picking, counting and transfer
/// cannot: their quantities feed database-side aggregates (stock reservation, open-quantity checks, the
/// transfer source/target invariant) that have no way to reach SAP's factors. Restating when the document is
/// opened lets those aggregates keep working unchanged.
/// </remarks>
public static class QuantityRestatement {
    // Every Quantity/ScannedQuantity column in play is decimal(18,2). Restating to full precision and then
    // comparing against a value the column rounded on write would never converge: the line would be "repaired"
    // and re-saved on every single load. Round to what the column can actually hold, the way SQL Server does.
    private const int QuantityScale = 2;

    /// <summary>
    /// Re-derives each line's base quantity from its scanned count and the item's <b>current</b> factor,
    /// mutating <see cref="IScannedQuantityLine.Quantity"/> in place.
    /// </summary>
    /// <param name="lines">Already-materialised lines to restate.</param>
    /// <param name="items">Current item masters, keyed by item code. A line whose item is missing is skipped.</param>
    /// <param name="applyFactor">
    /// Whether to apply PurFactor1..4. This must mirror the flow's own scan-time formula, or opening a document
    /// silently restates lines that never changed: picking and transfer scan with the factors, counting never
    /// does (<see cref="ValidateAddItemResult"/> carries no factor fields).
    /// </param>
    /// <param name="logger">Every restatement is logged — a quantity moving under a user needs a paper trail.</param>
    /// <returns>The number of lines whose quantity changed.</returns>
    public static int Restate(
        IEnumerable<IScannedQuantityLine> lines,
        IReadOnlyDictionary<string, ItemCheckResponse> items,
        bool applyFactor,
        ILogger logger) {
        int restated = 0;

        foreach (var line in lines) {
            // Only a scanned count can be re-derived; a directly-set base keeps its stored value.
            if (line.ScannedQuantity is not > 0m)
                continue;

            if (!items.TryGetValue(line.ItemCode, out var item))
                continue;

            decimal effective = Effective(line, item, applyFactor);
            if (effective == line.Quantity)
                continue;

            logger.LogInformation(
                "Line {LineId} ({ItemCode}): conversion factor changed, {Scanned} {Unit} restated from {OldQuantity} to {NewQuantity} base",
                line.Id, line.ItemCode, line.ScannedQuantity, line.Unit, line.Quantity, effective);

            line.Quantity = effective;
            restated++;
        }

        return restated;
    }

    /// <summary>
    /// The base pieces a line currently represents, rounded to what its column can store.
    /// </summary>
    public static decimal Effective(IScannedQuantityLine line, ItemResponse item, bool applyFactor) =>
        decimal.Round(
            GoodsReceiptQuantityCalculator.ToBaseQuantity(line.ScannedQuantity!.Value, line.Unit, item, applyFactor),
            QuantityScale,
            MidpointRounding.AwayFromZero);
}
