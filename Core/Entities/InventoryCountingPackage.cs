using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

public sealed class InventoryCountingPackage : BaseEntity {
    [Required]
    [ForeignKey("InventoryCounting")]
    public Guid InventoryCountingId { get; set; }
    
    [Required]
    [ForeignKey("Package")]
    public Guid PackageId { get; set; }
    
    [Required]
    [StringLength(50)]
    public required string PackageBarcode { get; set; }
    
    // Original package location when first scanned
    [Required]
    [StringLength(50)]
    public required string OriginalWhsCode { get; set; }
    
    public int? OriginalBinEntry { get; set; }
    
    // Counted location (if package was moved)
    [Required]
    [StringLength(50)]
    public required string CountedWhsCode { get; set; }
    
    public int? CountedBinEntry { get; set; }
    
    [Required]
    public bool IsNewPackage { get; set; } // true if created during counting
    
    [Required]
    public PackageStatus OriginalStatus { get; set; }
    
    // Navigation properties
    public InventoryCounting InventoryCounting { get; set; } = null!;
    public Package Package { get; set; } = null!;
    public ICollection<InventoryCountingPackageContent> Contents { get; set; } = new List<InventoryCountingPackageContent>();
}

public sealed class InventoryCountingPackageContent : BaseEntity {
    [Required]
    [ForeignKey("InventoryCountingPackage")]
    public Guid InventoryCountingPackageId { get; set; }
    
    [Required]
    [StringLength(50)]
    public required string ItemCode { get; set; }
    
    [Required]
    [Column(TypeName = "DECIMAL(18,6)")]
    public decimal CountedQuantity { get; set; }
    
    [Column(TypeName = "DECIMAL(18,6)")]
    public decimal? OriginalQuantity { get; set; } // null for new packages
    
    [Required]
    public UnitType Unit { get; set; }
    
    // Navigation property
    public InventoryCountingPackage InventoryCountingPackage { get; set; } = null!;
}