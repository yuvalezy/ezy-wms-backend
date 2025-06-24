using Core.Enums;

namespace Core.DTOs;

public class UpdateLineResponse {
    public UpdateLineReturnValue ReturnValue { get; set; } = UpdateLineReturnValue.Ok;
    public string? ErrorMessage { get; set; }
}