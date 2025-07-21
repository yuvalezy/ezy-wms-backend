using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Entities;

public class PickListCheckSession : BaseEntity {
    [Required]
    public int PickListId { get; set; }

    [Required]
    public Guid StartedByUserId { get; set; }

    [Required]
    [StringLength(100)]
    public string StartedByUserName { get; set; } = string.Empty;

    [Required]
    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [Required]
    public bool IsCompleted { get; set; }

    [Required]
    public bool IsCancelled { get; set; }

    // Navigation properties
    public virtual ICollection<PickListCheckItem> CheckedItems { get; set; } = new List<PickListCheckItem>();
    public virtual User StartedByUser { get; set; } = null!;
}

public class PickListCheckItem : BaseEntity {
    [Required]
    public Guid CheckSessionId { get; set; }

    [Required]
    [StringLength(50)]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    public int CheckedQuantity { get; set; }

    [Required]
    public UnitType Unit { get; set; }

    public int? BinEntry { get; set; }

    [Required]
    public DateTime CheckedAt { get; set; }

    [Required]
    public Guid CheckedByUserId { get; set; }

    // Navigation properties
    public virtual PickListCheckSession CheckSession { get; set; } = null!;
    public virtual User CheckedByUser { get; set; } = null!;
}