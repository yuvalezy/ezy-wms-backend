using Core.Interfaces;

namespace Adapters.Common.Utils;

/// <summary>
/// Builds the SQL expressions for an item's unit-of-measure ladder, so every OITM read resolves it the
/// same way. Item reads funnel into a single ItemResponse, so applying the Options.ForceDoz override here
/// reaches every flow (goods receipt, picking, counting, transfer) without per-flow conversion code.
/// </summary>
/// <remarks>
/// <para>
/// WMS has a three-rung ladder: the base unit, a middle unit of OITM."NumInBuy" pieces (labelled
/// BuyUnitMsr), and a pack of OITM."PurPackUn" middle units (labelled PurPackMsr). A pack is therefore
/// <c>NumInBuy x PurPackUn</c> base pieces.
/// </para>
/// <para>
/// When NumInBuy is 1 the middle rung is degenerate — one piece per piece, identical to the base unit — so
/// the operator has no dozen to pick in, and PurPackUn is effectively counting pieces per pack. ForceDoz
/// turns that rung into a real dozen of 12 and divides the pack to match, so the pack keeps holding exactly
/// the pieces it always held: 72 pieces stays 72 pieces, now expressed as 6 dozens rather than 72 singles.
/// </para>
/// <para>
/// <b>The pack must be compensated in the same breath.</b> Raising NumInBuy without dividing PurPackUn
/// multiplies the physical pack by twelve (a 72-piece box becoming 864). Every read that substitutes
/// <see cref="NumInBuy"/> must also substitute <see cref="PurPackUn"/>, or that query's pack inflates.
/// </para>
/// <para>
/// <b>Only packs that are a whole number of dozens are converted.</b> A pack of 4 pieces is not a third of a
/// dozen: dividing it would leave a repeating decimal (4/12 = 0.333333, whose pack re-multiplies to
/// 3.999996, not 4) and a middle rung larger than the pack that contains it. Requiring
/// <c>PurPackUn % 12 = 0</c> keeps the division exact and the ladder ordered, and leaves items that have no
/// meaningful dozen exactly as SAP describes them.
/// </para>
/// </remarks>
public static class UomQueryHelper {
    /// Pieces per dozen, substituted for a degenerate middle rung.
    private const int DozenFactor = 12;

    /// Label applied to a middle rung ForceDoz has converted; SAP's own label still says piece.
    private const string DozenLabel = "Doz";

    private static string Prefix(string? qualifier) => qualifier is null ? string.Empty : $"{qualifier}.";

    private static string NumInBuyColumn(string prefix) => $"COALESCE({prefix}\"NumInBuy\", 1)";

    private static string PurPackUnColumn(string prefix) => $"COALESCE({prefix}\"PurPackUn\", 1)";

    /// <summary>
    /// True exactly when the middle rung is degenerate (no real conversion) <i>and</i> the pack divides
    /// cleanly into dozens, so the substitution is both needed and lossless.
    /// </summary>
    private static string IsConvertible(string prefix) =>
        $"{NumInBuyColumn(prefix)} = 1 AND {PurPackUnColumn(prefix)} % {DozenFactor} = 0";

    /// <summary>Pieces per middle unit — SAP's OITM."NumInBuy", raised to a dozen by ForceDoz.</summary>
    /// <param name="qualifier">Table alias owning the columns (e.g. "OITM"), or null when unqualified.</param>
    public static string NumInBuy(ISettings settings, string? qualifier = null) {
        string prefix = Prefix(qualifier);
        string column = NumInBuyColumn(prefix);

        return settings.Options.ForceDoz
            ? $"CASE WHEN {IsConvertible(prefix)} THEN {DozenFactor} ELSE {column} END"
            : column;
    }

    /// <summary>
    /// Middle units per pack — SAP's OITM."PurPackUn", divided by 12 wherever <see cref="NumInBuy"/> was
    /// raised to a dozen, so <c>NumInBuy x PurPackUn</c> (the pack's base pieces) is left unchanged.
    /// The division is exact: <see cref="IsConvertible"/> only fires on packs that are whole dozens.
    /// </summary>
    public static string PurPackUn(ISettings settings, string? qualifier = null) {
        string prefix = Prefix(qualifier);
        string column = PurPackUnColumn(prefix);

        return settings.Options.ForceDoz
            ? $"CASE WHEN {IsConvertible(prefix)} THEN {column} / {DozenFactor} ELSE {column} END"
            : column;
    }

    /// <summary>
    /// The middle unit's label — SAP's OITM."BuyUnitMsr", relabelled wherever the rung was converted, so the
    /// picker and reports do not offer a rung that reads "PZA" but counts twelve.
    /// </summary>
    public static string BuyUnitMsr(ISettings settings, string? qualifier = null) {
        string prefix = Prefix(qualifier);
        string column = $"{prefix}\"BuyUnitMsr\"";

        return settings.Options.ForceDoz
            ? $"CASE WHEN {IsConvertible(prefix)} THEN '{DozenLabel}' ELSE {column} END"
            : column;
    }
}
