using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Middlewares;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransferController(ITransferService transferService, ITransferLineService transferLineService) : ControllerBase {
    [HttpPost("create")]
    [RequireRolePermission(RoleType.TransferSupervisor)]
    public async Task<TransferResponse> CreateTransfer([FromBody] CreateTransferRequest transferRequest) => await transferService.CreateTransfer(transferRequest, HttpContext.GetSession());

    [HttpGet("processInfo/{id:guid}")]
    [RequireRolePermission(RoleType.Transfer)]
    public async Task<TransferResponse> ProcessInfo(Guid id) => await transferService.GetProcessInfo(id);

    
    [HttpPost("addItem")]
    [RequireRolePermission(RoleType.Transfer)]
    public async Task<TransferAddItemResponse> AddItem([FromBody] TransferAddItemRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await transferLineService.AddItem(sessionInfo, request);
    }

    [HttpPost("updateLine")]
    [RequireRolePermission(RoleType.Transfer)]
    public async Task<UpdateLineResponse> UpdateLine([FromBody] UpdateLineRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await transferLineService.UpdateLine(sessionInfo, request);
    }

    //
    // [HttpPost]
    // [ActionName("UpdateLineQuantity")]
    // public UpdateLineReturnValue UpdateLineQuantity([FromBody] UpdateLineParameter parameters) {
    //     if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer))
    //         throw new UnauthorizedAccessException("You don't have access for updating line in transfer");
    //     using var conn = Global.Connector;
    //     try {
    //         conn.BeginTransaction();
    //         (UpdateLineReturnValue returnValue, int supervisorEmployeeID) = parameters.Validate(conn, Data);
    //         if (returnValue != UpdateLineReturnValue.Ok)
    //             return returnValue;
    //         Data.Transfer.UpdateLineQuantity(conn, parameters);
    //         conn.CommitTransaction();
    //         return returnValue;
    //     }
    //     catch {
    //         conn.RollbackTransaction();
    //         throw;
    //     }
    // }
    //
    // [HttpPost]
    // [ActionName("Cancel")]
    // public bool CancelTransfer([FromBody] IDParameters parameters) {
    //     if (!Global.ValidateAuthorization(EmployeeID, Authorization.TransferSupervisor))
    //         throw new UnauthorizedAccessException("You don't have access for transfer cancellation");
    //
    //     return Data.Transfer.CancelTransfer(parameters.ID, EmployeeID);
    // }
    //
    // [HttpPost]
    // [ActionName("Process")]
    // public bool ProcessTransfer([FromBody] IDParameters parameters) {
    //     if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer, Authorization.TransferSupervisor))
    //         throw new UnauthorizedAccessException("You don't have access for transfer cancellation");
    //     return Data.Transfer.ProcessTransfer(parameters.ID, EmployeeID, Data.General.AlertUsers);
    // }
    //
    // [HttpGet]
    // [ActionName("CancelReasons")]
    // public IEnumerable<ValueDescription<int>> GetCancelReasons() {
    //     if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer))
    //         throw new UnauthorizedAccessException("You don't have access to get cancel reasons");
    //     return Data.General.GetCancelReasons(ReasonType.Transfer);
    // }
    //
    [HttpGet]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    public async Task<IEnumerable<TransferResponse>> GetTransfers([FromQuery] TransfersRequest request) => await transferService.GetTransfers(request, HttpContext.GetSession().Warehouse);

    [HttpGet]
    [Route("{id:guid}")]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    public async Task<TransferResponse> GetTransfer(Guid id) => await transferService.GetTransfer(id);
    //
    // [HttpPost]
    // [ActionName("TransferContent")]
    // public IEnumerable<TransferContent> TransferContent([FromBody] TransferContentParameters parameters) {
    //     if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer, Authorization.TransferSupervisor))
    //         throw new UnauthorizedAccessException("You don't have access to get transfer content");
    //     return Data.Transfer.GetTransferContent(parameters);
    // }
    //
    // [HttpPost]
    // [ActionName("TransferContentTargetDetail")]
    // public IEnumerable<TransferContentTargetItemDetail> TransferContentTargetDetail([FromBody] TransferContentTargetItemDetailParameters parameters) {
    //     if (!Global.ValidateAuthorization(EmployeeID, Authorization.Transfer, Authorization.TransferSupervisor))
    //         throw new UnauthorizedAccessException("You don't have access to get transfer content");
    //     return Data.Transfer.TransferContentTargetDetail(parameters);
    // }
    //
    // [HttpPost]
    // [ActionName("UpdateContentTargetDetail")]
    // public void UpdateContentTargetDetail([FromBody] UpdateDetailParameters parameters) {
    //     if (!Global.ValidateAuthorization(EmployeeID, Authorization.TransferSupervisor))
    //         throw new UnauthorizedAccessException("You don't have access for document cancellation");
    //     Data.Transfer.UpdateContentTargetDetail(parameters);
    // }
    //
    // [HttpPost]
    // [ActionName("CreateTransferRequest")]
    // public int CreateTransferRequest([FromBody] TransferContent[] contents) {
    //     if (!Global.ValidateAuthorization(EmployeeID, Authorization.TransferRequest))
    //         throw new UnauthorizedAccessException("You don't have access for transfer request creation");
    //
    //     var employeeData = Data.General.GetEmployeeData(EmployeeID);
    //     return Data.Transfer.CreateTransferRequest(contents, employeeData);
    // }
}