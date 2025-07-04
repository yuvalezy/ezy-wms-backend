using Core.Enums;

namespace Core.DTOs.General;

public abstract class ResponseBase {
    public string?        ErrorMessage { get; set; }
    public ResponseStatus Status       { get; set; }
}