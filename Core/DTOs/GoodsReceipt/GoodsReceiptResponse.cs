using Core.Enums;
using Core.Models;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptResponse {
    public Guid                                ID                { get; set; }
    public int                                 Number            { get; set; }
    public string?                             Name              { get; set; }
    public ExternalValue<string>?              Vendor            { get; set; }
    public DateTime                            Date              { get; set; }
    public ObjectStatus                        Status            { get; set; }
    public GoodsReceiptType                    Type              { get; set; }
    public string                              WhsCode           { get; set; } = string.Empty;
    public List<GoodsReceiptLineResponse>?     Lines             { get; set; }
    public List<GoodsReceiptDocumentResponse>? Documents         { get; set; }
    public string?                             CreatedByUserName { get; set; }
    public string?                             ErrorMessage      { get; set; }
}

public class GoodsReceiptLineResponse {
    public Guid       ID                   { get; set; }
    public string     BarCode              { get; set; } = string.Empty;
    public string     ItemCode             { get; set; } = string.Empty;
    public string     ItemName             { get; set; } = string.Empty;
    public decimal    Quantity             { get; set; }
    public UnitType   Unit                 { get; set; }
    public LineStatus LineStatus           { get; set; }
    public DateTime   Date                 { get; set; }
    public string?    Comments             { get; set; }
    public int?       StatusReason         { get; set; }
    public Guid?      CancellationReasonId { get; set; }
}

public class GoodsReceiptDocumentResponse {
    public int DocumentEntry  { get; set; }
    public int DocumentNumber { get; set; }
    public int ObjectType     { get; set; }
}