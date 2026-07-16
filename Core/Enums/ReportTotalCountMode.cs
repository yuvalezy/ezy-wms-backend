namespace Core.Enums;

/// <summary>
/// Whether a report can report an exact total row count.
/// Serialized as a string (member name is the wire contract).
/// </summary>
public enum ReportTotalCountMode {
    /// <summary>
    /// Default. No total is ever computed: the page fetches <c>limit + 1</c> rows, trims the extra one and
    /// sets <c>HasMore</c>, giving real Next/Prev from a single query.
    /// </summary>
    None = 0,

    /// <summary>
    /// An exact <c>COUNT_BIG(*)</c> is available, but only through the separate lazy count endpoint —
    /// a slow count must never block the rows.
    /// </summary>
    Exact = 1
}
