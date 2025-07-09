using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Package;

public class PackageByBarcodeRequest : IValidatableObject {
    public string      Barcode    { get; set; } = string.Empty;
    public bool?       Contents   { get; set; }
    public bool?       History    { get; set; }
    public bool?       Details    { get; set; }
    public int?        BinEntry   { get; set; }
    public Guid?       ObjectId   { get; set; }
    public ObjectType? ObjectType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (ObjectType.HasValue && ObjectType != Enums.ObjectType.Package && ObjectId == null)
            yield return new ValidationResult(
                "ObjectId is required when ObjectType is not Package",
                [nameof(ObjectId)]
            );
    }
}