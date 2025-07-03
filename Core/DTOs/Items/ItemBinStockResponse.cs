using Core.DTOs.Package;

namespace Core.DTOs.Items;

public class ItemBinStockResponse {
    public required string BinCode { get; set; }

    public int                  BinEntry { get; set; }
    public int                  Quantity { get; set; }
    public PackageStockValue[]? Packages { get; set; }
}