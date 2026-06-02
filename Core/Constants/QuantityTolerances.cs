namespace Core.Constants;

public static class QuantityTolerances {
    /// <summary>
    /// Quantities are decimals (int->decimal migration). Fully-picked lists/items can carry
    /// sub-unit residue in SUM(RelQtty)/OpenQuantity, so "finished" must be a tolerance check
    /// rather than an exact "== 0" comparison.
    /// </summary>
    public const decimal Completed = 0.0001m;
}
