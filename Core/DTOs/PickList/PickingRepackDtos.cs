using Core.Enums;

namespace Core.DTOs.PickList;

public class PickingRepackSummaryResponse {
    public int PickListId { get; set; }
    public bool Started { get; set; }
    public bool Completed { get; set; }
    public DateTime? StartedAt { get; set; }
    public string? StartedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalLines { get; set; }
    public int AssignedLines { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal AssignedQuantity { get; set; }
    public List<PickingPackageLabelResponse> Labels { get; set; } = [];
    public List<PickingRepackItemResponse> Items { get; set; } = [];
}

public class PickingRepackItemResponse {
    public required string ItemCode { get; set; }
    public UnitType Unit { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal AssignedQuantity { get; set; }
    public int TotalLines { get; set; }
    public int AssignedLines { get; set; }
}

public class PickingRepackAssignRequest {
    public Guid PickingPackageLabelId { get; set; }
    public required string ItemCode { get; set; }
    public UnitType Unit { get; set; }
}

public class PickingRepackAssignResponse {
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public PickingRepackSummaryResponse? Summary { get; set; }
}
