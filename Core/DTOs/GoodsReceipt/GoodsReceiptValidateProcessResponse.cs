using Core.Enums;
using Core.Models;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptValidateProcessResponse {
    public          int                   DocumentNumber { get; set; }
    public required ExternalValue<string> Vendor         { get; set; }
    public          int                   BaseType       { get; set; }
    public          int                   BaseEntry      { get; set; }

    public List<GoodsReceiptValidateProcessLineResponse>? Lines { get; } = [];
}

public class GoodsReceiptValidateProcessLineResponse {
    public          int      LineNumber { get; set; }
    public required string   ItemCode   { get; set; }
    public          string?  ItemName   { get; set; }
    public          decimal  Quantity   { get; set; }
    public          int      BaseLine   { get; set; }
    public          decimal  OpenInvQty { get; set; }
    public          int      NumInBuy   { get; set; }
    public          string?  BuyUnitMsr { get; set; }
    public          int      PurPackUn  { get; set; }
    public          string?  PurPackMsr { get; set; }
    public          UnitType UnitType   { get; set; }

    public GoodsReceiptValidateProcessLineStatus LineStatus { get; set; }
}

public class GoodsReceiptValidateProcessLineDetails {
    public          DateTime TimeStamp         { get; set; }
    public required string   CreatedByUserName { get; set; }
    public          decimal  Quantity          { get; set; }
    public          decimal  ScannedQuantity   { get; set; }
    public          UnitType Unit              { get; set; }
}

public enum GoodsReceiptValidateProcessLineStatus {
    OK          = 0,
    LessScan    = 1,
    MoreScan    = 2,
    ClosedLine  = 3,
    NotReceived = 4
}