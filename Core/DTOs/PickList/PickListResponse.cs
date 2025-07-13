using Core.Entities;
using Core.Enums;

namespace Core.DTOs.PickList;

public class PickListResponse {
    public int                           Entry          { get; set; }
    public DateTime                      Date           { get; set; }
    public int                           SalesOrders    { get; set; }
    public int                           Invoices       { get; set; }
    public int                           Transfers      { get; set; }
    public string?                       Remarks        { get; set; }
    public ObjectStatus                  Status         { get; set; }
    public SyncStatus                    SyncStatus     { get; set; }
    public int                           Quantity       { get; set; }
    public int                           OpenQuantity   { get; set; }
    public int                           UpdateQuantity { get; set; }
    public List<PickListDetailResponse>? Detail         { get; set; }
    public bool                          PickPackOnly   { get; set; }
}

public class PickListDetailResponse {
    public int                               Type           { get; set; }
    public int                               Entry          { get; set; }
    public int                               Number         { get; set; }
    public DateTime                          Date           { get; set; }
    public string                            CardCode       { get; set; } = string.Empty;
    public string                            CardName       { get; set; } = string.Empty;
    public int                               TotalItems     { get; set; }
    public int                               TotalOpenItems { get; set; }
    public List<PickListDetailItemResponse>? Items          { get; set; }
}

public class PickListDetailItemResponse {
    public string                             ItemCode      { get; set; } = string.Empty;
    public string                             ItemName      { get; set; } = string.Empty;
    public int                                Quantity      { get; set; }
    public int                                Picked        { get; set; }
    public int                                OpenQuantity  { get; set; }
    public int                                NumInBuy      { get; set; }
    public string                             BuyUnitMsr    { get; set; } = string.Empty;
    public int                                PurPackUn     { get; set; }
    public string                             PurPackMsr    { get; set; } = string.Empty;
    public List<BinLocationQuantityResponse>? BinQuantities { get; set; }
    public int?                               Available     { get; set; }

    public required Dictionary<string, object>            CustomFields { get; set; }
    public          BinLocationPackageQuantityResponse[]? Packages     { get; set; }
}

public class BinLocationQuantityResponse {
    public int                                   Entry    { get; set; }
    public string                                Code     { get; set; } = string.Empty;
    public int                                   Quantity { get; set; }
    public BinLocationPackageQuantityResponse[]? Packages { get; set; }
}

public record BinLocationPackageQuantityResponse(Guid Id, string Barcode, int BinEntry, string ItemCode, decimal Quantity);