using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Transfer;

public class TransferAddItemRequest : IValidatableObject {
    public Guid         ID       { get; set; }
    public string       ItemCode { get; set; }
    public string?       BarCode  { get; set; }
    public int?         BinEntry { get; set; }
    public UnitType?    Unit     { get; set; }
    public int          Quantity { get; set; }
    public SourceTarget Type     { get; set; }
    
    // Package-related properties
    public Guid? PackageId { get; set; }
    public bool IsPackageTransfer { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (ID == Guid.Empty)
            yield return new ValidationResult("ID is a required parameter", [nameof(ID)]);
        if (Quantity <= 0)
            yield return new ValidationResult("Quantity is a required parameter", [nameof(Quantity)]);
        if (!Unit.HasValue)
            yield return new ValidationResult("Unit is a required parameter", [nameof(Unit)]);
        if (string.IsNullOrWhiteSpace(ItemCode))
            yield return new ValidationResult("Item Code is a required parameter", [nameof(ItemCode)]);

    }
}