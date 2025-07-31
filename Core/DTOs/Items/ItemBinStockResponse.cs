namespace Core.DTOs.Items;

public class ItemBinStockResponse : ItemStockResponse {
    public required string BinCode { get; set; }
    public int BinEntry { get; set; }
}