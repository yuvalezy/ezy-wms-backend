using Core.Enums;

namespace Core.DTOs.InventoryCounting;

public class InventoryCountingBatchResponse {
    public Guid Id { get; set; }
    public int SequenceOrder { get; set; }
    public BatchStatus Status { get; set; }
    public bool IsInitialBinBatch { get; set; }
    public int LineCount { get; set; }
    public int? SapDocEntry { get; set; }
    public int? SapDocNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public int RetryCount { get; set; }
}
