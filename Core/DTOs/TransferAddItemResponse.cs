namespace Core.DTOs;

public class TransferAddItemResponse {
    public string?   LineID         { get; set; }
    public bool   ClosedTransfer { get; set; }
    public string ErrorMessage   { get; set; }
}