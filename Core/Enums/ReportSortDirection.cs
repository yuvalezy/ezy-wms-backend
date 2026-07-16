namespace Core.Enums;

/// <summary>
/// Sort direction for a report column. Deliberately an enum and never a free string —
/// the composed <c>ORDER BY</c> must be a pure function of the definition and the request's
/// validated sort choice.
/// Serialized as a string (member name is the wire contract).
/// </summary>
public enum ReportSortDirection {
    /// <summary>
    /// Ascending — emits <c>ASC</c>.
    /// </summary>
    Asc = 0,

    /// <summary>
    /// Descending — emits <c>DESC</c>.
    /// </summary>
    Desc = 1
}
