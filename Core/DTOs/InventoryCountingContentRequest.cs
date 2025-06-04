namespace Core.DTOs;

public class InventoryCountingContentRequest {
    public Guid ID { get; set; }
    public int? BinEntry { get; set; }
}