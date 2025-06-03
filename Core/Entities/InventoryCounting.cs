using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

public class InventoryCounting : BaseEntity {
    [Required]
    public DateTime Date { get; set; }

    [Required]
    public int InvCountEntry { get; set; } = -1;

    public ObjectStatus Status { get; set; } = ObjectStatus.Open;

    public DateTime? StatusDate { get; set; }

    [ForeignKey("StatusUserId")]
    public Guid? StatusUserId { get; set; }

    public User? StatusUser { get; set; }

    [Required]
    [StringLength(8)]
    public required string WhsCode { get; set; }

    // Navigation property
    public virtual ICollection<InventoryCountingLine> Lines { get; set; } = new List<InventoryCountingLine>();
}

public class InventoryCountingLine : BaseEntity {
    [Required]
    [StringLength(254)]
    public required string BarCode { get; set; }

    public int? BinEntry { get; set; }

    [MaxLength(4000)]
    public string? Comments { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [ForeignKey("StatusUserId")]
    public Guid? StatusUserId { get; set; }

    public User? StatusUser { get; set; }

    [Required]
    [StringLength(50)]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    public LineStatus LineStatus { get; set; } = LineStatus.Open;

    [Required]
    public int Quantity { get; set; }

    public int? StatusReason { get; set; }

    [Required]
    public UnitType Unit { get; set; } = UnitType.Pack;

    // Navigation property
    [ForeignKey("InventoryCounting")]
    public Guid InventoryCountingId { get; set; }

    public virtual InventoryCounting InventoryCounting { get; set; } = null!;
}