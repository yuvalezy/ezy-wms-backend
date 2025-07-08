using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Models;

namespace Core.Entities;

public class CloudSyncQueue {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public CloudLicenseEvent EventType      { get; set; }
    public string            DeviceUuid     { get; set; } = string.Empty;
    public string            RequestPayload { get; set; } = string.Empty; // JSON serialized request
    public DateTime?         ProcessedAt    { get; set; }
    public int               RetryCount     { get; set; }
    public DateTime          NextRetryAt    { get; set; }
    public CloudSyncStatus   Status         { get; set; }
    public string?           LastError      { get; set; }
}