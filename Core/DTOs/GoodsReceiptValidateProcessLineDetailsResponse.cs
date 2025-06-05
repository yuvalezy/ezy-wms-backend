namespace Core.DTOs;

public class GoodsReceiptValidateProcessLineDetailsResponse {
    public int SourceType { get; set; }
    public int SourceEntry { get; set; }
    public int SourceLine { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal OpenQuantity { get; set; }
}