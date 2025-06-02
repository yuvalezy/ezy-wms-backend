namespace Core.Models;

public abstract class ResponseBase {
    public string?        ErrorMessage { get; set; }
    public ResponseStatus Status       { get; set; }
}