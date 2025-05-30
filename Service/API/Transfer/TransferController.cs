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

    [HttpGet]
    [Route("ProcessInfo/{id:int}")]
    public Models.Transfer ProcessInfo(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer))
            throw new UnauthorizedAccessException("You don't have access for transfer completion check");
        return Data.Transfer.ProcessInfo(id);
    }

    [HttpPost]
    [ActionName("AddItem")]
    public AddItemResponse AddItem([FromBody] AddItemParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer))
            throw new UnauthorizedAccessException("You don't have access for adding item to transfer");
        using var conn = Global.Connector;
        conn.BeginTransaction();
        try {
            if (!parameters.Validate(conn, Data, EmployeeID))
                return new AddItemResponse { ClosedTransfer = true };
            var addItemResponse = Data.Transfer.AddItem(conn, parameters, EmployeeID);
            conn.CommitTransaction();
            return addItemResponse;
        }
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpPost]
    [ActionName("UpdateLine")]
    public UpdateLineReturnValue UpdateLine([FromBody] UpdateLineParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer))
            throw new UnauthorizedAccessException("You don't have access for updating line in transfer");
        using var conn = Global.Connector;
        try {
            conn.BeginTransaction();
            (UpdateLineReturnValue returnValue, int supervisorEmployeeID) = parameters.Validate(conn, Data);
            if (returnValue != UpdateLineReturnValue.Ok)
                return returnValue;
            Data.Transfer.UpdateLine(conn, parameters);
            conn.CommitTransaction();
            return returnValue;
        }
        catch  {
            conn.RollbackTransaction();
            throw;
        }
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
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer, Authorization.TransferSupervisor))
            throw new UnauthorizedAccessException("You don't have access for transfer cancellation");
        return Data.Transfer.ProcessTransfer(parameters.ID, EmployeeID, Data.General.AlertUsers);
    }

    [HttpGet]
    [ActionName("CancelReasons")]
    public IEnumerable<ValueDescription<int>> GetCancelReasons() {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer))
            throw new UnauthorizedAccessException("You don't have access to get cancel reasons");
        return Data.General.GetCancelReasons(ReasonType.Transfer);
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
        return Data.Transfer.GetTransferContent(parameters);
    }

    [HttpPost]
    [ActionName("TransferContentTargetDetail")]
    public IEnumerable<TransferContentTargetItemDetail> TransferContentTargetDetail([FromBody] TransferContentTargetItemDetailParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer, Authorization.TransferSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get transfer content");
        return Data.Transfer.TransferContentTargetDetail(parameters);
    }

    [HttpPost]
    [ActionName("UpdateContentTargetDetail")]
    public void UpdateContentTargetDetail([FromBody] UpdateDetailParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.TransferSupervisor))
            throw new UnauthorizedAccessException("You don't have access for document cancellation");
        Data.Transfer.UpdateContentTargetDetail(parameters);
    }

    [HttpPost]
    [ActionName("CreateTransferRequest")]
    public int CreateTransferRequest([FromBody] TransferContent[] contents) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.TransferRequest))
            throw new UnauthorizedAccessException("You don't have access for transfer request creation");

        var employeeData = Data.General.GetEmployeeData(EmployeeID);
        return Data.Transfer.CreateTransferRequest(contents, employeeData);
    }
}