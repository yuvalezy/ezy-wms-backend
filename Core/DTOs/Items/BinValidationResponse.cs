namespace Core.DTOs.Items;

public record BinValidationResponse(int Entry, string Warehouse, decimal Stock, string? BinCode);

public record ItemValidationResposne(
    string ItemCode,
    string? ItemName,
    string? MainBarcode,
    bool StockItem,
    decimal NumInBuy,
    decimal PurPackUn,
    string? Barcode,
    string? Warehouse,
    decimal factor1,
    decimal factor2,
    decimal factor3,
    decimal factor4);