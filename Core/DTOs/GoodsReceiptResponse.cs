using Core.Enums;

namespace Core.DTOs;

public class GoodsReceiptResponse : ResponseBase {
    public Guid                            ID       { get; set; }
    public int                             Number   { get; set; }
    public string?                         Name     { get; set; }
    public string?                         CardCode { get; set; }
    public string?                         CardName { get; set; }
    public DateTime                        Date     { get; set; }
    public ObjectStatus                    Status   { get; set; }
    public GoodsReceiptType                Type     { get; set; }
    public string                          WhsCode  { get; set; } = string.Empty;
    public List<GoodsReceiptLineResponse>? Lines    { get; set; }
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