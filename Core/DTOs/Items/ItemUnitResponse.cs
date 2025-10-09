namespace Core.DTOs.Items;

public class ItemUnitResponse {
    public required string  ItemName       { get; set; }
    public required string  UnitMeasure    { get; set; } = string.Empty;
    public required decimal QuantityInUnit { get; set; } = 1;
    public required string  PackMeasure    { get; set; } = string.Empty;
    public required decimal QuantityInPack { get; set; } = 1;
}