using Core.Enums;

namespace Core.Models;

public class PickingCreationData {
    public string ItemCode { get; set; } = string.Empty;
    public int PickEntry { get; set; }
    public int Quantity { get; set; }
    public int BinEntry { get; set; }
    public int BaseObject { get; set; }
    public int OrderEntry { get; set; }
    public int OrderLine { get; set; }
    public UnitType Unit { get; set; }
}