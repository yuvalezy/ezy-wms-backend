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
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int PickedQuantity { get; set; }
    public int CheckedQuantity { get; set; }
    public int Difference { get; set; }
    public UnitType Unit { get; set; }
    public string? BinLocation { get; set; }
}