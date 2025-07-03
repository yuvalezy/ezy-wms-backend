namespace Core.DTOs.Package;

public class PackageValueResponse(Guid id, string barcode) {
    public Guid   Id      { get; init; } = id;
    public string Barcode { get; init; } = barcode;
}

public class PackageStockValue(Guid id, string barcode, int quantity) : PackageValueResponse(id, barcode) {
    public int Quantity { get; init; } = quantity;
}