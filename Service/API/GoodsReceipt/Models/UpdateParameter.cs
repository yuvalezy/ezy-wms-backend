using System;
using Service.Shared;

namespace Service.API.GoodsReceipt.Models;

public class UpdateLineParameter {
    public int    ID             { get; set; }
    public int    LineID         { get; set; }
    public string Comment        { get; set; }
    public int?   QuantityInUnit { get; set; }
    public int?   CloseReason    { get; set; }
    public string UserName       { get; set; }

    public (UpdateLineReturnValue, int) Validate(Data data) {
        if (ID <= 0)
            throw new ArgumentException(ErrorMessages.ID_is_a_required_parameter);
        if (LineID < 0)
            throw new ArgumentException(ErrorMessages.LineID_is_a_required_parameter);

        if (QuantityInUnit is < 1)
            throw new Exception("Quantity in Unit cannot be less then 1!");

        int empID = -1;
        
        if (CloseReason.HasValue || QuantityInUnit.HasValue) {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new Exception("A supervisor password is required to update line!");
            if (!Data.ValidateAccess(UserName, out empID, out _))
                return (UpdateLineReturnValue.SupervisorPassword, -1);
            if (!Global.ValidateAuthorization(empID, Authorization.GoodsReceiptSupervisor))
                return (UpdateLineReturnValue.NotSupervisor, -1);
        }

        return ((UpdateLineReturnValue)data.GoodsReceipt.ValidateUpdateLine(this), empID);
    }
}