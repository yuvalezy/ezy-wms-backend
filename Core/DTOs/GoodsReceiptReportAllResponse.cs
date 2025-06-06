namespace Core.DTOs;

public class GoodsReceiptReportAllResponse {
    public string  ItemCode   { get; set; } = string.Empty;
    public string  ItemName   { get; set; } = string.Empty;
    public decimal Quantity   { get; set; }
    public decimal Delivery   { get; set; }
    public decimal Showroom   { get; set; }
    public decimal Stock     { get; set; }
    public int     NumInBuy   { get; set; }
    public string  BuyUnitMsr { get; set; } = string.Empty;
    public int     PurPackUn  { get; set; }
    public string  PurPackMsr { get; set; } = string.Empty;
}