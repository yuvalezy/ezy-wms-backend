using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

[Table("WmsAlerts")]
public class WmsAlert : BaseEntity {
    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }

    [Required]
    public WmsAlertType AlertType { get; set; }

    [Required]
    public WmsAlertObjectType ObjectType { get; set; }

    [Required]
    public Guid ObjectId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string Message { get; set; }

    [MaxLength(2000)]
    public string? Data { get; set; }

    [Required]
    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
