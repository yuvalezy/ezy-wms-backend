using Core.Interfaces;

namespace Adapters.Common.Utils;

/// <summary>
/// Builds the SQL expression for an item's unit-of-measure conversion factors, so that every OITM read
/// resolves them the same way. Item reads funnel into a single ItemResponse.NumInBuy, so applying the
/// Options.ForceDoz override here reaches every flow without per-flow conversion code.
/// </summary>
public static class UomQueryHelper {
    /// The buy-unit label, upper-cased, that ForceDoz recognises as a dozen.
    private const string DozenLabel = "DOZ";

    /// Units per dozen, substituted when a dozen-labelled unit carries a factor of 1.
    private const int DozenFactor = 12;

    /// <summary>
    /// SQL expression yielding the item's dozen factor, honouring <c>Options.ForceDoz</c>.
    /// </summary>
    /// <remarks>
    /// The substitution is gated on the unit *label*, not on the factor alone. A factor of 1 is perfectly
    /// correct for an item whose buy unit is a piece (PZA, EA, a blank label) — it means one piece per piece.
    /// Only a unit that calls itself a dozen while carrying a factor of 1 is misconfigured, and only that
    /// combination is rewritten. Without this guard the override rewrites every piece-based item in the
    /// catalogue and inflates its stock twelvefold, including Pack scans, which multiply through NumInBuy.
    /// </remarks>
    /// <param name="qualifier">Table alias owning the columns (e.g. "OITM"), or null when unqualified.</param>
    public static string NumInBuy(ISettings settings, string? qualifier = null) {
        string prefix    = qualifier is null ? string.Empty : $"{qualifier}.";
        string column    = $"{prefix}\"NumInBuy\"";
        string label     = $"{prefix}\"BuyUnitMsr\"";
        string coalesced = $"COALESCE({column}, 1)";

        if (!settings.Options.ForceDoz)
            return coalesced;

        // UPPER/TRIM so 'Doz', 'DOZ' and ' doz ' all match; a NULL label can never equal 'DOZ'.
        string isDozenLabel = $"UPPER(LTRIM(RTRIM(COALESCE({label}, '')))) = '{DozenLabel}'";

        return $"CASE WHEN {coalesced} = 1 AND {isDozenLabel} THEN {DozenFactor} ELSE {coalesced} END";
    }
}
