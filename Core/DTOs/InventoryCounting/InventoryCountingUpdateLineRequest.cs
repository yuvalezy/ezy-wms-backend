namespace Core.DTOs.InventoryCounting;

public class InventoryCountingUpdateLineRequest {
    public Guid ID { get; set; }
    public Guid LineID { get; set; }
    public int? Quantity { get; set; }
    public string? Comment { get; set; }
    public Guid? CancellationReasonId { get; set; }
}