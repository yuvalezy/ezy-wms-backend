namespace Core.Models;

public record BinValidation(int Entry, string Warehouse, decimal Stock, string? BinCode);

public record ItemValidation(string ItemCode, string? ItemName, string? MainBarcode, bool StockItem, int NumInBuy, int PurPackUn, string? Barcode, string? Warehouse);