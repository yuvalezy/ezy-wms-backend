using Core.Enums;

namespace Core.DTOs.Package;

public class MovePackageRequest {
    public          Guid        PackageId           { get; set; }
    public required string      ToWhsCode           { get; set; }
    public          int?        ToBinEntry          { get; set; }
    public          Guid        UserId              { get; set; }
    public          ObjectType? SourceOperationType { get; set; }
    public          Guid?       SourceOperationId   { get; set; }
    public          string?     Notes               { get; set; }
}