using Core.Enums;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptReportAllResponse {
    public ObjectStatus                                   Status { get; set; }
    public ICollection<GoodsReceiptReportAllResponseLine> Lines  { get; set; } = [];
}

public class GoodsReceiptReportAllResponseLine {
    public string                      ItemCode     { get; set; } = string.Empty;
    public string                      ItemName     { get; set; } = string.Empty;
    public decimal                     Quantity     { get; set; }
    public decimal                     Delivery     { get; set; }
    public decimal                     Showroom     { get; set; }
    public decimal                     Stock        { get; set; }
    public int                         NumInBuy     { get; set; }
    public string                      BuyUnitMsr   { get; set; } = string.Empty;
    public int                         PurPackUn    { get; set; }
    public string                      PurPackMsr   { get; set; } = string.Empty;
    public Dictionary<string, object>? CustomFields { get; set; }
}