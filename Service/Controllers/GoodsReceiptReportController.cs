using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs.GoodsReceipt;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Middlewares;

namespace Service.Controllers;

/// <summary>
/// Goods Receipt Report Controller - Supervisor-level reporting and validation endpoints for goods receipt documents
/// </summary>
[ApiController]
[Route("api/goodsreceipt")]
[Authorize]
public class GoodsReceiptReportController(
    IGoodsReceiptService receiptService,
    IGoodsReceiptReportService receiptReportService)
: ControllerBase {

    /// <summary>
    /// Gets a comprehensive report of all items in a goods receipt document (supervisor only)
    /// </summary>
    /// <param name="id">The unique identifier of the goods receipt</param>
    /// <returns>A detailed report of all items in the goods receipt</returns>
    /// <response code="200">Returns the goods receipt all report</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id:guid}/report/all")]
    [ProducesResponseType(typeof(IEnumerable<GoodsReceiptReportAllResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<GoodsReceiptReportAllResponse>>> GetGoodsReceiptAllReport(Guid id) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type switch {
            GoodsReceiptType.SpecificTransfers => RoleType.TransferConfirmationSupervisor,
            GoodsReceiptType.SpecificReceipts => RoleType.GoodsReceiptConfirmationSupervisor,
            _ => RoleType.GoodsReceiptSupervisor
        };

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return Ok(await receiptReportService.GetGoodsReceiptAllReport(id, sessionInfo.Warehouse));
    }

    /// <summary>
    /// Gets detailed report information for a specific item in a goods receipt document (supervisor only)
    /// </summary>
    /// <param name="id">The unique identifier of the goods receipt</param>
    /// <param name="itemCode">The item code to get detailed information for</param>
    /// <returns>Detailed report information for the specified item</returns>
    /// <response code="200">Returns the goods receipt all report details</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id:guid}/report/all/{itemCode}")]
    [ProducesResponseType(typeof(IEnumerable<GoodsReceiptReportAllDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<GoodsReceiptReportAllDetailsResponse>>> GetGoodsReceiptAllReportDetails(Guid id, string itemCode) {
        var sessionInfo = HttpContext.GetSession();
        itemCode = Uri.UnescapeDataString(itemCode);

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type switch {
            GoodsReceiptType.SpecificTransfers => RoleType.TransferConfirmationSupervisor,
            GoodsReceiptType.SpecificReceipts => RoleType.GoodsReceiptConfirmationSupervisor,
            _ => RoleType.GoodsReceiptSupervisor
        };

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return Ok(await receiptReportService.GetGoodsReceiptAllReportDetails(id, itemCode));
    }

    /// <summary>
    /// Updates all items in a goods receipt document in bulk (supervisor only)
    /// </summary>
    /// <param name="request">The request containing bulk update information</param>
    /// <returns>True if update was successful, false otherwise</returns>
    /// <response code="200">Returns true if update was successful</response>
    /// <response code="400">If the request is invalid or update failed</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("updateAll")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> UpdateGoodsReceiptAll([FromBody] UpdateGoodsReceiptAllRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(request.Id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type switch {
            GoodsReceiptType.SpecificTransfers => RoleType.TransferConfirmationSupervisor,
            GoodsReceiptType.SpecificReceipts => RoleType.GoodsReceiptConfirmationSupervisor,
            _ => RoleType.GoodsReceiptSupervisor
        };

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        string? errorMessage = await receiptReportService.UpdateGoodsReceiptAll(request, sessionInfo);
        return string.IsNullOrWhiteSpace(errorMessage) ? Ok(true) : BadRequest(errorMessage);
    }

    /// <summary>
    /// Gets a comparison report between goods receipt and exit documents (supervisor only)
    /// </summary>
    /// <param name="id">The unique identifier of the goods receipt</param>
    /// <returns>A comparison report showing variances between receipt and exit</returns>
    /// <response code="200">Returns the goods receipt vs exit report</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id:guid}/report/vsExit")]
    [ProducesResponseType(typeof(IEnumerable<GoodsReceiptVSExitReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<GoodsReceiptVSExitReportResponse>>> GetGoodsReceiptVSExitReport(Guid id) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type switch {
            GoodsReceiptType.SpecificTransfers => RoleType.TransferConfirmationSupervisor,
            GoodsReceiptType.SpecificReceipts => RoleType.GoodsReceiptConfirmationSupervisor,
            _ => RoleType.GoodsReceiptSupervisor
        };

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return Ok(await receiptReportService.GetGoodsReceiptVSExitReport(id));
    }

    /// <summary>
    /// Validates whether a goods receipt document is ready for processing (supervisor only)
    /// </summary>
    /// <param name="id">The unique identifier of the goods receipt</param>
    /// <returns>Validation results indicating if the document can be processed</returns>
    /// <response code="200">Returns the goods receipt validation results</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id:guid}/validateProcess")]
    [ProducesResponseType(typeof(IEnumerable<GoodsReceiptValidateProcessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<GoodsReceiptValidateProcessResponse>>> GetGoodsReceiptValidateProcess(Guid id) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type switch {
            GoodsReceiptType.SpecificTransfers => RoleType.TransferConfirmationSupervisor,
            GoodsReceiptType.SpecificReceipts => RoleType.GoodsReceiptConfirmationSupervisor,
            _ => RoleType.GoodsReceiptSupervisor
        };

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return Ok(await receiptReportService.GetGoodsReceiptValidateProcess(id));
    }

    /// <summary>
    /// Gets detailed validation information for specific lines in a goods receipt document (supervisor only)
    /// </summary>
    /// <param name="request">The request containing line validation criteria</param>
    /// <returns>Detailed validation information for the specified lines</returns>
    /// <response code="200">Returns the goods receipt validation line details</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("validateProcessLineDetails")]
    [ProducesResponseType(typeof(IEnumerable<GoodsReceiptValidateProcessLineDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<GoodsReceiptValidateProcessLineDetailsResponse>>> GetGoodsReceiptValidateProcessLineDetails(
        [FromBody] GoodsReceiptValidateProcessLineDetailsRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(request.ID);
        if (document == null) {
            return NotFound();
        }

        var requiredSupervisorRole = document.Type switch {
            GoodsReceiptType.SpecificTransfers => RoleType.TransferConfirmationSupervisor,
            GoodsReceiptType.SpecificReceipts => RoleType.GoodsReceiptConfirmationSupervisor,
            _ => RoleType.GoodsReceiptSupervisor
        };

        if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredSupervisorRole)) {
            return Forbid();
        }

        return Ok(await receiptReportService.GetGoodsReceiptValidateProcessLineDetails(request));
    }
}
