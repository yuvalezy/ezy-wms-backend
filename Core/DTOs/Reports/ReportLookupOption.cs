namespace Core.DTOs.Reports;

/// <summary>
/// One dropdown entry, from either a <c>SelectList</c>'s declared static options or a <c>SqlLookup</c>'s
/// query (which must return exactly two columns named <c>Value</c> and <c>Label</c>).
/// </summary>
public class ReportLookupOption {
    /// <summary>
    /// The value bound to the SQL parameter. Never null and never empty — the frontend's
    /// <c>&lt;Select&gt;</c> items require a non-null, non-empty value.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Display text. Falls back to <see cref="Value"/> when the query's <c>Label</c> is NULL.
    /// </summary>
    public required string Label { get; set; }
}
