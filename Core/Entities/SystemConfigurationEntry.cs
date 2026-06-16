using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

/// <summary>
/// Current value of a single top-level configuration section, stored as
/// binder-compatible JSON. Secret leaves inside <see cref="Json"/> are encrypted
/// at rest (marker-prefixed). One row per section (PK = <see cref="Section"/>).
/// </summary>
public class SystemConfigurationEntry {
    [Key]
    [MaxLength(100)]
    public required string Section { get; set; }

    public required string Json { get; set; }

    public int Version { get; set; } = 1;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? UpdatedByUserId { get; set; }

    /// <summary>Optimistic concurrency token (SQL Server rowversion).</summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
