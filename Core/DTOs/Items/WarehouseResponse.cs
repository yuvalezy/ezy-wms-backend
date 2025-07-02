namespace Core.DTOs.Items;

public record WarehouseResponse(string Id, string Name, bool EnableBinLocations, int? DefaultBinLocation);