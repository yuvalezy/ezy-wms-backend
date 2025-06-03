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

    [HttpPost("updateLineQuantity")]
    [RequireRolePermission(RoleType.Transfer)]
    public async Task<UpdateLineResponse> UpdateLineQuantity([FromBody] UpdateLineQuantityRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await transferLineService.UpdateLineQuantity(sessionInfo, request);
    }

    [HttpPost("cancel")]
    [RequireRolePermission(RoleType.TransferSupervisor)]
    public async Task<IActionResult> CancelTransfer([FromBody] CancelTransferRequest request) {
        var  sessionInfo = HttpContext.GetSession();
        bool result      = await transferService.CancelTransfer(request.ID, sessionInfo);
        return Ok(new { success = result });
    }

    [HttpPost("process")]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    public async Task<ActionResult<ProcessTransferResponse>> ProcessTransfer([FromBody] ProcessTransferRequest request) {
        var sessionInfo = HttpContext.GetSession();
        var result      = await transferService.ProcessTransfer(request.ID, sessionInfo);
        return Ok(result);
    }


    [HttpGet]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    public async Task<IEnumerable<TransferResponse>> GetTransfers([FromQuery] TransfersRequest request) => await transferService.GetTransfers(request, HttpContext.GetSession().Warehouse);

    [HttpGet]
    [Route("{id:guid}")]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    public async Task<TransferResponse> GetTransfer(Guid id) => await transferService.GetTransfer(id);
    [HttpPost("transferContent")]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    public async Task<IEnumerable<TransferContentResponse>> TransferContent([FromBody] TransferContentRequest request) {
        return await transferService.GetTransferContent(request);
    }

    [HttpPost("transferContentTargetDetail")]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    public async Task<IEnumerable<TransferContentTargetDetailResponse>> TransferContentTargetDetail([FromBody] TransferContentTargetDetailRequest request) {
        return await transferService.GetTransferContentTargetDetail(request);
    }

    [HttpPost("updateContentTargetDetail")]
    [RequireRolePermission(RoleType.TransferSupervisor)]
    public async Task<IActionResult> UpdateContentTargetDetail([FromBody] UpdateContentTargetDetailRequest request) {
        var sessionInfo = HttpContext.GetSession();
        await transferService.UpdateContentTargetDetail(request, sessionInfo);
        return Ok(new { success = true });
    }

    [HttpPost("createTransferRequest")]
    [RequireRolePermission(RoleType.TransferRequest)]
    public async Task<CreateTransferRequestResponse> CreateTransferRequest([FromBody] CreateTransferRequestRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await transferService.CreateTransferRequest(request, sessionInfo);
    }
}