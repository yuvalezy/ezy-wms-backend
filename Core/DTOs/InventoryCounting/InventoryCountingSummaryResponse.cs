namespace Core.DTOs.InventoryCounting;

public class InventoryCountingSummaryResponse {
    public          Guid     CountingId         { get; set; }
    public          int      Number             { get; set; }
    public          string?  Name               { get; set; }
    public          DateTime Date               { get; set; }
    public required string   WhsCode            { get; set; }
    public          int      TotalLines         { get; set; }
    public          int      ProcessedLines     { get; set; }
    public          int      VarianceLines      { get; set; }
    public          decimal  TotalSystemValue   { get; set; }
    public          decimal  TotalCountedValue  { get; set; }
    public          decimal  TotalVarianceValue { get; set; }

    public List<InventoryCountingReportLine>? Lines { get; set; }
}

public class InventoryCountingReportLine {
    public required string                      ItemCode     { get; set; }
    public required string                      ItemName     { get; set; }
    public required string                      BinCode      { get; set; }
    public          decimal                     Quantity     { get; set; }
    public          string?                     BuyUnitMsr   { get; set; }
    public          decimal                     NumInBuy     { get; set; }
    public          string?                     PurPackMsr   { get; set; }
    public          decimal                     PurPackUn    { get; set; }
    public          Dictionary<string, object>? CustomFields { get; set; }
}