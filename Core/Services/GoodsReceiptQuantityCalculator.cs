using Core.DTOs.Items;
using Core.Enums;

namespace Core.Services;

/// <summary>
/// Single source of truth for converting a physically-scanned goods-receipt count
/// (in its <see cref="UnitType"/>: unit / dozen / pack) to the base-piece quantity,
/// and back. Every read/process path derives base quantity from the scanned count and
/// the <b>current</b> SAP conversion factor through this calculator, so a factor
/// correction in SAP recalculates pieces while keeping the scanned box/dozen count fixed
/// — no frozen factor, no phantom quantities.
/// </summary>
public static class GoodsReceiptQuantityCalculator {
    // A conversion factor of 0 (or negative) is not physically meaningful and would
    // collapse a quantity to zero. Treat it as 1 so bad/missing master data can never
    // silently wipe out a received quantity.
    private static decimal Normalize(decimal factor) => factor <= 0m ? 1m : factor;

    /// <summary>
    /// Scanned count (in <paramref name="unit"/>) → base pieces, using the given factors.
    /// Mirrors the historical scan-time formula: count × NumInBuy (dozen/pack) × PurPackUn (pack) × PurFactor1..4.
    /// </summary>
    public static decimal ToBaseQuantity(
        decimal scannedQuantity,
        UnitType unit,
        decimal numInBuy,
        decimal purPackUn,
        decimal factor1 = 1m,
        decimal factor2 = 1m,
        decimal factor3 = 1m,
        decimal factor4 = 1m) {
        decimal quantity = scannedQuantity;

        if (unit != UnitType.Unit)
            quantity *= Normalize(numInBuy);

        if (unit == UnitType.Pack)
            quantity *= Normalize(purPackUn);

        quantity *= Normalize(factor1) * Normalize(factor2) * Normalize(factor3) * Normalize(factor4);

        return quantity;
    }

    /// <summary>
    /// Scanned count → base pieces using an item master's current factors.
    /// <paramref name="applyFactor"/> gates the PurFactor1..4 multipliers (parity with the scan path).
    /// </summary>
    public static decimal ToBaseQuantity(decimal scannedQuantity, UnitType unit, ItemResponse item, bool applyFactor = true) =>
        ToBaseQuantity(
            scannedQuantity,
            unit,
            item.NumInBuy,
            item.PurPackUn,
            applyFactor ? item.Factor1 : 1m,
            applyFactor ? item.Factor2 : 1m,
            applyFactor ? item.Factor3 : 1m,
            applyFactor ? item.Factor4 : 1m);

    /// <summary>
    /// The base pieces a receipt line currently represents: re-derived from the scanned count and the
    /// current factor when a scanned count exists, otherwise the stored/frozen base (legacy lines with
    /// <paramref name="scannedQuantity"/> == 0).
    /// </summary>
    public static decimal EffectiveBaseQuantity(
        decimal scannedQuantity,
        UnitType unit,
        decimal storedBaseQuantity,
        decimal numInBuy,
        decimal purPackUn,
        decimal factor1 = 1m,
        decimal factor2 = 1m,
        decimal factor3 = 1m,
        decimal factor4 = 1m) =>
        scannedQuantity > 0m
            ? ToBaseQuantity(scannedQuantity, unit, numInBuy, purPackUn, factor1, factor2, factor3, factor4)
            : storedBaseQuantity;

    /// <inheritdoc cref="EffectiveBaseQuantity(decimal, UnitType, decimal, decimal, decimal, decimal, decimal, decimal, decimal)"/>
    public static decimal EffectiveBaseQuantity(decimal scannedQuantity, UnitType unit, decimal storedBaseQuantity, ItemResponse item) =>
        scannedQuantity > 0m ? ToBaseQuantity(scannedQuantity, unit, item) : storedBaseQuantity;

    /// <summary>
    /// Rescales one stored (scan-time) allocation — a <c>GoodsReceiptSource.Quantity</c> in base pieces —
    /// to the line's current effective base, preserving the source's share of the line. Multiply-then-divide
    /// to avoid the precision loss of forming a ratio first (the confirmation flow compares with exact equality).
    /// Returns the allocation unchanged when there is nothing to rescale (legacy / empty line).
    /// </summary>
    public static decimal RescaleAllocation(decimal storedAllocation, decimal storedLineBase, decimal effectiveLineBase) =>
        storedLineBase == 0m ? storedAllocation : storedAllocation * effectiveLineBase / storedLineBase;

    /// <summary>
    /// Base pieces → scanned count (in <paramref name="unit"/>). Inverse of <see cref="ToBaseQuantity(decimal, UnitType, decimal, decimal, decimal, decimal, decimal, decimal)"/>.
    /// </summary>
    public static decimal ToScannedQuantity(
        decimal baseQuantity,
        UnitType unit,
        decimal numInBuy,
        decimal purPackUn,
        decimal factor1 = 1m,
        decimal factor2 = 1m,
        decimal factor3 = 1m,
        decimal factor4 = 1m) {
        decimal quantity = baseQuantity;

        if (unit != UnitType.Unit)
            quantity /= Normalize(numInBuy);

        if (unit == UnitType.Pack)
            quantity /= Normalize(purPackUn);

        quantity /= Normalize(factor1) * Normalize(factor2) * Normalize(factor3) * Normalize(factor4);

        return quantity;
    }
}
