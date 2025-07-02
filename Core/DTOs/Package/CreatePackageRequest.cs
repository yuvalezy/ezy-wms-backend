using Core.Enums;

namespace Core.DTOs.Package;

public class CreatePackageRequest {
    public          int?                       BinEntry            { get; set; }
    public          ObjectType?                SourceOperationType { get; set; }
    public          Guid?                      SourceOperationId   { get; set; }
    public          Dictionary<string, object> CustomAttributes    { get; set; } = new();
}