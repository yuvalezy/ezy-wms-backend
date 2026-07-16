using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

/// <summary>
/// A superuser-authored, SQL-backed report. The aggregate root: it owns its <see cref="Columns"/>
/// and <see cref="Variables"/>, and is what the execution engine is handed (never an id, so the
/// engine stays free of <c>SystemDbContext</c>).
/// </summary>
/// <remarks>
/// The invariant everything hangs off: <b>shape is fixed at save time; only parameter values vary at
/// run time</b>. The composed SQL text is a pure function of (definition, sort key, sort direction,
/// compose mode) — never of submitted values.
/// </remarks>
[Table("ReportDefinitions")]
public class ReportDefinition : BaseEntity {
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Stable URL identifier (<c>/reports/{slug}</c>, <c>POST /api/report/{slug}/run</c>). Unique.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Slug { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// The author's SQL. Operator-trusted (superuser-authored) but still executed inside an
    /// always-rollback scope against the reporting connection. Must contain the <c>{{orderBy}}</c> and
    /// <c>{{paging}}</c> tokens exactly once each.
    /// </summary>
    [Required]
    public required string Sql { get; set; }

    /// <summary>
    /// Optional hand-written cheap count query, used only when <see cref="TotalCountMode"/> is
    /// <see cref="ReportTotalCountMode.Exact"/>. This exists for <b>cost</b> — the engine can already
    /// hoist CTEs and wrap <see cref="Sql"/> in <c>COUNT_BIG(*)</c> on its own.
    /// </summary>
    public string? CountSql { get; set; }

    /// <summary>
    /// Disabled reports stay authored but are invisible to (and unrunnable by) end users.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Appends <c>OPTION (RECOMPILE)</c> at statement level in <b>both</b> compose shapes. Declarative
    /// rather than author-written, because <c>OPTION</c> is illegal inside the derived table the count
    /// shape wraps the query in — a hand-written one would save clean and blow up on the count request.
    /// Near-mandatory in practice: the <c>(@X IS NULL OR col = @X)</c> pattern is a parameter-sniffing trap.
    /// </summary>
    public bool Recompile { get; set; }

    public ReportTotalCountMode TotalCountMode { get; set; } = ReportTotalCountMode.None;

    /// <summary>
    /// Page size used when the request omits one. Clamped to [1, 200] at execution.
    /// </summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>
    /// <see cref="ReportColumn.Key"/> to sort by when the request specifies none. An <i>unspecified</i>
    /// request key falls back to this; an <i>unknown</i> request key is a 400.
    /// </summary>
    [MaxLength(128)]
    public string? DefaultSortColumnKey { get; set; }

    public ReportSortDirection DefaultSortDirection { get; set; } = ReportSortDirection.Asc;

    /// <summary>
    /// Mandatory <c>CommandTimeout</c> for every path (page, count, lookup, validate, discover).
    /// Clamped to [5, 300]. ADO.NET's 30s default is not relied on.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    // Navigation properties
    public ICollection<ReportColumn> Columns { get; set; } = new List<ReportColumn>();
    public ICollection<ReportVariable> Variables { get; set; } = new List<ReportVariable>();
}
