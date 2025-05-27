namespace Service.API.Models;

public class BinContent {
    public string ItemCode   { get; set; }
    public string ItemName   { get; set; }
    public double OnHand     { get; set; }
    public int    NumInBuy   { get; set; }
    public string BuyUnitMsr { get; set; }
    public int    PurPackUn  { get; set; }
    public string PurPackMsr { get; set; }
}