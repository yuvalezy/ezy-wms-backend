namespace Core.DTOs;

public class ItemBinStockResponse {
    public required string BinCode  { get; set; }
    public          int    Quantity { get; set; }
}