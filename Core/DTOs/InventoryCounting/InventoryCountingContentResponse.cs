namespace Core.DTOs.InventoryCounting;

public class InventoryCountingContentResponse {
    public string  ItemCode        { get; set; } = string.Empty;
    public string  ItemName        { get; set; } = string.Empty;
    public int?    BinEntry        { get; set; }
    public string? BinCode         { get; set; }
    public int     SystemQuantity  { get; set; }
    public int     CountedQuantity { get; set; }
    public int     Variance        { get; set; }
    public decimal SystemValue     { get; set; }
    public decimal CountedValue    { get; set; }
    public decimal VarianceValue   { get; set; }
    public string? BuyUnitMsr      { get; set; }
    public int     NumInBuy        { get; set; }
    public string? PurPackMsr      { get; set; }
    public int     PurPackUn       { get; set; }

    public Dictionary<string, object> CustomFields { get; set; } = new();
}