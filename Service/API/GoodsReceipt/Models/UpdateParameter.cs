using System;
using Newtonsoft.Json;
using Service.API.General.Models;
using Service.Shared;
using Service.Shared.Data;

namespace Service.API.GoodsReceipt.Models;

public class UpdateLineParameter {
    public              int    ID            { get; set; }
    public              int    LineID        { get; set; }
    public              string Comment       { get; set; }
    public              int?   CloseReason   { get; set; }
    public              string UserName      { get; set; }
    [JsonIgnore] public bool   InternalClose { get; set; }

    public (UpdateLineReturnValue, int) Validate(DataConnector conn, Data data) {
        if (ID <= 0)
            throw new ArgumentException(ErrorMessages.ID_is_a_required_parameter);
        if (LineID < 0)
            throw new ArgumentException(ErrorMessages.LineID_is_a_required_parameter);

        int empID = -1;

        if (CloseReason.HasValue && Global.GRPOModificationsRequiredSupervisor) {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new Exception("A supervisor password is required to update line!");
            if (!Data.ValidateAccess(UserName, out empID, out _))
                return (UpdateLineReturnValue.SupervisorPassword, -1);
            if (!Global.ValidateAuthorization(empID, Authorization.GoodsReceiptSupervisor, Authorization.GoodsReceiptConfirmationSupervisor))
                return (UpdateLineReturnValue.NotSupervisor, -1);
        }

        return ((UpdateLineReturnValue)data.GoodsReceipt.ValidateUpdateLine(conn, ID, LineID, CloseReason), empID);
    }
}