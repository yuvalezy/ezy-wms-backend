namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptAddItemSourceDocumentResponse {
    public int Type   { get; set; }
    public int Entry  { get; set; }
    public int Number { get; set; }
    public int LineNum  { get; set; }
    public decimal Quantity { get; set; }
}