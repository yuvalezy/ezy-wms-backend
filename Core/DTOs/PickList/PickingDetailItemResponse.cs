using Core.DTOs.Items;

namespace Core.DTOs.PickList;

public class PickingDetailItemResponse : ItemResponse {
    public int Quantity     { get; set; }
    public int Picked       { get; set; }
    public int OpenQuantity { get; set; }
}