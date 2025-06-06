namespace Core.Models;

public class GoodsReceiptAddItemSourceDocument {
    public int Type     { get; set; }
    public int Entry    { get; set; }
    public int LineNum  { get; set; }
    public int Quantity { get; set; }
}