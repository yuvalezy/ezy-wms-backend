namespace Service.API.GoodsReceipt.Models;

public class GoodsReceiptReportAll {
    public string ItemCode   { get; set; }
    public string ItemName   { get; set; }
    public int    Delivery   { get; set; }
    public int    Showroom   { get; set; }
    public int    Stock      { get; set; }
    public int    Quantity   { get; set; }
    public int    PackUnit   { get; set; }
    public string BuyUnitMsr { get; set; }
}