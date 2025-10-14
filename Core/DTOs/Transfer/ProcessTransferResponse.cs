using Core.Enums;

namespace Core.DTOs.Transfer;

public class ProcessTransferResponse {
    public bool Success { get; set; }
    public int? ExternalEntry { get; set; }
    public int? ExternalNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public ResponseStatus Status { get; set; } = ResponseStatus.Ok;
}