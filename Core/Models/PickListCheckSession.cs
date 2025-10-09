using Core.Enums;

namespace Core.Models;

public class PickListCheckSession {
    public int PickListId { get; set; }
    public Guid StartedByUserId { get; set; }
    public string StartedByUserName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, PickListCheckItem> CheckedItems { get; set; } = new();
    public bool IsCompleted { get; set; }
}

public class PickListCheckItem {
    public string ItemCode { get; set; } = string.Empty;
    public decimal CheckedQuantity { get; set; }
    public UnitType Unit { get; set; }
    public int? BinEntry { get; set; }
    public DateTime CheckedAt { get; set; }
    public Guid CheckedByUserId { get; set; }
}