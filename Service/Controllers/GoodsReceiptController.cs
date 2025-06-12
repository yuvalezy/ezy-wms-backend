using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs;
using Core.DTOs.GoodsReceipt;
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
public class GoodsReceiptController(
    IGoodsReceiptService        goodsReceiptService,
    IGoodsReceiptLineItemService goodsReceiptLineItemService,
    ISettings                   settings) : ControllerBase {
    // 1. Create Goods Receipt
    [HttpPost("create")]
    [RequireAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor)]
    public async Task<ActionResult<GoodsReceiptResponse>> CreateGoodsReceipt([FromBody] CreateGoodsReceiptRequest request) {
        var sessionInfo = HttpContext.GetSession();

        RoleType requiredRole;
        // Check if non-supervisor can create based on settings
        if (settings.Options.GoodsReceiptCreateSupervisorRequired) {
            requiredRole = request.Type != GoodsReceiptType.SpecificReceipts
                ? RoleType.GoodsReceipt
                : RoleType.GoodsReceiptConfirmation;
        }
        else {
            // Supervisor required - check correct supervisor role
            requiredRole = request.Type != GoodsReceiptType.SpecificReceipts
                ? RoleType.GoodsReceiptSupervisor
                : RoleType.GoodsReceiptConfirmationSupervisor;
        }

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole)) {
            return Forbid();
        }

        var result = await goodsReceiptService.CreateGoodsReceipt(request, sessionInfo);
        return Ok(result);
    }

    // 2. Add Item to Goods Receipt
    [HttpPost("addItem")]
    public async Task<ActionResult<GoodsReceiptAddItemResponse>> AddItem([FromBody] GoodsReceiptAddItemRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(request.Id);
        if (document == null) {
            return NotFound();
        }

        var requiredRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceipt
            : RoleType.GoodsReceiptConfirmation;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole)) {
            var supervisorRole = requiredRole == RoleType.GoodsReceipt
                ? RoleType.GoodsReceiptSupervisor
                : RoleType.GoodsReceiptConfirmationSupervisor;
            if (!sessionInfo.Roles.Contains(supervisorRole)) {
                return Forbid();
            }
        }

        return await goodsReceiptLineItemService.AddItem(sessionInfo, request);
    }

    // 3. Update Line
    [HttpPost("updateLine")]
    public async Task<ActionResult<UpdateLineResponse>> UpdateLine([FromBody] UpdateGoodsReceiptLineRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(request.Id);
        if (document == null) {
            return NotFound();
        }

        var requiredRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceipt
            : RoleType.GoodsReceiptConfirmation;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole)) {
            var supervisorRole = requiredRole == RoleType.GoodsReceipt
                ? RoleType.GoodsReceiptSupervisor
                : RoleType.GoodsReceiptConfirmationSupervisor;
            if (!sessionInfo.Roles.Contains(supervisorRole)) {
                return Forbid();
            }
        }

        return await goodsReceiptService.UpdateLine(sessionInfo, request);
    }

    // 4. Update Line Quantity
    [HttpPost("updateLineQuantity")]
    public async Task<ActionResult<UpdateLineResponse>> UpdateLineQuantity([FromBody] UpdateGoodsReceiptLineQuantityRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(request.Id);
        if (document == null) {
            return NotFound();
        }

        var requiredRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceipt
            : RoleType.GoodsReceiptConfirmation;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole)) {
            var supervisorRole = requiredRole == RoleType.GoodsReceipt
                ? RoleType.GoodsReceiptSupervisor
                : RoleType.GoodsReceiptConfirmationSupervisor;
            if (!sessionInfo.Roles.Contains(supervisorRole)) {
                return Forbid();
            }
        }

        return await goodsReceiptLineItemService.UpdateLineQuantity(sessionInfo, request);
    }

    // 5. Cancel Goods Receipt
    [HttpPost("cancel/{id:guid}")]
    public async Task<ActionResult<bool>> CancelGoodsReceipt(Guid id) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceiptSupervisor
            : RoleType.GoodsReceiptConfirmationSupervisor;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        bool result = await goodsReceiptService.CancelGoodsReceipt(id, sessionInfo);
        return Ok(result);
    }

    // 6. Process Goods Receipt
    [HttpPost("process/{id:guid}")]
    public async Task<ActionResult<ProcessGoodsReceiptResponse>> ProcessGoodsReceipt(Guid id) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceiptSupervisor
            : RoleType.GoodsReceiptConfirmationSupervisor;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return await goodsReceiptService.ProcessGoodsReceipt(id, sessionInfo);
    }

    // 7. Get Documents (list) - Uses POST for complex filtering
    [HttpPost]
    public async Task<ActionResult<IEnumerable<GoodsReceiptResponse>>> GetGoodsReceipts([FromBody] GoodsReceiptsRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Determine required roles based on Confirm parameter
        if (request.Confirm == true) {
            if (!sessionInfo.SuperUser &&
                !sessionInfo.Roles.Contains(RoleType.GoodsReceiptConfirmation) &&
                !sessionInfo.Roles.Contains(RoleType.GoodsReceiptConfirmationSupervisor)) {
                return Forbid();
            }
        }
        else {
            if (!sessionInfo.SuperUser &&
                !sessionInfo.Roles.Contains(RoleType.GoodsReceipt) &&
                !sessionInfo.Roles.Contains(RoleType.GoodsReceiptSupervisor)) {
                return Forbid();
            }
        }

        request.WhsCode = sessionInfo.Warehouse;
        return Ok(await goodsReceiptService.GetGoodsReceipts(request, sessionInfo.Warehouse));
    }

    // 8. Get Document by ID
    [HttpGet("{id:guid}")]
    [RequireAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor,
        RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor)]
    public async Task<ActionResult<GoodsReceiptResponse>> GetGoodsReceipt(Guid id) {
        var sessionInfo = HttpContext.GetSession();
        var document    = await goodsReceiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        // Verify role matches document type
        if (document.Type != GoodsReceiptType.SpecificReceipts) {
            if (!sessionInfo.SuperUser &&
                !sessionInfo.Roles.Contains(RoleType.GoodsReceipt) &&
                !sessionInfo.Roles.Contains(RoleType.GoodsReceiptSupervisor)) {
                return Forbid();
            }
        }
        else {
            if (!sessionInfo.SuperUser &&
                !sessionInfo.Roles.Contains(RoleType.GoodsReceiptConfirmation) &&
                !sessionInfo.Roles.Contains(RoleType.GoodsReceiptConfirmationSupervisor)) {
                return Forbid();
            }
        }

        return Ok(document);
    }

    // 9. Get All Report
    [HttpGet("{id:guid}/report/all")]
    public async Task<ActionResult<IEnumerable<GoodsReceiptReportAllResponse>>> GetGoodsReceiptAllReport(Guid id) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceiptSupervisor
            : RoleType.GoodsReceiptConfirmationSupervisor;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return Ok(await goodsReceiptService.GetGoodsReceiptAllReport(id, sessionInfo.Warehouse));
    }

    // 10. Get All Report Details
    [HttpGet("{id:guid}/report/all/{itemCode}")]
    public async Task<ActionResult<IEnumerable<GoodsReceiptReportAllDetailsResponse>>> GetGoodsReceiptAllReportDetails(Guid id, string itemCode) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceiptSupervisor
            : RoleType.GoodsReceiptConfirmationSupervisor;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return Ok(await goodsReceiptService.GetGoodsReceiptAllReportDetails(id, itemCode));
    }

    // 11. Update Goods Receipt All
    [HttpPost("updateAll")]
    public async Task<ActionResult<bool>> UpdateGoodsReceiptAll([FromBody] UpdateGoodsReceiptAllRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(request.Id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceiptSupervisor
            : RoleType.GoodsReceiptConfirmationSupervisor;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        var result = await goodsReceiptService.UpdateGoodsReceiptAll(request, sessionInfo);
        return Ok(result);
    }

    // 12. Get VS Exit Report
    [HttpGet("{id:guid}/report/vsExit")]
    public async Task<ActionResult<IEnumerable<GoodsReceiptVSExitReportResponse>>> GetGoodsReceiptVSExitReport(Guid id) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceiptSupervisor
            : RoleType.GoodsReceiptConfirmationSupervisor;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return Ok(await goodsReceiptService.GetGoodsReceiptVSExitReport(id));
    }

    // 13. Get Validate Process
    [HttpGet("{id:guid}/validateProcess")]
    public async Task<ActionResult<IEnumerable<GoodsReceiptValidateProcessResponse>>> GetGoodsReceiptValidateProcess(Guid id) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceiptSupervisor
            : RoleType.GoodsReceiptConfirmationSupervisor;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return Ok(await goodsReceiptService.GetGoodsReceiptValidateProcess(id));
    }

    // 14. Get Validate Process Line Details
    [HttpPost("validateProcessLineDetails")]
    public async Task<ActionResult<IEnumerable<GoodsReceiptValidateProcessLineDetailsResponse>>> GetGoodsReceiptValidateProcessLineDetails(
        [FromBody] GoodsReceiptValidateProcessLineDetailsRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await goodsReceiptService.GetGoodsReceipt(request.ID);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type != GoodsReceiptType.SpecificReceipts
            ? RoleType.GoodsReceiptSupervisor
            : RoleType.GoodsReceiptConfirmationSupervisor;

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return Ok(await goodsReceiptService.GetGoodsReceiptValidateProcessLineDetails(request));
    }
}