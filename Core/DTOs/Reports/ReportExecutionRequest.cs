using Core.Enums;

namespace Core.DTOs.Reports;

/// <summary>
/// One run of a report: the submitted filter values plus the sort/page choice. Shared by the page, count,
/// lookup and export paths.
/// </summary>
/// <remarks>
/// Nothing here ever reaches the SQL <b>text</b>. Values become typed <c>SqlParameter</c>s; the sort key is a
/// whitelist lookup that emits the stored column key; offset/limit become <c>@__offset</c>/<c>@__limit</c>.
/// </remarks>
public class ReportExecutionRequest {
    /// <summary>
    /// Submitted values keyed by <see cref="Core.Entities.ReportVariable.Name"/>. Compare keys
    /// case-insensitively — <c>@foo</c> and <c>@Foo</c> are the same parameter to SQL Server.
    /// A declared variable absent from this map binds a typed <c>DBNull</c> (or is rejected if it's Required).
    /// </summary>
    public Dictionary<string, ReportVariableValue> Variables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Requested sort column. Matched case-insensitively against the declared sortable columns; unknown
    /// or non-sortable → 400, null/empty → the definition's <c>DefaultSortColumnKey</c>. Never emitted verbatim.
    /// </summary>
    public string? SortColumnKey { get; set; }

    /// <summary>
    /// Null falls back to the definition's <c>DefaultSortDirection</c>.
    /// </summary>
    public ReportSortDirection? SortDirection { get; set; }

    /// <summary>
    /// Rows to skip. Negative is rejected and it is hard-capped at 100,000: <c>OFFSET</c> is O(n), so a
    /// half-million offset rescans half a million rows on <b>every</b> page.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Rows to return. Null falls back to the definition's <c>DefaultPageSize</c>; clamped to [1, 200]
    /// (hard max 500). The engine actually fetches <c>limit + 1</c> to derive <see cref="ReportPageResult.HasMore"/>.
    /// </summary>
    public int? Limit { get; set; }
}

/// <summary>
/// A single submitted filter value. Every field is a string: values are parsed strictly against the
/// variable's declared type and <b>rejected, never loosely coerced</b>. Exactly one shape applies per
/// variable type.
/// </summary>
public class ReportVariableValue {
    /// <summary>
    /// The value, for every single-valued type (<c>Text</c>, <c>Integer</c>, <c>Decimal</c>, <c>Date</c>,
    /// <c>DateTime</c>, <c>Boolean</c>, <c>YesNo</c>, and <c>SelectList</c>/<c>SqlLookup</c> with
    /// <c>AllowMultiple = false</c>). Dates are ISO, parsed exactly.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// The selected values for <c>SelectList</c>/<c>SqlLookup</c> with <c>AllowMultiple = true</c>. Joined
    /// into <b>one</b> <c>NVarChar(4000)</c> CSV parameter for <c>STRING_SPLIT(@X, ',')</c> — the SQL text must
    /// not vary with how many values were picked.
    /// </summary>
    public List<string>? Values { get; set; }

    /// <summary>
    /// Lower bound of a <c>DateRange</c> → binds <c>@{Name}From</c>. Requires From &lt;= <see cref="To"/>.
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// Upper bound of a <c>DateRange</c> → binds <c>@{Name}To</c>.
    /// </summary>
    public string? To { get; set; }
}
