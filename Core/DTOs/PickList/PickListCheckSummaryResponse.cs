using Core.Enums;

namespace Core.DTOs.PickList;

public class PickListCheckSummaryResponse {
    public int PickListId { get; set; }
    public DateTime CheckStartedAt { get; set; }
    public string CheckStartedBy { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ItemsChecked { get; set; }
    public int DiscrepancyCount { get; set; }
    public List<PickListCheckItemDetail> Items { get; set; } = new();
}

public class PickListCheckItemDetail {
    public required string ItemCode { get; set; } = string.Empty;
    public required string ItemName { get; set; } = string.Empty;
    public decimal PickedQuantity { get; set; }
    public decimal CheckedQuantity { get; set; }
    public decimal Difference { get; set; }
    public required string UnitMeasure { get; set; } = string.Empty;
    public required decimal QuantityInUnit { get; set; } = 1;
    public required string PackMeasure { get; set; } = string.Empty;
    public required decimal QuantityInPack { get; set; } = 1;
    public required decimal Factor1 { get; set; } = 1;
    public required decimal Factor2 { get; set; } = 1;
    public required decimal Factor3 { get; set; } = 1;
    public required decimal Factor4 { get; set; } = 1;
}