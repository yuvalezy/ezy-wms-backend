using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

public class PickingRepackSession : BaseEntity {
    [Required]
    public int AbsEntry { get; set; }

    [Required]
    [StringLength(50)]
    public required string WhsCode { get; set; }

    [Required]
    public Guid StartedByUserId { get; set; }

    [StringLength(100)]
    public required string StartedByUserName { get; set; }

    [Required]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public bool IsCompleted { get; set; }

    public bool IsCancelled { get; set; }

    public DateTime? CancelledAt { get; set; }
}
