using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs.General;
using Core.DTOs.GoodsReceipt;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Controllers.Authorization;
using Service.Middlewares;

namespace Service.Controllers;

/// <summary>
/// Goods Receipt Controller - Manages goods receipt operations including creation, processing, and line item management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GoodsReceiptController(
    IGoodsReceiptService receiptService,
    IGoodsReceiptLineService receiptLineService,
    ISettings settings)
: ControllerBase {

    /// <summary>
    /// Creates a new goods receipt document
    /// </summary>
    /// <param name="request">The goods receipt creation request containing document details</param>
    /// <returns>The created goods receipt with its generated ID and details</returns>
    /// <response code="200">Returns the created goods receipt</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("create")]
    [RequireAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor, RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor, RoleType.TransferConfirmation,
        RoleType.TransferConfirmationSupervisor)]
    [ProducesResponseType(typeof(GoodsReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GoodsReceiptResponse>> CreateGoodsReceipt([FromBody] CreateGoodsReceiptRequest request) {
        var sessionInfo = HttpContext.GetSession();

        if (!GoodsReceiptAuthorizationHelper.HasRequiredRole(sessionInfo, request.Type, supervisorOnly: false, settings)) {
            return Forbid();
        }

        var result = await receiptService.CreateGoodsReceipt(request, sessionInfo);
        return Ok(result);
    }

    /// <summary>
    /// Adds an item to an existing goods receipt document
    /// </summary>
    /// <param name="request">The request containing item details and receipt ID</param>
    /// <returns>Response indicating success or failure of the operation</returns>
    /// <response code="200">Returns the add item response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("addItem")]
    [ProducesResponseType(typeof(GoodsReceiptAddItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoodsReceiptAddItemResponse>> AddItem([FromBody] GoodsReceiptAddItemRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(request.Id);
        if (document == null) {
            return NotFound();
        }

        if (!GoodsReceiptAuthorizationHelper.HasRequiredRole(sessionInfo, document.Type, supervisorOnly: false, settings)) {
            return Forbid();
        }

        try {
            return await receiptLineService.AddItem(sessionInfo, request);
        }
        catch (Exception ex) {
            return new GoodsReceiptAddItemResponse(ex.Message);
        }
    }

    /// <summary>
    /// Updates a specific line in a goods receipt document
    /// </summary>
    /// <param name="request">The request containing line details to update</param>
    /// <returns>Response indicating success or failure of the update</returns>
    /// <response code="200">Returns the update line response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("updateLine")]
    [ProducesResponseType(typeof(UpdateLineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateLineResponse>> UpdateLine([FromBody] UpdateGoodsReceiptLineRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(request.Id);
        if (document == null) {
            return NotFound();
        }

        if (!GoodsReceiptAuthorizationHelper.HasRequiredRole(sessionInfo, document.Type, supervisorOnly: false, settings)) {
            return Forbid();
        }

        return await receiptLineService.UpdateLine(sessionInfo, request);
    }

    /// <summary>
    /// Updates the quantity of a specific line in a goods receipt document
    /// </summary>
    /// <param name="request">The request containing line ID and new quantity</param>
    /// <returns>Response indicating success or failure of the quantity update</returns>
    /// <response code="200">Returns the update line response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("updateLineQuantity")]
    [ProducesResponseType(typeof(UpdateLineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateLineResponse>> UpdateLineQuantity([FromBody] UpdateGoodsReceiptLineQuantityRequest request) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(request.Id);
        if (document == null) {
            return NotFound();
        }

        if (!GoodsReceiptAuthorizationHelper.HasRequiredRole(sessionInfo, document.Type, supervisorOnly: false, settings)) {
            return Forbid();
        }

        return await receiptLineService.UpdateLineQuantity(sessionInfo, request);
    }

    /// <summary>
    /// Cancels a goods receipt document (supervisor only)
    /// </summary>
    /// <param name="id">The unique identifier of the goods receipt to cancel</param>
    /// <returns>True if cancellation was successful, false otherwise</returns>
    /// <response code="200">Returns true if cancellation was successful</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("cancel/{id:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> CancelGoodsReceipt(Guid id) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        if (!GoodsReceiptAuthorizationHelper.HasRequiredRole(sessionInfo, document.Type, supervisorOnly: true, settings)) {
            return Forbid();
        }

        bool result = await receiptService.CancelGoodsReceipt(id, sessionInfo);
        return Ok(result);
    }

    /// <summary>
    /// Processes a goods receipt document, creating the actual inventory movement (supervisor only)
    /// </summary>
    /// <param name="id">The unique identifier of the goods receipt to process</param>
    /// <returns>Response indicating success or failure of the processing operation</returns>
    /// <response code="200">Returns the process goods receipt response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("process/{id:guid}")]
    [ProducesResponseType(typeof(ProcessGoodsReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProcessGoodsReceiptResponse>> ProcessGoodsReceipt(Guid id) {
        var sessionInfo = HttpContext.GetSession();

        // Get document type to determine required role
        var document = await receiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        if (!GoodsReceiptAuthorizationHelper.HasRequiredRole(sessionInfo, document.Type, supervisorOnly: true, settings)) {
            return Forbid();
        }

        return await receiptService.ProcessGoodsReceipt(id, sessionInfo);
    }

    /// <summary>
    /// Gets a list of goods receipt documents with optional filtering
    /// </summary>
    /// <param name="request">The request containing filter criteria</param>
    /// <returns>A list of goods receipt documents matching the filter criteria</returns>
    /// <response code="200">Returns the list of goods receipts</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <remarks>
    /// Uses POST method to support complex filtering criteria that may exceed URL length limits.
    /// The Confirm parameter determines which role permissions are required.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(IEnumerable<GoodsReceiptResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<GoodsReceiptResponse>>> GetGoodsReceipts([FromBody] GoodsReceiptsRequest request) {
        var sessionInfo = HttpContext.GetSession();

        var requiredRoles = request.ProcessType switch {
            GoodsReceiptProcessType.Regular => new[] { RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor },
            GoodsReceiptProcessType.Confirmation => new[] { RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor },
            GoodsReceiptProcessType.TransferConfirmation => new[] { RoleType.Transfer, RoleType.TransferSupervisor },
            _ => throw new ArgumentOutOfRangeException()
        };

        if (!sessionInfo.SuperUser && !requiredRoles.Any(role => sessionInfo.Roles.Contains(role))) {
            return Forbid();
        }

        request.WhsCode = sessionInfo.Warehouse;
        return Ok(await receiptService.GetGoodsReceipts(request, sessionInfo.Warehouse));
    }

    /// <summary>
    /// Gets a specific goods receipt document by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the goods receipt</param>
    /// <returns>The goods receipt document details</returns>
    /// <response code="200">Returns the goods receipt document</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="404">If the goods receipt document is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id:guid}")]
    [RequireAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor,
        RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor,
        RoleType.TransferConfirmation, RoleType.TransferConfirmationSupervisor)]
    [ProducesResponseType(typeof(GoodsReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoodsReceiptResponse>> GetGoodsReceipt(Guid id) {
        var sessionInfo = HttpContext.GetSession();
        var document = await receiptService.GetGoodsReceipt(id);
        if (document == null) {
            return NotFound();
        }

        if (!GoodsReceiptAuthorizationHelper.HasRequiredRole(sessionInfo, document.Type, supervisorOnly: false, settings)) {
            return Forbid();
        }

        return Ok(document);
    }
}
