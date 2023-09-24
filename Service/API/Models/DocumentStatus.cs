namespace Service.API.Models;

public enum DocumentStatus {
    Open       = 'O',
    Processing = 'P',
    Finished   = 'F',
    Cancelled  = 'C',
    InProgress = 'I'
}
