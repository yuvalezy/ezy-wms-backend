using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public class UpdateBarCodeRequest : IValidatableObject {
    public required string    ItemCode       { get; set; }
    public          string[]? AddBarcodes    { get; set; }
    public          string[]? RemoveBarcodes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if ((AddBarcodes == null || AddBarcodes.Length == 0) && (RemoveBarcodes == null || RemoveBarcodes.Length == 0))
            yield return new ValidationResult("At least one barcode must be added or removed.", [nameof(AddBarcodes), nameof(RemoveBarcodes)]);
    }
}