namespace Core.DTOs.Package;

public class PackageValueResponse(Guid id, string barcode) {
    public Guid   Id      { get; init; } = id;
    public string Barcode { get; init; } = barcode;
}

public class PackageStockValue(Guid id, string barcode, decimal quantity) : PackageValueResponse(id, barcode) {
    public decimal Quantity { get; init; } = quantity;
}