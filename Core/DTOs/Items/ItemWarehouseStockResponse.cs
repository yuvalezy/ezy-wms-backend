namespace Core.DTOs.Items;

public class ItemWarehouseStockResponse {
    public required string ItemCode   { get; set; }
    public required string ItemName   { get; set; }
    public          int    Stock     { get; set; }
    public          int    NumInBuy   { get; set; }
    public required string BuyUnitMsr { get; set; }
    public          int    PurPackUn  { get; set; }
    public required string PurPackMsr { get; set; }
}