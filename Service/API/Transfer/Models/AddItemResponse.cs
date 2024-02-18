namespace Service.API.Transfer.Models;

public class AddItemResponse {
    public int?   LineID         { get; set; }
    public bool   ClosedTransfer { get; set; }
    public string ErrorMessage   { get; set; }
}