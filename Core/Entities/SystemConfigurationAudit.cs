using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Entities;

/// <summary>
/// Append-only history of every configuration write (migration, seed, edit,
/// import, restore, and pre-migration backup). Secret leaves stay encrypted.
/// </summary>
public class SystemConfigurationAudit {
    [Key]
    public Guid Id { get; set; }

    [MaxLength(100)]
    public required string Section { get; set; }

    public required string Json { get; set; }

    public int Version { get; set; }

    public ConfigChangeType ChangeType { get; set; }

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? ChangedByUserId { get; set; }

    [MaxLength(400)]
    public string? Note { get; set; }
}
