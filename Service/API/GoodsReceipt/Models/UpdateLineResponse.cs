namespace Service.API.GoodsReceipt.Models;

public enum UpdateLineReturnValue {
    Status             = -1,
    LineStatus         = -2,
    CloseReason        = -3,
    Ok                 = 0,
    SupervisorPassword = 1,
    NotSupervisor      = 2
}