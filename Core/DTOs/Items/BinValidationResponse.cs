namespace Core.DTOs.Items;

public record BinValidationResponse(int Entry, string Warehouse, decimal Stock, string? BinCode);

public record ItemValidationResposne(string ItemCode, string? ItemName, string? MainBarcode, bool StockItem, int NumInBuy, int PurPackUn, string? Barcode, string? Warehouse);