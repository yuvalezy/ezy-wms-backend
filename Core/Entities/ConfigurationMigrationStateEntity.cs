using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Entities;

/// <summary>
/// Single-row record (Id == 1) tracking the outcome of the file -> database
/// configuration initialization (migrate or seed).
/// </summary>
public class ConfigurationMigrationStateEntity {
    [Key]
    public int Id { get; set; } = 1;

    public ConfigMigrationStatus Status { get; set; } = ConfigMigrationStatus.NotStarted;

    public ConfigSourceKind Source { get; set; } = ConfigSourceKind.None;

    public DateTime? LastRunAtUtc { get; set; }

    [MaxLength(400)]
    public string? ArchivePath { get; set; }

    public int? SectionsCount { get; set; }

    /// <summary>Last diff (on failure) or short success note.</summary>
    public string? Detail { get; set; }
}
