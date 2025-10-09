using Core.Enums;

namespace Core.DTOs.PickList;

public class PickListCheckItemRequest {
    public int PickListId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public decimal CheckedQuantity { get; set; }
    public UnitType Unit { get; set; }
    public int? BinEntry { get; set; }
}