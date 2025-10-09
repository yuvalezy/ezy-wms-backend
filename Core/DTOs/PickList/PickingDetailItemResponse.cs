using Core.DTOs.Items;

namespace Core.DTOs.PickList;

public class PickingDetailItemResponse : ItemResponse {
    public decimal Quantity     { get; set; }
    public decimal Picked       { get; set; }
    public decimal OpenQuantity { get; set; }
}