namespace Service.API.Models;

public class BinContent {
    public string ItemCode   { get; set; }
    public string ItemName   { get; set; }
    public double OnHand     { get; set; }
    public int    PackUnit   { get; set; }
    public string BuyUnitMsr { get; set; }
}