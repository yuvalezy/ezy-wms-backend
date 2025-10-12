namespace Core.DTOs.Items;

public class ItemUnitResponse {
    public required string ItemName { get; set; }
    public required string UnitMeasure { get; set; } = string.Empty;
    public required decimal QuantityInUnit { get; set; } = 1;
    public required string PackMeasure { get; set; } = string.Empty;
    public required decimal QuantityInPack { get; set; } = 1;
    public required decimal Factor1 { get; set; }
    public required decimal Factor2 { get; set; }
    public required decimal Factor3 { get; set; }
    public required decimal Factor4 { get; set; }
}