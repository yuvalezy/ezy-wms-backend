namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptAddItemTargetDocumentsResponse {
    public short    Priority { get; set; }
    public int      Type     { get; set; }
    public int      Entry    { get; set; }
    public int      LineNum  { get; set; }
    public DateTime Date     { get; set; }
    public int      Quantity { get; set; }
}