using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class ItemBarCodeRequest : IValidatableObject {
    public string? ItemCode { get; set; }
    public string? Barcode  { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (string.IsNullOrWhiteSpace(ItemCode) && string.IsNullOrWhiteSpace(Barcode)) {
            yield return new ValidationResult("Either Item Code or Bar Code must have a value");
        }
    }
}