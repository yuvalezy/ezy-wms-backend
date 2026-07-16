using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

/// <summary>
/// Append-only record of one report execution. Cheap to write, and the only thing that helps after an
/// incident — report SQL runs against the live SAP database, so "who ran what, with which values, and how
/// long did it pin the server" is the whole forensic trail.
/// </summary>
[Table("ReportExecutionAudits")]
public class ReportExecutionAudit {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid ReportDefinitionId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// The whitelisted <see cref="ReportColumn.Key"/> actually used, after validation — not the request's text.
    /// </summary>
    [MaxLength(128)]
    public string? SortColumnKey { get; set; }

    public ReportSortDirection SortDirection { get; set; }

    /// <summary>
    /// The submitted variable values, serialized as JSON.
    /// </summary>
    public string? ParametersJson { get; set; }

    /// <summary>
    /// Rows returned to the caller (after the <c>limit + 1</c> probe row is trimmed).
    /// </summary>
    public long RowCount { get; set; }

    public long DurationMs { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
}
