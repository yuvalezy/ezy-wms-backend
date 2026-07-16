using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Reports;

/// <summary>
/// Superuser payload to create a report — also the payload of <c>POST /api/reportDefinition/test</c>, which
/// validates and discovers columns against an <b>unsaved</b> definition (that is exactly why the execution
/// engine takes a <c>ReportDefinition</c> entity rather than an id: the Test button has no id yet).
/// </summary>
public class CreateReportDefinitionRequest {
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Unique URL identifier.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Slug { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Must contain <c>{{orderBy}}</c> and <c>{{paging}}</c> exactly once each, in code regions rather than
    /// inside literals or comments; any other <c>{{...}}</c> token is rejected (this is what catches an
    /// <c>{{OrderBy}}</c> casing typo that a plain <c>string.Replace</c> would pass silently to SQL Server).
    /// Must start with <c>SELECT</c>/<c>WITH</c> and contain no statement-separating <c>;</c>.
    /// </summary>
    [Required]
    public required string Sql { get; set; }

    /// <summary>
    /// Optional cheap hand-written count query; only consulted when <see cref="TotalCountMode"/> is
    /// <see cref="ReportTotalCountMode.Exact"/>.
    /// </summary>
    public string? CountSql { get; set; }

    public bool Enabled { get; set; } = true;

    public bool Recompile { get; set; }

    public ReportTotalCountMode TotalCountMode { get; set; } = ReportTotalCountMode.None;

    [Range(1, 200)]
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>
    /// Must match one of <see cref="Columns"/> and that column must be sortable.
    /// </summary>
    [MaxLength(128)]
    public string? DefaultSortColumnKey { get; set; }

    public ReportSortDirection DefaultSortDirection { get; set; } = ReportSortDirection.Asc;

    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 60;

    public List<ReportColumnRequest> Columns { get; set; } = [];

    public List<ReportVariableRequest> Variables { get; set; } = [];
}

/// <summary>
/// One column as authored. Typically seeded from discovery (<see cref="ReportValidationResult.Columns"/>)
/// and then adjusted.
/// </summary>
public class ReportColumnRequest {
    /// <summary>
    /// Must exactly match an output column name produced by the SQL.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public required string Key { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Label { get; set; }

    public ReportColumnFormat Format { get; set; } = ReportColumnFormat.Text;

    /// <summary><c>left</c>, <c>center</c> or <c>right</c>; null lets the client decide from the format.</summary>
    [MaxLength(10)]
    public string? Align { get; set; }

    [Range(0, 10)]
    public int Decimals { get; set; } = 2;

    public bool Visible { get; set; } = true;

    public bool Sortable { get; set; } = true;

    public bool Nullable { get; set; } = true;

    public int Order { get; set; }
}

/// <summary>
/// One filter variable as authored.
/// </summary>
public class ReportVariableRequest {
    /// <summary>
    /// Without the <c>@</c>. Validated against <c>^[A-Za-z_][A-Za-z0-9_]{0,59}$</c>, rejected if it starts
    /// with <c>__</c> (reserved for the engine's <c>@__offset</c>/<c>@__limit</c>), and rejected if it
    /// duplicates another variable case-insensitively.
    /// </summary>
    [Required]
    [MaxLength(60)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Label { get; set; }

    public ReportVariableType Type { get; set; } = ReportVariableType.Text;

    [Range(0, 10)]
    public int Decimals { get; set; } = 2;

    public bool Required { get; set; }

    public bool AllowMultiple { get; set; }

    [MaxLength(4000)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Static dropdown options for <see cref="ReportVariableType.SelectList"/>. Serialized to the entity's
    /// <c>StaticOptions</c> JSON by the service — the client sends structured options, not a JSON string.
    /// </summary>
    public List<ReportLookupOption> Options { get; set; } = [];

    /// <summary>
    /// Query for <see cref="ReportVariableType.SqlLookup"/>; must return exactly two columns named
    /// <c>Value</c> and <c>Label</c>. Validated at save with the same rules as the report SQL.
    /// </summary>
    public string? LookupSql { get; set; }

    public int Order { get; set; }
}
