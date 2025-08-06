using Core.Enums;

namespace Core.DTOs.InventoryCounting;

public class InventoryCountingAddItemRequest {
    public Guid ID { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string? BarCode { get; set; }
    public int Quantity { get; set; }
    public int? BinEntry { get; set; }
    public UnitType Unit { get; set; }
    
    // Package-related properties
    public bool StartNewPackage { get; set; }
    public Guid? PackageId { get; set; }
}