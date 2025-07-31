using Core.DTOs.Package;

namespace Core.DTOs.Items;

public class ItemStockResponse {
    public int Quantity { get; set; }
    public PackageStockValue[]? Packages { get; set; }
}