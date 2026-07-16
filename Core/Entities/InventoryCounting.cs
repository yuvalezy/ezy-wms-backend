using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;

public sealed class InventoryCounting : BaseEntity {
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Number { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public int InvCountEntry { get; set; } = -1;

    public ObjectStatus Status { get; set; } = ObjectStatus.Open;

    [Required]
    [StringLength(8)]
    public required string WhsCode { get; set; }

    // Navigation properties
    public ICollection<InventoryCountingLine> Lines { get; set; } = new List<InventoryCountingLine>();
    public ICollection<InventoryCountingBatch> Batches { get; set; } = new List<InventoryCountingBatch>();
}

public sealed class InventoryCountingLine : BaseEntity, IScannedQuantityLine {
    [StringLength(254)]
    public  string? BarCode { get; set; }

    public int? BinEntry { get; set; }

    [MaxLength(4000)]
    public string? Comments { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(50)]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    public LineStatus LineStatus { get; set; } = LineStatus.Open;

    [Required]
    public decimal Quantity { get; set; }

    /// <summary>
    /// The count physically entered, in <see cref="Unit"/>. <see cref="Quantity"/> is derived from it and the
    /// item's conversion factor, so a factor corrected in SAP can restate the pieces without touching the count.
    /// Null on rows created before this column existed.
    /// </summary>
    public decimal? ScannedQuantity { get; set; }

    public int? StatusReason { get; set; }

    [ForeignKey("CancellationReason")]
    public Guid? CancellationReasonId { get; set; }

    [Required]
    public UnitType Unit { get; set; } = UnitType.Pack;

    // Navigation property
    [ForeignKey("InventoryCounting")]
    public Guid InventoryCountingId { get; set; }

    public InventoryCounting InventoryCounting { get; set; } = null!;
    public CancellationReason? CancellationReason { get; set; }
}
