namespace Core.Models;

public class PickingValidationResult {
    public bool IsValid      { get; set; }
    public int? PickEntry    { get; set; }
    public int  ReturnValue  { get; set; }
    public int  OpenQuantity { get; set; }
    public int  BinOnHand   { get; set; }
    public string? ErrorMessage { get; set; }
}