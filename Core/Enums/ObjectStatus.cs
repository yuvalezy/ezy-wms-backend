namespace Core.Enums;

public enum ObjectStatus {
    Open       = 0,
    InProgress = 1,
    Finished   = 2,
    Cancelled  = 3,
    Processing = 4,
    Closed     = 5
}

public enum SyncStatus {
    Pending    = 0,
    Processing = 1,
    Synced     = 2,
    Failed     = 3,
    Retry      = 4
}