namespace Core.DTOs.Items;

public class ItemUnitResponse {
    public required string UnitMeasure    { get; set; } = string.Empty;
    public required int    QuantityInUnit { get; set; } = 1;
    public required string PackMeasure    { get; set; } = string.Empty;
    public required int    QuantityInPack { get; set; } = 1;
}