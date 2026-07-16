using Core.Enums;

namespace Core.Interfaces;

/// <summary>
/// A document line whose stored base quantity was derived from a physically-scanned count in a
/// <see cref="UnitType"/>, and can therefore be re-derived when the item's SAP conversion factor changes.
/// </summary>
/// <remarks>
/// <see cref="ScannedQuantity"/> is null when the line's base was set directly rather than scanned — a row
/// created before the column existed, or a base quantity typed straight into a correction screen. Such a line
/// has no truth to re-derive from and is left exactly as it is.
/// </remarks>
public interface IScannedQuantityLine {
    Guid Id { get; }

    string ItemCode { get; }

    /// <summary>The rung <see cref="ScannedQuantity"/> was counted in.</summary>
    UnitType Unit { get; }

    decimal? ScannedQuantity { get; }

    /// <summary>Base pieces. The derived value — restated in place when the factor moves.</summary>
    decimal Quantity { get; set; }
}
