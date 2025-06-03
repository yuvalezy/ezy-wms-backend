namespace Core.DTOs;

public class ValidateAddItemResult {
    public bool   IsValidItem        { get; set; }
    public bool   IsValidBarCode     { get; set; }
    public bool   IsInventoryItem    { get; set; }
    public bool   ItemExistsInWarehouse { get; set; }
    public bool   BinExists          { get; set; }
    public bool   BinBelongsToWarehouse { get; set; }
    public decimal AvailableQuantity    { get; set; }
    public int    NumInBuy           { get; set; } = 1;
    public int    PurPackUn          { get; set; } = 1;
}