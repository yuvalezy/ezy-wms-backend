using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Entities;

public sealed class InventoryCountingBatch : BaseEntity {
    public int SequenceOrder { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Pending;
    public bool IsInitialBinBatch { get; set; }
    public int LineCount { get; set; }
    public int? SapDocEntry { get; set; }
    public int? SapDocNumber { get; set; }

    [MaxLength(4000)]
    public string? ErrorMessage { get; set; }

    public DateTime? LastAttemptAt { get; set; }
    public int RetryCount { get; set; }
    public string PayloadJson { get; set; } = string.Empty;

    public Guid InventoryCountingId { get; set; }
    public InventoryCounting InventoryCounting { get; set; } = null!;
}
