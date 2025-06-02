namespace Service.API.General.Models;

public enum UpdateLineReturnValue {
    Status                    = -1,
    LineStatus                = -2,
    CloseReason               = -3,
    QuantityMoreThenAvailable = -13,
    Ok                        = 0,
    SupervisorPassword        = 1,
    NotSupervisor             = 2
}