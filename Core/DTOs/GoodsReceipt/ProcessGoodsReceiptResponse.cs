namespace Core.DTOs.GoodsReceipt;

public class ProcessGoodsReceiptResponse : ResponseBase {
    public int? DocumentNumber { get; set; }
    public bool Success { get; set; }
    public int ActivatedPackages { get; set; }
}