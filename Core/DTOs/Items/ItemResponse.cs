namespace Core.DTOs.Items;

public class ItemResponse {
    public string ItemCode { get; set; } = string.Empty;

    public string                     ItemName     { get; set; }
    public int                        NumInBuy     { get; set; }
    public string                     BuyUnitMsr   { get; set; }
    public int                        PurPackUn    { get; set; }
    public string                     PurPackMsr   { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
}