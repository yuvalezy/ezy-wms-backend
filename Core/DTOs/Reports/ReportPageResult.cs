using Core.Enums;

namespace Core.DTOs.Reports;

/// <summary>
/// One page of report rows.
/// </summary>
/// <remarks>
/// There is no total here by design: the engine fetches <c>limit + 1</c> rows, trims the extra and sets
/// <see cref="HasMore"/>, which gives real Next/Prev from a single query. An exact count is opt-in through
/// the separate lazy count endpoint so a slow count never blocks the rows.
/// </remarks>
public class ReportPageResult {
    /// <summary>
    /// The report's columns in display order, so the runner can render a SQL-driven table it can't hardcode.
    /// </summary>
    public IReadOnlyList<ReportColumnDescriptor> Columns { get; set; } = [];

    /// <summary>
    /// Row values keyed by <see cref="ReportColumnDescriptor.Key"/>. A SQL NULL is a null entry — the client
    /// must render it as an em dash rather than 0.
    /// </summary>
    public IReadOnlyList<Dictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>
    /// True when the <c>limit + 1</c> probe row came back (and was dropped from <see cref="Rows"/>).
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// The offset actually used.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// The page size actually used, after clamping.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// The whitelisted column key actually sorted by — echo this back to the header arrow rather than the
    /// requested text. A header reading "Qty ▼" over data sorted by DocEntry is how someone ships the wrong pallet.
    /// </summary>
    public string? SortColumnKey { get; set; }

    public ReportSortDirection SortDirection { get; set; }

    /// <summary>
    /// Server-side execution time, also written to the execution audit.
    /// </summary>
    public long DurationMs { get; set; }
}
