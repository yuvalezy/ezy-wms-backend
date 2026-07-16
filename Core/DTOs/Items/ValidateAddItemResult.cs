namespace Core.DTOs.Items;

public class ValidateAddItemResult {
    public bool IsValidItem { get; set; }
    public bool IsValidBarCode { get; set; }
    public bool IsInventoryItem { get; set; }
    public bool ItemExistsInWarehouse { get; set; }
    public bool BinExists { get; set; }
    public bool BinBelongsToWarehouse { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal NumInBuy { get; set; } = 1;
    public decimal PurPackUn { get; set; } = 1;

    // SAP's PurFactor1..4. The validation query has always read these; they are surfaced so a caller that
    // converts a scanned count here reaches the same base quantity the write path stores. Counting deliberately
    // ignores them — it has never applied factors, and adopting them would restate every counted line.
    public decimal Factor1 { get; set; } = 1;
    public decimal Factor2 { get; set; } = 1;
    public decimal Factor3 { get; set; } = 1;
    public decimal Factor4 { get; set; } = 1;

    public string? BinCode { get; set; }
}