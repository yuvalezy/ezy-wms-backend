namespace Core.DTOs;

public class GoodsReceiptReportAllDetailsResponse {
    public Guid LineID { get; set; }
    public string BarCode { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Quantity { get; set; }
    public string? Comments { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CancellationReason { get; set; }
}