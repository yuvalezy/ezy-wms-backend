namespace Core.DTOs.PickList;

public class PickingValidationResult {
    public bool IsValid { get; set; }
    public int? PickEntry { get; set; }
    public int ReturnValue { get; set; }
    public decimal OpenQuantity { get; set; }
    public decimal BinOnHand { get; set; }
    public decimal OnHand { get; set; }
    public string? ErrorMessage { get; set; }
}