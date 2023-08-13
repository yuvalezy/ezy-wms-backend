namespace Service.Shared.PrintLayout; 

public class SpecificFilter {
    public string ItemCode    { get; set; }
    public string CardCode    { get; set; }
    public string CardCode2   { get; set; }
    public string ShipToCode  { get; set; }
    public string ShipToCode2 { get; set; }

    public SpecificFilter() {
    }

    public SpecificFilter(string itemCode = null, string cardCode = null, string shipToCode = null) {
        ItemCode   = itemCode;
        CardCode   = cardCode;
        ShipToCode = shipToCode;
    }
}