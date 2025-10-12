namespace Core.DTOs.Items;

public class ItemResponse : IResponseCustomFields {
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal NumInBuy { get; set; }
    public string BuyUnitMsr { get; set; } = string.Empty;
    public decimal PurPackUn { get; set; }
    public string PurPackMsr { get; set; } = string.Empty;
    public decimal Factor1 { get; set; }
    public decimal Factor2 { get; set; }
    public decimal Factor3 { get; set; }
    public decimal Factor4 { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

public interface IResponseCustomFields {
    public Dictionary<string, object> CustomFields { get; set; }
}