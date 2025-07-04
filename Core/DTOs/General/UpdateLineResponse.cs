using Core.Enums;

namespace Core.DTOs.General;

public class UpdateLineResponse {
    public UpdateLineReturnValue ReturnValue { get; set; } = UpdateLineReturnValue.Ok;
    public string? ErrorMessage { get; set; }
}