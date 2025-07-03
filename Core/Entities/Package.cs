using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

public class Package : BaseEntity {
    [Required]
    [StringLength(50)]
    public required string Barcode { get; set; }

    [Required]
    public PackageStatus Status { get; set; } = PackageStatus.Init;

    [Required]
    [StringLength(50)]
    public required string WhsCode { get; set; }

    public int? BinEntry { get; set; }

    [Required]
    public required Guid CreatedBy { get; set; }

    public DateTime? ClosedAt { get; set; }

    public Guid? ClosedBy { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // JSON field for custom attributes
    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? CustomAttributes { get; set; }

    // Navigation properties
    public virtual ICollection<PackageContent>         Contents        { get; set; } = new List<PackageContent>();
    public virtual ICollection<PackageTransaction>     Transactions    { get; set; } = new List<PackageTransaction>();
    public virtual ICollection<PackageLocationHistory> LocationHistory { get; set; } = new List<PackageLocationHistory>();
}

public class PackageContent : BaseEntity {
    [Required]
    [ForeignKey("Package")]
    public Guid PackageId { get; set; }

    [Required]
    [StringLength(50)]
    public required string ItemCode { get; set; }

    [Required]
    [Column(TypeName = "DECIMAL(18,6)")]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(50)]
    public required string WhsCode { get; set; }

    public int? BinEntry { get; set; }

    [Required]
    public required Guid CreatedBy { get; set; }

    // Navigation property
    public virtual Package Package { get; set; } = null!;
}

public class PackageTransaction : BaseEntity {
    [Required]
    [ForeignKey("Package")]
    public Guid PackageId { get; set; }

    [Required]
    public PackageTransactionType TransactionType { get; set; }

    [Required]
    [StringLength(50)]
    public required string ItemCode { get; set; }

    [Required]
    [Column(TypeName = "DECIMAL(18,6)")]
    public decimal Quantity { get; set; } // Positive for Add, Negative for Remove

    [Required]
    [Column(TypeName = "DECIMAL(18,6)")]
    public decimal UnitQuantity { get; set; } // Positive for Add, Negative for Remove

    [Required]
    public required UnitType UnitType { get; set; }

    [Required]
    public ObjectType SourceOperationType { get; set; } // GoodsReceipt, Counting, Transfer, Picking, Package

    public Guid? SourceOperationId { get; set; }

    public Guid? SourceOperationLineId { get; set; }

    [Required]
    public required Guid UserId { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation property
    public virtual Package Package { get; set; } = null!;
}

public class PackageLocationHistory : BaseEntity {
    [Required]
    [ForeignKey("Package")]
    public Guid PackageId { get; set; }

    [Required]
    public PackageMovementType MovementType { get; set; }

    [StringLength(50)]
    public string? FromWhsCode { get; set; }

    public int? FromBinEntry { get; set; }

    [Required]
    [StringLength(50)]
    public required string ToWhsCode { get; set; }

    public int? ToBinEntry { get; set; }

    [Required]
    public ObjectType SourceOperationType { get; set; } // GoodsReceipt, Transfer, Package

    public Guid? SourceOperationId { get; set; }

    [Required]
    public required Guid UserId { get; set; }

    [Required]
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation property
    public virtual Package Package { get; set; } = null!;
}

public class PackageInconsistency : BaseEntity {
    [Required]
    [ForeignKey("Package")]
    public Guid PackageId { get; set; }

    [Required]
    [StringLength(50)]
    public required string PackageBarcode { get; set; }

    [StringLength(50)]
    public string? ItemCode { get; set; }

    [StringLength(50)]
    public string? BatchNo { get; set; }

    [StringLength(50)]
    public string? SerialNo { get; set; }

    [StringLength(50)]
    public string? WhsCode { get; set; }

    public int? BinEntry { get; set; }

    [Column(TypeName = "DECIMAL(18,6)")]
    public decimal? SapQuantity { get; set; }

    [Column(TypeName = "DECIMAL(18,6)")]
    public decimal? WmsQuantity { get; set; }

    [Column(TypeName = "DECIMAL(18,6)")]
    public decimal? PackageQuantity { get; set; }

    [Required]
    public InconsistencyType InconsistencyType { get; set; }

    [Required]
    public InconsistencySeverity Severity { get; set; }

    [Required]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public bool IsResolved { get; set; } = false;

    public DateTime? ResolvedAt { get; set; }

    [StringLength(50)]
    public string? ResolvedBy { get; set; }

    [StringLength(500)]
    public string? ResolutionAction { get; set; }

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Navigation property
    public virtual Package Package { get; set; } = null!;
}