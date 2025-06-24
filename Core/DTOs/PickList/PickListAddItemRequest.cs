using Core.Enums;

namespace Core.DTOs.PickList;

public class PickListAddItemRequest {
    public int ID { get; set; }
    public int Type { get; set; }
    public int Entry { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? BinEntry { get; set; }
    public UnitType Unit { get; set; }
    public int? PickEntry { get; set; }
}