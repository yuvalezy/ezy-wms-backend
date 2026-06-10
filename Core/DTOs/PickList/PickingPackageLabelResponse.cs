namespace Core.DTOs.PickList;

public class PickingPackageLabelResponse {
    public Guid Id { get; set; }
    public int AbsEntry { get; set; }
    public required string WhsCode { get; set; }
    public required string Code { get; set; }
    public int Sequence { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LineCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public List<PickingPackageLabelItemResponse> Items { get; set; } = [];
}

public class PickingPackageLabelItemResponse {
    public required string ItemCode { get; set; }
    public decimal ScannedQuantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public Core.Enums.UnitType Unit { get; set; }
    public int? BinEntry { get; set; }
    public int LineCount { get; set; }
}
