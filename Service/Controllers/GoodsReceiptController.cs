using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs.General;
using Core.DTOs.GoodsReceipt;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Middlewares;

namespace Service.Controllers;

/// <summary>
/// Goods Receipt Controller - Manages goods receipt operations including creation, processing, and reporting
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GoodsReceiptController(
    IGoodsReceiptService receiptService,
    IGoodsReceiptReportService receiptReportService,
    IGoodsReceiptLineService receiptLineService,
    IExternalCommandService externalCommandService,
    ISettings settings)
: ControllerBase {
    private RoleType[] GetRequiredRole(GoodsReceiptType type, bool supervisorOnly = false) {
        switch (type) {
            case GoodsReceiptType.All:
                if (supervisorOnly)
                    return [RoleType.GoodsReceiptSupervisor, RoleType.GoodsReceiptConfirmationSupervisor, RoleType.TransferConfirmationSupervisor];
                else
                    return [RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor, RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor, RoleType.TransferConfirmation, RoleType.TransferConfirmationSupervisor];
            case GoodsReceiptType.SpecificTransfers:
                if (supervisorOnly)
                    return [RoleType.TransferConfirmationSupervisor];
                else if (settings.Options.GoodsReceiptCreateSupervisorRequired)
                    return [RoleType.TransferConfirmation];
                else
                    return [RoleType.TransferConfirmation, RoleType.TransferConfirmationSupervisor];
            case GoodsReceiptType.SpecificReceipts:
                if (supervisorOnly)
                    return [RoleType.GoodsReceiptConfirmationSupervisor];
                else if (settings.Options.GoodsReceiptCreateSupervisorRequired)
                    return [RoleType.GoodsReceiptConfirmation];
                else
                    return [RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor];
            case GoodsReceiptType.SpecificOrders:
                if (supervisorOnly)
                    return [RoleType.GoodsReceiptSupervisor];
                else if (settings.Options.GoodsReceiptCreateSupervisorRequired)
                    return [RoleType.GoodsReceipt];
                else
                    return [RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor];
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool HasRequiredRole(SessionInfo sessionInfo, GoodsReceiptType type, bool supervisorOnly = false) {
        if (sessionInfo.SuperUser) return true;

        var requiredRole = GetRequiredRole(type, supervisorOnly);
        if (sessionInfo.Roles.Any(r => requiredRole.Contains(r)))
            return true;

        // Check if user has supervisor role when non-supervisor role is required
        if (!supervisorOnly) {
            var supervisorRole = type switch {
                GoodsReceiptType.SpecificTransfers => RoleType.TransferConfirmationSupervisor,
                GoodsReceiptType.SpecificReceipts => RoleType.GoodsReceiptConfirmationSupervisor,
                _ => RoleType.GoodsReceiptSupervisor
            };

            return sessionInfo.Roles.Contains(supervisorRole);
        }

        return false;
    }

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

        if (!HasRequiredRole(sessionInfo, request.Type)) {
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

        if (!HasRequiredRole(sessionInfo, document.Type)) {
            return Forbid();
        }

        try {
            // Standard goods receipt line creation
            var response = await receiptLineService.AddItem(sessionInfo, request);

            if (request.StartNewPackage) {
                await externalCommandService.ExecuteCommandsAsync(CommandTriggerType.CreatePackage, ObjectType.Package, response.PackageId!.Value);
            }

            return response;
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

        if (!HasRequiredRole(sessionInfo, document.Type)) {
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

        if (!HasRequiredRole(sessionInfo, document.Type)) {
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

        if (!HasRequiredRole(sessionInfo, document.Type, supervisorOnly: true)) {
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

        if (!HasRequiredRole(sessionInfo, document.Type, supervisorOnly: true)) {
            return Forbid();
        }

        var response = await receiptService.ProcessGoodsReceipt(id, sessionInfo);

        foreach (var packageId in response.ActivatedPackages) {
            await externalCommandService.ExecuteCommandsAsync(CommandTriggerType.ActivatePackage, ObjectType.Package, packageId);
        }

        return response;
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

        if (!HasRequiredRole(sessionInfo, document.Type)) {
            return Forbid();
        }

        return Ok(document);
    }

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