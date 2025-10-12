using Core.Enums;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptReportAllResponse {
    public ObjectStatus Status { get; set; }
    public ICollection<GoodsReceiptReportAllResponseLine> Lines { get; set; } = [];
}

public class GoodsReceiptReportAllResponseLine {
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Delivery { get; set; }
    public decimal Showroom { get; set; }
    public decimal Stock { get; set; }
    public decimal NumInBuy { get; set; }
    public string BuyUnitMsr { get; set; } = string.Empty;
    public decimal PurPackUn { get; set; }
    public string PurPackMsr { get; set; } = string.Empty;
    public decimal Factor1 { get; set; }
    public decimal Factor2 { get; set; }
    public decimal Factor3 { get; set; }
    public decimal Factor4 { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}