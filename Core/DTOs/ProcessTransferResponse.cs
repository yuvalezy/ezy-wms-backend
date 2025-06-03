using Core.Enums;

namespace Core.DTOs;

public class ProcessTransferResponse {
    public bool Success { get; set; }
    public int? SapDocEntry { get; set; }
    public int? SapDocNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public ResponseStatus Status { get; set; } = ResponseStatus.Ok;
}