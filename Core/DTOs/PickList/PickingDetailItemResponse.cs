namespace Core.DTOs.PickList;

public class PickingDetailItemResponse {
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int Picked { get; set; }
    public int OpenQuantity { get; set; }
    public int NumInBuy { get; set; }
    public string BuyUnitMsr { get; set; } = string.Empty;
    public int PurPackUn { get; set; }
    public string PurPackMsr { get; set; } = string.Empty;
}