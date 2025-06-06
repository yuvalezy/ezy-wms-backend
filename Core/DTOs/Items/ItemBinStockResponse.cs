namespace Core.DTOs.Items;

public class ItemBinStockResponse {
    public required string BinCode  { get; set; }
    public          int    Quantity { get; set; }
}