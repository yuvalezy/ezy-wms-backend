using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.GoodsReceipt;

public class CreateGoodsReceiptRequest : IValidatableObject {
    [MaxLength(100)]
    public string? Name { get; set; }

    [Required]
    public GoodsReceiptType Type { get; set; } = GoodsReceiptType.All;

    [StringLength(15)]
    public string? Vendor { get; set; }


    public List<DocumentParameter> Documents { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (Type != GoodsReceiptType.All && Documents.Count == 0) {
            yield return new ValidationResult("At least one document is required");
        }
    }
}

public class DocumentParameter {
    public int ObjectType     { get; set; }
    public int DocumentNumber { get; set; }
    public int DocumentEntry  { get; set; }
}