namespace Core.DTOs;

public class GoodsReceiptValidateProcessResponse {
    public Guid LineID { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string BarCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public List<GoodsReceiptSourceInfo>? Sources { get; set; }
}

public class GoodsReceiptSourceInfo {
    public int SourceType { get; set; }
    public int SourceEntry { get; set; }
    public int SourceLine { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}