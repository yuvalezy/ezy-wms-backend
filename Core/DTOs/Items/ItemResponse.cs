namespace Core.DTOs.Items;

public class ItemResponse : IResponseCustomFields {
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int    NumInBuy   { get; set; }
    public string BuyUnitMsr { get; set; } = string.Empty;
    public int    PurPackUn  { get; set; }
    public string PurPackMsr { get; set; } = string.Empty;
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

public interface IResponseCustomFields {
    public Dictionary<string, object> CustomFields { get; set; } 
}