namespace Service.API.GoodsReceipt.Models;

public enum UpdateLineReturnValue {
    Status             = -1,
    LineStatus         = -2,
    Ok                 = 0,
    SupervisorPassword = 1,
    NotSupervisor      = 2
}