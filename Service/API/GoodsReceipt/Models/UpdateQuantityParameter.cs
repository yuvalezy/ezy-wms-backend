using System;
using Newtonsoft.Json;
using Service.API.General.Models;
using Service.API.Transfer.Models;
using Service.Shared;
using Service.Shared.Data;

namespace Service.API.GoodsReceipt.Models;

public class UpdateLineQuantityParameter {
    public              int    ID            { get; set; }
    public              int    LineID        { get; set; }
    public              int    Quantity      { get; set; }
    public              string UserName      { get; set; }
    [JsonIgnore] public bool   InternalClose { get; set; }

    public (UpdateItemResponse, int) Validate(DataConnector conn, Data data) {
        if (ID <= 0)
            throw new ArgumentException(ErrorMessages.ID_is_a_required_parameter);
        if (LineID < 0)
            throw new ArgumentException(ErrorMessages.LineID_is_a_required_parameter);

        int empID = -1;

        if (Global.GRPOModificationsRequiredSupervisor) {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new Exception("A supervisor password is required to update line!");
            if (!Data.ValidateAccess(UserName, out empID, out _))
                return new ValueTuple<UpdateItemResponse, int>(new UpdateItemResponse(UpdateLineReturnValue.SupervisorPassword), -1);
            if (!Global.ValidateAuthorization(empID, Authorization.GoodsReceiptSupervisor))
                return new ValueTuple<UpdateItemResponse, int>(new UpdateItemResponse(UpdateLineReturnValue.NotSupervisor), -1);
        }

        return new ValueTuple<UpdateItemResponse, int>(new UpdateItemResponse((UpdateLineReturnValue)data.GoodsReceipt.ValidateUpdateLine(conn, ID, LineID)), empID);
    }
}