using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

public sealed class GoodsReceipt : BaseEntity {
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Number { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    [StringLength(15)]
    public string? CardCode { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public ObjectStatus Status { get; set; } = ObjectStatus.Open;

    [Required]
    public GoodsReceiptType Type { get; set; } = GoodsReceiptType.All;

    [Required]
    [StringLength(8)]
    public required string WhsCode { get; set; }

    // Navigation properties
    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
    public ICollection<GoodsReceiptDocument> Documents { get; set; } = new List<GoodsReceiptDocument>();
}

public sealed class GoodsReceiptLine : BaseEntity {
    [StringLength(254)]
    public string? BarCode { get; set; }

    [MaxLength(4000)]
    public string? Comments { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(50)]
    public required string ItemCode { get; set; }

    [Required]
    public LineStatus LineStatus { get; set; } = LineStatus.Open;

    [Required]
    public decimal Quantity { get; set; }

    public int? StatusReason { get; set; }

    [ForeignKey("CancellationReason")]
    public Guid? CancellationReasonId { get; set; }

    [Required]
    public UnitType Unit { get; set; } = UnitType.Pack;

    // Navigation property
    [ForeignKey("GoodsReceipt")]
    public Guid GoodsReceiptId { get; set; }

    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public CancellationReason? CancellationReason { get; set; }

    public ICollection<GoodsReceiptTarget> Targets { get; set; } = new List<GoodsReceiptTarget>();
    public ICollection<GoodsReceiptSource> Sources { get; set; } = new List<GoodsReceiptSource>();
}

public sealed class GoodsReceiptTarget : BaseEntity {
    [Required]
    [StringLength(50)]
    public required string ItemCode { get; set; }

    [Required]
    [StringLength(8)]
    public required string WhsCode { get; set; }

    [Required]
    public int TargetEntry { get; set; }

    [Required]
    public int TargetLine { get; set; }

    [Required]
    public decimal TargetQuantity { get; set; }

    [Required]
    public LineStatus TargetStatus { get; set; } = LineStatus.Open;

    [Required]
    public int TargetType { get; set; }

    // Navigation property
    [ForeignKey("GoodsReceiptLine")]
    public Guid GoodsReceiptLineId { get; set; }

    public GoodsReceiptLine GoodsReceiptLine { get; set; } = null!;
}

public sealed class GoodsReceiptDocument : BaseEntity {
    [Required]
    public int DocEntry { get; set; }

    [Required]
    public int ObjType { get; set; }

    public int DocNumber { get; set; }

    // Navigation property
    [ForeignKey("GoodsReceipt")]
    public Guid GoodsReceiptId { get; set; }

    public GoodsReceipt GoodsReceipt { get; set; } = null!;
}

public sealed class GoodsReceiptSource : BaseEntity {
    [Required]
    public decimal Quantity { get; set; }

    [Required]
    public int SourceEntry { get; set; }

    [Required]
    public int SourceNumber { get; set; }

    [Required]
    public int SourceLine { get; set; }

    [Required]
    public int SourceType { get; set; }

    // Navigation property
    [ForeignKey("GoodsReceiptLine")]
    public Guid GoodsReceiptLineId { get; set; }

    public GoodsReceiptLine GoodsReceiptLine { get; set; } = null!;
}