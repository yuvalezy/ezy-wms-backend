namespace Core.Models;

public class ItemCheckResponse {
    public string       ItemCode   { get; set; }
    public string       ItemName   { get; set; }
    public int          NumInBuy   { get; set; }
    public string       BuyUnitMsr { get; set; }
    public int          PurPackUn  { get; set; }
    public string       PurPackMsr { get; set; }
    public List<string> Barcodes   { get; set; } = [];
}