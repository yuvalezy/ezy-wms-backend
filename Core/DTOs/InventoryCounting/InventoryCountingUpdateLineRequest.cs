namespace Core.DTOs.InventoryCounting;

public class InventoryCountingUpdateLineRequest {
    public Guid Id { get; set; }
    public Guid LineId { get; set; }
    public decimal? Quantity { get; set; }
    public string? Comment { get; set; }
    public Guid? CancellationReasonId { get; set; }
}