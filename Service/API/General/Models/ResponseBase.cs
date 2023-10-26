namespace Service.API.General.Models; 

public abstract class ResponseBase {
    public string         ErrorMessage { get; set; }
    public ResponseStatus Status       { get; set; }
}

public enum ResponseStatus {
    Error,
    Ok
}