using System.Runtime.InteropServices.JavaScript;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptAddItemResponse {
    public Guid?   LineId         { get; set; }
    public bool    ClosedDocument { get; set; }
    public bool    Fulfillment    { get; set; }
    public bool    Showroom       { get; set; }
    public bool    Warehouse      { get; set; }
    public int     Quantity       { get; set; }
    public int     NumInBuy       { get; set; }
    public string? BuyUnitMsr     { get; set; }
    public int     PurPackUn      { get; set; }
    public string? PurPackMsr     { get; set; }
    public string? ErrorMessage   { get; set; }
    
    public Dictionary<string, object> CustomFields { get; set; } = new();

    public GoodsReceiptAddItemResponse() {
        
    }

    public GoodsReceiptAddItemResponse(string errorMessage, bool closedDocument = false) {
        ErrorMessage = errorMessage;
        ClosedDocument = closedDocument;
    }
}

