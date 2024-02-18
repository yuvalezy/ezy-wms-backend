using System;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.General.Models;
using Service.API.Models;
using Service.API.Transfer.Models;
using Service.Shared;

namespace Service.API.Transfer;

[Authorize, RoutePrefix("api/Transfer")]
public class TransferController : LWApiController {
    
    [HttpPost]
    [ActionName("Create")]
    public Models.Transfer CreateTransfer([FromBody] CreateParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.TransferSupervisor))
            throw new UnauthorizedAccessException("You don't have access for transfer creation");

        int id = Data.Transfer.CreateTransfer(parameters, EmployeeID);
        return Data.Transfer.GetTransfer(id);
    }

    [HttpPost]
    [ActionName("AddItem")]
    public AddItemResponse AddItem([FromBody] AddItemParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer))
            throw new UnauthorizedAccessException("You don't have access for adding item to transfer");
        if (!parameters.Validate(Data, EmployeeID))
            return new AddItemResponse { ClosedTransfer = true };
        return Data.Transfer.AddItem(parameters, EmployeeID);
    }

    [HttpPost]
    [ActionName("UpdateLine")]
    public UpdateLineReturnValue UpdateLine([FromBody] UpdateLineParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer))
            throw new UnauthorizedAccessException("You don't have access for updating line in transfer");
        (UpdateLineReturnValue returnValue, int supervisorEmployeeID) = parameters.Validate(Data);
        if (returnValue != UpdateLineReturnValue.Ok)
            return returnValue;
        Data.Transfer.UpdateLine(parameters);
        return returnValue;
    }

    [HttpPost]
    [ActionName("Cancel")]
    public bool CancelTransfer([FromBody] IDParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.TransferSupervisor))
            throw new UnauthorizedAccessException("You don't have access for transfer cancellation");

        return Data.Transfer.CancelTransfer(parameters.ID, EmployeeID);
    }

    [HttpPost]
    [ActionName("Process")]
    public bool ProcessTransfer([FromBody] IDParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.TransferSupervisor))
            throw new UnauthorizedAccessException("You don't have access for transfer cancellation");
        return Data.Transfer.ProcessTransfer(parameters.ID, EmployeeID, Data.General.AlertUsers);
    }
    [HttpGet]
    [ActionName("Transfers")]
    public IEnumerable<Models.Transfer> GetTransfers([FromUri] FilterParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer, Authorization.TransferSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get transfer");
        parameters.WhsCode = Data.General.GetEmployeeData(EmployeeID).WhsCode;
        return Data.Transfer.GetTransfers(parameters);
    }

    [HttpGet]
    [Route("Transfer/{id:int}")]
    public Models.Transfer GetTransfer(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer, Authorization.TransferSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get transfer");
        return Data.Transfer.GetTransfer(id);
    }

    [HttpPost]
    [ActionName("TransferContent")]
    public IEnumerable<TransferContent> TransferContent([FromBody] TransferContentParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer, Authorization.TransferSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get transfer content");
        return Data.Transfer.GetTransferContent(parameters.ID, parameters.BinEntry);
    }
}