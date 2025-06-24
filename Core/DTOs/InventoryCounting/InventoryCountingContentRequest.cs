namespace Core.DTOs.InventoryCounting;

public class InventoryCountingContentRequest {
    public Guid ID { get; set; }
    public int? BinEntry { get; set; }
}