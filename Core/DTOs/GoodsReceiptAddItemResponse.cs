using Core.Enums;

namespace Core.DTOs;

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
}
