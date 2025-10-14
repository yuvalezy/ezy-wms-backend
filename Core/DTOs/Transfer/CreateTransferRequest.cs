namespace Core.DTOs.Transfer;

public class CreateTransferRequest {
    public string? Name     { get; set; }
    public string? Comments { get; set; }
    public string? TargetWhsCode { get; set; }
}