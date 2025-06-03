namespace Core.DTOs;

public class TransferAddItemResponse {
    public Guid?   LineID         { get; set; }
    public bool    ClosedTransfer { get; set; }
    public string? ErrorMessage   { get; set; }
}