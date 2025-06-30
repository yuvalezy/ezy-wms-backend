using Core.Enums;

namespace Core.DTOs.Items;

public class BinLocationResponse {
    public          int    Entry { get; set; }
    public required string Code  { get; set; }
}

public class BinLocationResponseQuantity : BinLocationResponse {
    public int Quantity { get; set; }
}

public class ItemBinLocationResponseQuantity : BinLocationResponseQuantity {
    public string ItemCode { get; set; } = string.Empty;
}

public class PickingSelectionResponse {
    public required string ItemCode { get; set; }
    public          int    BinEntry { get; set; }
    public          int    Packs    { get; set; }
    public          int    Dozens   { get; set; }
    public          int    Units    { get; set; }
    public required string CodeBars { get; set; }
}