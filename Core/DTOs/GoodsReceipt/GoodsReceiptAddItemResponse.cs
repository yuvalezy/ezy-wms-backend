namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptAddItemResponse {
    public Guid?   LineId         { get; set; }
    public bool    ClosedDocument { get; set; }
    public bool    Fulfillment    { get; set; }
    public bool    Showroom       { get; set; }
    public bool    Warehouse      { get; set; }
    public decimal Quantity       { get; set; }
    public decimal     NumInBuy       { get; set; }
    public string? BuyUnitMsr     { get; set; }
    public decimal     PurPackUn      { get; set; }
    public string? PurPackMsr     { get; set; }
    public decimal     Factor1      { get; set; }
    public decimal     Factor2      { get; set; }
    public decimal     Factor3      { get; set; }
    public decimal     Factor4      { get; set; }
    public string? ErrorMessage   { get; set; }
    
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    // Package-related properties
    public Guid? PackageId { get; set; }
    public string? PackageBarcode { get; set; }

    public GoodsReceiptAddItemResponse() {
        
    }

    public GoodsReceiptAddItemResponse(string errorMessage, bool closedDocument = false) {
        ErrorMessage = errorMessage;
        ClosedDocument = closedDocument;
    }
}

