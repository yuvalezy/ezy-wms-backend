using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs;

public class TransferAddItemRequest : AddItemRequestBase, IValidatableObject {
    public int          Quantity { get; set; }
    public SourceTarget Type     { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (ID == Guid.Empty)
            yield return new ValidationResult("ID is a required parameter", [nameof(ID)]);
        if (Quantity <= 0)
            yield return new ValidationResult("Quantity is a required parameter", [nameof(Quantity)]);
        if (!Unit.HasValue)
            yield return new ValidationResult("Unit is a required parameter", [nameof(Unit)]);
        if (string.IsNullOrWhiteSpace(ItemCode))
            yield return new ValidationResult("Item Code is a required parameter", [nameof(ItemCode)]);
        if (Type == SourceTarget.Source && string.IsNullOrWhiteSpace(BarCode))
            yield return new ValidationResult("Barcode is a required paramter", [nameof(BarCode)]);
    }
}