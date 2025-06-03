using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

public class Transfer : BaseEntity {
    [MaxLength(4000)]
    public string? Comments { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public ObjectStatus Status { get; set; } = ObjectStatus.Open;

    public DateTime? StatusDate { get; set; }

    [ForeignKey("StatusUserId")]
    public Guid? StatusUserId { get; set; }

    public User? StatusUser { get; set; }

    [Required]
    [StringLength(8)]
    public required string WhsCode { get; set; } = string.Empty;

    // Navigation property
    public virtual ICollection<TransferLine> Lines { get; set; } = new List<TransferLine>();
}

public class TransferLine : BaseEntity {
    [Required]
    [StringLength(254)]
    public required string BarCode { get; set; }

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
    public int Quantity { get; set; }

    public int? StatusReason { get; set; }

    public DateTime? StatusTimeStamp { get; set; }

    [ForeignKey("StatusUserId")]
    public Guid? StatusUserId { get; set; }

    public User? StatusUser { get; set; }

    [Required]
    public SourceTarget Type { get; set; } = SourceTarget.Source;

    [Required]
    public UnitType UnitType { get; set; } = UnitType.Pack;

    [ForeignKey("Transfer")]
    public required Guid TransferId { get; set; }

    // Navigation property
    public virtual Transfer Transfer { get; set; } = null!;
}