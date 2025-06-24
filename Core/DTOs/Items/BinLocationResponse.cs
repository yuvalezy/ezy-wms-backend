namespace Core.DTOs.Items;

public class BinLocationResponse {
    public int    Entry { get; set; }
    public required string Code  { get; set; }
}


public class BinLocationResponseQuantity : BinLocationResponse {
    public int    Quantity { get; set; }
}
public class ItemBinLocationResponseQuantity : BinLocationResponseQuantity {
    public string ItemCode { get; set; } = string.Empty;
}
