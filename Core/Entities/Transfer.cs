using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;

public sealed class Transfer : BaseEntity {
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Number { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(4000)]
    public string? Comments { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public ObjectStatus Status { get; set; } = ObjectStatus.Open;

    [Required]
    [StringLength(8)]
    public required string WhsCode { get; set; } = string.Empty;

    [StringLength(8)]
    public string? TargetWhsCode { get; set; }

    /// <summary>
    /// SAP's key for the posted stock transfer. Null until the transfer reaches
    /// <see cref="ObjectStatus.Finished"/>, and on transfers posted before this column existed.
    /// </summary>
    public int? SapDocEntry { get; set; }

    /// <inheritdoc cref="SapDocEntry"/>
    public int? SapDocNumber { get; set; }

    // Navigation properties
    public ICollection<TransferLine> Lines { get; set; } = new List<TransferLine>();
}
public sealed class TransferLine : BaseEntity, IScannedQuantityLine {
    [StringLength(254)]
    public string? BarCode { get; set; }

    public int? BinEntry { get; set; }

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

    /// <summary>
    /// The count physically scanned, in <see cref="UnitType"/>. <see cref="Quantity"/> is derived from it and
    /// the item's conversion factor, so a factor corrected in SAP can restate the pieces without touching the
    /// count. Null on rows created before this column existed, and on lines whose base was typed directly into
    /// the target-detail screen — those have no scan behind them and must not be restated.
    /// </summary>
    public decimal? ScannedQuantity { get; set; }

    public int? StatusReason { get; set; }

    [ForeignKey("CancellationReason")]
    public Guid? CancellationReasonId { get; set; }

    [Required]
    public SourceTarget Type { get; set; } = SourceTarget.Source;

    [Required]
    public UnitType UnitType { get; set; } = UnitType.Pack;

    // This line's rung is called UnitType here but Unit on every other scanned line; map rather than rename a
    // column that DTOs and the frontend already speak.
    UnitType IScannedQuantityLine.Unit => UnitType;

    [ForeignKey("Transfer")]
    public Guid TransferId { get; set; }

    // Navigation properties
    public Transfer Transfer { get; set; } = null!;
    public CancellationReason? CancellationReason { get; set; }
}
