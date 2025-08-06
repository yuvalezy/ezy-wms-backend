using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptAddItemRequest {
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(50)]
    public required string ItemCode { get; set; }

    [StringLength(254)]
    public string? BarCode { get; set; }

    [Required]
    public UnitType Unit { get; set; } = UnitType.Pack;

    // Package-related properties
    public Guid? PackageId { get; set; }
    public bool StartNewPackage { get; set; }
}