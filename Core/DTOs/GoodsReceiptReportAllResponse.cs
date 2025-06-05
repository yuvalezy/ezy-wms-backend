namespace Core.DTOs;

public class GoodsReceiptReportAllResponse {
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal ScannedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal Variance { get; set; }
    public int LineCount { get; set; }
}