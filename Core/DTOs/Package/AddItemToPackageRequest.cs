using Core.Enums;

namespace Core.DTOs.Package;

public class AddItemToPackageRequest {
    public          Guid        PackageId             { get; set; }
    public required string      ItemCode              { get; set; }
    public          decimal     Quantity              { get; set; }
    public          UnitType    UnitType              { get; set; }
    public          string?     BatchNo               { get; set; }
    public          string?     SerialNo              { get; set; }
    public          DateTime?   ExpiryDate            { get; set; }
    public          int?        BinEntry              { get; set; }
    public          string?     BinCode               { get; set; }
    public          ObjectType? SourceOperationType   { get; set; }
    public          Guid?       SourceOperationId     { get; set; }
    public          Guid?       SourceOperationLineId { get; set; }
}