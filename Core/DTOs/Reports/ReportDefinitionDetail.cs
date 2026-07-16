using Core.Enums;

namespace Core.DTOs.Reports;

/// <summary>
/// Full view of a report definition. Serves the superuser editor and the end-user runner
/// (<c>GET /api/report/{slug}</c>).
/// </summary>
/// <remarks>
/// <b>Security:</b> <see cref="Sql"/>, <see cref="CountSql"/> and <see cref="ReportVariableDescriptor.LookupSql"/>
/// are populated for superusers only and are <b>null on the end-user path</b> — the runner has no business
/// seeing the query. Everything else is what the runner needs to draw its filter bar and table.
/// </remarks>
public class ReportDefinitionDetail {
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public string? Description { get; set; }

    /// <summary>Superuser only; null for end users.</summary>
    public string? Sql { get; set; }

    /// <summary>Superuser only; null for end users.</summary>
    public string? CountSql { get; set; }

    public bool Enabled { get; set; }

    public bool Recompile { get; set; }

    /// <summary>
    /// <see cref="ReportTotalCountMode.None"/> means the runner must not call the count endpoint —
    /// it pages off <see cref="ReportPageResult.HasMore"/> alone.
    /// </summary>
    public ReportTotalCountMode TotalCountMode { get; set; }

    public int DefaultPageSize { get; set; }

    public string? DefaultSortColumnKey { get; set; }

    public ReportSortDirection DefaultSortDirection { get; set; }

    public int TimeoutSeconds { get; set; }

    public List<ReportColumnDescriptor> Columns { get; set; } = [];

    public List<ReportVariableDescriptor> Variables { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// One filter variable on the wire — everything the runner needs to render the right input control.
/// </summary>
public class ReportVariableDescriptor {
    /// <summary>
    /// The parameter name without <c>@</c>; the key to use in <see cref="ReportExecutionRequest.Variables"/>.
    /// </summary>
    public required string Name { get; set; }

    public required string Label { get; set; }

    public ReportVariableType Type { get; set; }

    public int Decimals { get; set; }

    public bool Required { get; set; }

    public bool AllowMultiple { get; set; }

    public string? DefaultValue { get; set; }

    /// <summary>
    /// Static dropdown options, already parsed out of the entity's <c>StaticOptions</c> JSON — the client
    /// never parses that string itself. Populated for <see cref="ReportVariableType.SelectList"/> only.
    /// </summary>
    public List<ReportLookupOption> Options { get; set; } = [];

    /// <summary>
    /// True when the variable is a <see cref="ReportVariableType.SqlLookup"/> with a query — the client
    /// fetches its options from <c>/api/report/{slug}/lookup/{name}</c>, posting the current filter values so
    /// cascading lookups (warehouse → bin) narrow correctly.
    /// </summary>
    public bool HasLookup { get; set; }

    /// <summary>Superuser only; null for end users.</summary>
    public string? LookupSql { get; set; }

    public int Order { get; set; }
}
