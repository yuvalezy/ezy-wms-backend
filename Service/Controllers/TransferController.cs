using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs.General;
using Core.DTOs.Transfer;
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
/// Transfer Controller - Manages inventory transfer operations between warehouses including creation, processing, and line item management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransferController(
    ITransferDocumentService transferDocumentService,
    ITransferContentService transferContentService,
    ITransferProcessingService transferProcessingService,
    ITransferLineService transferLineService,
    ITransferPackageService transferPackageService,
    ISettings settings) : ControllerBase {

    private RoleType[] GetRequiredRole(bool supervisorOnly = false) {
        if (supervisorOnly)
            return [RoleType.TransferSupervisor];

        if (settings.Options.TransferCreateSupervisorRequired)
            return [RoleType.Transfer];

        return [RoleType.Transfer, RoleType.TransferSupervisor];
    }

    private bool HasRequiredRole(SessionInfo sessionInfo, bool supervisorOnly = false) {
        if (sessionInfo.SuperUser) return true;

        var requiredRole = GetRequiredRole(supervisorOnly);
        if (sessionInfo.Roles.Any(r => requiredRole.Contains(r)))
            return true;

        // Check if user has supervisor role when non-supervisor role is required
        if (!supervisorOnly && sessionInfo.Roles.Contains(RoleType.TransferSupervisor)) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a new transfer document
    /// </summary>
    /// <param name="transferRequest">The transfer creation request containing source/destination warehouse and details</param>
    /// <returns>The created transfer document with its details</returns>
    /// <response code="200">Returns the created transfer</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("create")]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TransferResponse>> CreateTransfer([FromBody] CreateTransferRequest transferRequest) {
        var sessionInfo = HttpContext.GetSession();

        if (!HasRequiredRole(sessionInfo)) {
            return Forbid();
        }

        var result = await transferDocumentService.CreateTransfer(transferRequest, sessionInfo);
        return Ok(result);
    }

    /// <summary>
    /// Gets processing information for a transfer document
    /// </summary>
    /// <param name="id">The unique identifier of the transfer</param>
    /// <returns>Transfer information required for processing</returns>
    /// <response code="200">Returns the transfer processing information</response>
    /// <response code="404">If the transfer is not found</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("processInfo/{id:guid}")]
    [RequireRolePermission(RoleType.Transfer)]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<TransferResponse> ProcessInfo(Guid id) => await transferDocumentService.GetProcessInfo(id);


    /// <summary>
    /// Adds an item to a transfer document
    /// </summary>
    /// <param name="request">The request containing item details and transfer ID</param>
    /// <returns>Response indicating success or failure of the operation</returns>
    /// <response code="200">Returns the add item response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("addItem")]
    [RequireRolePermission(RoleType.Transfer)]
    [ProducesResponseType(typeof(TransferAddItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<TransferAddItemResponse> AddItem([FromBody] TransferAddItemRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await transferLineService.AddItem(sessionInfo, request);
    }

    /// <summary>
    /// Adds a source package to a transfer document by scanning its barcode
    /// </summary>
    /// <param name="request">The request containing package barcode and transfer ID</param>
    /// <returns>Response with package contents that were added to the transfer</returns>
    /// <response code="200">Returns the package scan response with contents</response>
    /// <response code="400">If the package is not found or is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("addSourcePackage")]
    [RequireRolePermission(RoleType.Transfer)]
    [ProducesResponseType(typeof(TransferAddItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<TransferAddItemResponse> AddSourcePackage([FromBody] TransferAddSourcePackageRequest request) {
        if (!settings.Options.EnablePackages) {
            return new TransferAddItemResponse { ErrorMessage = "Package feature is not enabled" };
        }

        var sessionInfo = HttpContext.GetSession();
        return await transferPackageService.HandleSourcePackageScanAsync(request, sessionInfo);
    }

    /// <summary>
    /// Transfers a package to the target location
    /// </summary>
    /// <param name="request">The request containing package ID and target location details</param>
    /// <returns>Response indicating success of the package transfer</returns>
    /// <response code="200">Returns the package transfer response</response>
    /// <response code="400">If the package or transfer is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("addTargetPackage")]
    [RequireRolePermission(RoleType.Transfer)]
    [ProducesResponseType(typeof(TransferAddItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<TransferAddItemResponse> AddTargetPackage([FromBody] TransferAddTargetPackageRequest request) {
        if (!settings.Options.EnablePackages) {
            return new TransferAddItemResponse { ErrorMessage = "Package feature is not enabled" };
        }

        var sessionInfo = HttpContext.GetSession();
        return await transferPackageService.HandleTargetPackageTransferAsync(request, sessionInfo);
    }

    /// <summary>
    /// Updates a specific line in a transfer document
    /// </summary>
    /// <param name="request">The request containing line details to update</param>
    /// <returns>Response indicating success or failure of the update</returns>
    /// <response code="200">Returns the update line response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("updateLine")]
    [RequireRolePermission(RoleType.Transfer)]
    [ProducesResponseType(typeof(UpdateLineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<UpdateLineResponse> UpdateLine([FromBody] TransferUpdateLineRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await transferLineService.UpdateLine(sessionInfo, request);
    }

    /// <summary>
    /// Updates the quantity of a specific line in a transfer document
    /// </summary>
    /// <param name="request">The request containing line ID and new quantity</param>
    /// <returns>Response indicating success or failure of the quantity update</returns>
    /// <response code="200">Returns the update line response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("updateLineQuantity")]
    [RequireRolePermission(RoleType.Transfer)]
    [ProducesResponseType(typeof(UpdateLineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<UpdateLineResponse> UpdateLineQuantity([FromBody] TransferUpdateLineQuantityRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await transferLineService.UpdateLineQuantity(sessionInfo, request);
    }

    /// <summary>
    /// Cancels a transfer document (supervisor only)
    /// </summary>
    /// <param name="request">The cancellation request containing transfer ID</param>
    /// <returns>Success indication</returns>
    /// <response code="200">Returns success status</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("cancel")]
    [RequireRolePermission(RoleType.TransferSupervisor)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelTransfer([FromBody] CancelTransferRequest request) {
        var sessionInfo = HttpContext.GetSession();

        if (!HasRequiredRole(sessionInfo, supervisorOnly: true)) {
            return Forbid();
        }

        bool result = await transferProcessingService.CancelTransfer(request.ID, sessionInfo);
        return Ok(new { success = result });
    }

    /// <summary>
    /// Processes a transfer document, creating the actual inventory movement
    /// </summary>
    /// <param name="request">The processing request containing transfer ID</param>
    /// <returns>Response indicating success or failure of the processing operation</returns>
    /// <response code="200">Returns the process transfer response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("process")]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    [ProducesResponseType(typeof(ProcessTransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProcessTransferResponse>> ProcessTransfer([FromBody] ProcessTransferRequest request) {
        var sessionInfo = HttpContext.GetSession();
        var result      = await transferProcessingService.ProcessTransfer(request.ID, sessionInfo);
        return Ok(result);
    }

    /// <summary>
    /// Approves or rejects a cross-warehouse transfer request (supervisor only)
    /// </summary>
    /// <param name="request">The approval request containing transfer ID, approval status, and optional rejection reason</param>
    /// <returns>Response indicating success or failure of the approval/rejection operation</returns>
    /// <response code="200">Returns the process transfer response</response>
    /// <response code="400">If the request is invalid or transfer is not in waiting for approval status</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("approve")]
    [RequireRolePermission(RoleType.TransferSupervisor)]
    [ProducesResponseType(typeof(ProcessTransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProcessTransferResponse>> ApproveTransfer([FromBody] TransferApprovalRequest request) {
        var sessionInfo = HttpContext.GetSession();
        var result      = await transferProcessingService.ApproveTransferRequest(request, sessionInfo);
        return Ok(result);
    }

    /// <summary>
    /// Gets a list of transfer documents with optional filtering
    /// </summary>
    /// <param name="request">The request containing filter criteria</param>
    /// <returns>A list of transfer documents matching the filter criteria</returns>
    /// <response code="200">Returns the list of transfers</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<TransferResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IEnumerable<TransferResponse>> GetTransfers([FromQuery] TransfersRequest request) => await transferDocumentService.GetTransfers(request, HttpContext.GetSession().Warehouse);

    /// <summary>
    /// Gets a specific transfer document by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the transfer</param>
    /// <returns>The transfer document details</returns>
    /// <response code="200">Returns the transfer document</response>
    /// <response code="404">If the transfer is not found</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet]
    [Route("{id:guid}")]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<TransferResponse> GetTransfer(Guid id) => await transferDocumentService.GetTransfer(id);

    /// <summary>
    /// Gets the content details for a transfer document
    /// </summary>
    /// <param name="request">The request containing transfer content criteria</param>
    /// <returns>A list of transfer content items</returns>
    /// <response code="200">Returns the transfer content</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("transferContent")]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<TransferContentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IEnumerable<TransferContentResponse>> TransferContent([FromBody] TransferContentRequest request) {
        return await transferContentService.GetTransferContent(request);
    }

    /// <summary>
    /// Gets detailed target information for transfer content items
    /// </summary>
    /// <param name="request">The request containing transfer content target detail criteria</param>
    /// <returns>A list of transfer content target details</returns>
    /// <response code="200">Returns the transfer content target details</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("transferContentTargetDetail")]
    [RequireAnyRole(RoleType.Transfer, RoleType.TransferSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<TransferContentTargetDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IEnumerable<TransferContentTargetDetailResponse>> TransferContentTargetDetail([FromBody] TransferContentTargetDetailRequest request) {
        return await transferContentService.GetTransferContentTargetDetail(request);
    }

    /// <summary>
    /// Updates target detail information for transfer content (supervisor only)
    /// </summary>
    /// <param name="request">The request containing updated target detail information</param>
    /// <returns>Success indication</returns>
    /// <response code="200">Returns success status</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("updateContentTargetDetail")]
    [RequireRolePermission(RoleType.TransferSupervisor)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateContentTargetDetail([FromBody] TransferUpdateContentTargetDetailRequest request) {
        var sessionInfo = HttpContext.GetSession();
        await transferContentService.UpdateContentTargetDetail(request, sessionInfo);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Creates a transfer request document
    /// </summary>
    /// <param name="request">The transfer request creation details</param>
    /// <returns>The created transfer request response</returns>
    /// <response code="200">Returns the created transfer request</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("createTransferRequest")]
    [RequireRolePermission(RoleType.TransferRequest)]
    [ProducesResponseType(typeof(CreateTransferRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<CreateTransferRequestResponse> CreateTransferRequest([FromBody] CreateTransferRequestRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await transferProcessingService.CreateTransferRequest(request, sessionInfo);
    }
}