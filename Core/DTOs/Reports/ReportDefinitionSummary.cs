namespace Core.DTOs.Reports;

/// <summary>
/// List-row view of a report. Used by the superuser definitions list, the end-user Reports list
/// (<c>GET /api/report</c>, filtered live by the caller's granted reports), and the authorization-group
/// grant card. Deliberately carries no SQL.
/// </summary>
public class ReportDefinitionSummary {
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Always true on the end-user list (disabled reports are filtered out); may be false on the
    /// superuser list.
    /// </summary>
    public bool Enabled { get; set; }

    public int ColumnCount { get; set; }

    public int VariableCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
