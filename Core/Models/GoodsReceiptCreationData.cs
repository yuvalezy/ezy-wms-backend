using Core.Enums;

namespace Core.Models;

public class GoodsReceiptCreationData {
    public string ItemCode { get; set; } = string.Empty;
    public string BarCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public UnitType Unit { get; set; }
    public DateTime Date { get; set; }
    public string? Comments { get; set; }
    public List<GoodsReceiptSourceData> Sources { get; set; } = new();
}

public class GoodsReceiptSourceData {
    public int SourceType { get; set; }
    public int SourceEntry { get; set; }
    public int SourceLine { get; set; }
    public decimal Quantity { get; set; }
}