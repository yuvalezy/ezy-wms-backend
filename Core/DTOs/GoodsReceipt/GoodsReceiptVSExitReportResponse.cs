namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptVSExitReportResponse {
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal ReceivedQuantity { get; set; }
    public decimal ExitQuantity { get; set; }
    public decimal Variance { get; set; }
    public string? SourceDocument { get; set; }
}