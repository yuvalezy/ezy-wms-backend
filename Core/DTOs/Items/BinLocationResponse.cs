namespace Core.DTOs.Items;

public class BinLocationResponse {
    public int Entry { get; set; }
    public required string Code { get; set; }
}

public class BinLocationResponseQuantity : BinLocationResponse {
    public decimal Quantity { get; set; }
}

public class ItemBinLocationResponseQuantity : BinLocationResponseQuantity {
    public string ItemCode { get; set; } = string.Empty;
}

public class PickingSelectionResponse {
    public required string ItemCode { get; set; }
    public int BinEntry { get; set; }
    public decimal Quantity { get; set; }
    public required string CodeBars { get; set; }
    public decimal NumInBuy { get; set; }
    public decimal PackUn { get; set; }
    public decimal Factor1 { get; set; }
    public decimal Factor2 { get; set; }
    public decimal Factor3 { get; set; }
    public decimal Factor4 { get; set; }
}