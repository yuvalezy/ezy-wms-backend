using Core.Enums;

namespace Core.DTOs.Package;

public class RemoveItemFromPackageRequest {
    public          Guid        PackageId           { get; set; }
    public required string      ItemCode            { get; set; }
    public          decimal     Quantity            { get; set; }
    public          decimal?    UnitQuantity        { get; set; }
    public          UnitType    UnitType            { get; set; }
    public          ObjectType? SourceOperationType { get; set; }
    public          Guid?       SourceOperationId   { get; set; }
}