namespace Core.DTOs.InventoryCounting;

public class RetryBatchRequest {
    public Guid CountingId { get; set; }
    public Guid? BatchId { get; set; }
}
