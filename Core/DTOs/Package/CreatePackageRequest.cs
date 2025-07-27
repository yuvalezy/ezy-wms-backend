using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Package;

public class CreatePackageRequest : IValidatableObject {
    public          int?                       BinEntry            { get; set; }
    public          ObjectType?                SourceOperationType { get; set; }
    public          Guid?                      SourceOperationId   { get; set; }
    public          Dictionary<string, object> CustomAttributes    { get; set; } = new();
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (SourceOperationType is ObjectType.Package or ObjectType.Picking || SourceOperationId != null)
            yield break;

        yield return new ValidationResult("SourceOperationId is required");
    }
}