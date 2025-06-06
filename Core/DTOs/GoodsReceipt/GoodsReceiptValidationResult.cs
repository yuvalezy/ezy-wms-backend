namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptValidationResult {
    public bool    IsValid      { get; set; }
    public string? ErrorMessage { get; set; }
    public int     ReturnValue  { get; set; }
}

public class ProcessGoodsReceiptResult {
    public bool    Success        { get; set; }
    public int?    DocumentNumber { get; set; }
    public string? ErrorMessage   { get; set; }
}