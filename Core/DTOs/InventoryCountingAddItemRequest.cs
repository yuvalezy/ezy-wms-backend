using Core.Enums;

namespace Core.DTOs;

public class InventoryCountingAddItemRequest {
    public Guid ID { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string BarCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? BinEntry { get; set; }
    public UnitType Unit { get; set; }
}