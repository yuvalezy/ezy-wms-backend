using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

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

    // Navigation properties
    public ICollection<TransferLine> Lines { get; set; } = new List<TransferLine>();
    public ICollection<TransferPackage> Packages { get; set; } = new List<TransferPackage>();
}

public sealed class TransferLine : BaseEntity {
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
    
    [ForeignKey("CancellationReason")]
    public Guid? CancellationReasonId { get; set; }

    [Required]
    public SourceTarget Type { get; set; } = SourceTarget.Source;

    [Required]
    public UnitType UnitType { get; set; } = UnitType.Pack;

    [ForeignKey("Transfer")]
    public Guid TransferId { get; set; }

    // Navigation properties
    public Transfer Transfer { get; set; } = null!;
    public CancellationReason? CancellationReason { get; set; }
}

public sealed class TransferPackage : BaseEntity {
    [ForeignKey("Transfer")]
    public Guid TransferId { get; set; }
    
    [Required]
    public Guid PackageId { get; set; }
    
    [Required]
    public SourceTarget Type { get; set; } = SourceTarget.Source;
    
    public int? BinEntry { get; set; }
    
    [Required]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public Guid AddedByUserId { get; set; }
    
    // Navigation properties
    public Transfer Transfer { get; set; } = null!;
}