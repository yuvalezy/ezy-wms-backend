using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs;
using Core.DTOs.General;
using Core.DTOs.InventoryCounting;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Service.Middlewares;

namespace Service.Controllers;

/// <summary>
/// Counting Controller - Manages inventory counting operations including creation, item counting, and processing
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CountingController(
    IInventoryCountingsService service,
    IInventoryCountingsLineService lineService,
    IExternalCommandService externalCommandService,
    ILogger<CountingController> logger
) : ControllerBase
{
    /// <summary>
    /// Creates a new inventory counting document (supervisor only)
    /// </summary>
    /// <param name="request">The counting creation request containing counting details</param>
    /// <returns>The created inventory counting document</returns>
    /// <response code="200">Returns the created inventory counting</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("create")]
    [RequireRolePermission(RoleType.CountingSupervisor)]
    [ProducesResponseType(typeof(InventoryCountingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<InventoryCountingResponse> CreateCounting([FromBody] CreateInventoryCountingRequest request)
    {
        var sessionInfo = HttpContext.GetSession();
        return await service.CreateCounting(request, sessionInfo);
    }

    /// <summary>
    /// Gets a list of inventory counting documents with optional filtering
    /// </summary>
    /// <param name="request">The request containing filter criteria</param>
    /// <returns>A list of inventory counting documents matching the filter criteria</returns>
    /// <response code="200">Returns the list of inventory countings</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet]
    [RequireAnyRole(RoleType.Counting, RoleType.CountingSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<InventoryCountingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IEnumerable<InventoryCountingResponse>> GetCountings([FromQuery] InventoryCountingsRequest request)
    {
        var sessionInfo = HttpContext.GetSession();
        return await service.GetCountings(request, sessionInfo.Warehouse);
    }

    /// <summary>
    /// Gets a specific inventory counting document by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the inventory counting</param>
    /// <returns>The inventory counting document details</returns>
    /// <response code="200">Returns the inventory counting document</response>
    /// <response code="404">If the inventory counting is not found</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id:guid}")]
    [RequireAnyRole(RoleType.Counting, RoleType.CountingSupervisor)]
    [ProducesResponseType(typeof(InventoryCountingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<InventoryCountingResponse> GetCounting(Guid id)
    {
        return await service.GetCounting(id);
    }

    /// <summary>
    /// Adds an item to an inventory counting document
    /// </summary>
    /// <param name="request">The request containing item details and counting ID</param>
    /// <returns>Response indicating success or failure of the operation</returns>
    /// <response code="200">Returns the add item response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("addItem")]
    [RequireRolePermission(RoleType.Counting)]
    [ProducesResponseType(typeof(InventoryCountingAddItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<InventoryCountingAddItemResponse> AddItem([FromBody] InventoryCountingAddItemRequest request)
    {
        var sessionInfo = HttpContext.GetSession();

        // Call the standard service method to add item to counting
        var response = await lineService.AddItem(sessionInfo, request);

        if (request.StartNewPackage)
        {
            await externalCommandService.ExecuteCommandsAsync(CommandTriggerType.CreatePackage, ObjectType.Package, response.PackageId!.Value);
        }

        return response;
    }


    /// <summary>
    /// Updates a specific line in an inventory counting document
    /// </summary>
    /// <param name="request">The request containing line details to update</param>
    /// <returns>Response indicating success or failure of the update</returns>
    /// <response code="200">Returns the update line response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("updateLine")]
    [RequireRolePermission(RoleType.Counting)]
    [ProducesResponseType(typeof(UpdateLineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<UpdateLineResponse> UpdateLine([FromBody] InventoryCountingUpdateLineRequest request)
    {
        var sessionInfo = HttpContext.GetSession();
        return await lineService.UpdateLine(sessionInfo, request);
    }

    /// <summary>
    /// Cancels an inventory counting document (supervisor only)
    /// </summary>
    /// <param name="request">The cancellation request containing counting ID</param>
    /// <returns>True if cancellation was successful, false otherwise</returns>
    /// <response code="200">Returns true if cancellation was successful</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("cancel")]
    [RequireRolePermission(RoleType.CountingSupervisor)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> CancelCounting([FromBody] CancelInventoryCountingRequest request)
    {
        var sessionInfo = HttpContext.GetSession();
        return await service.CancelCounting(request.ID, sessionInfo);
    }

    /// <summary>
    /// Processes an inventory counting document, creating actual inventory adjustments (supervisor only)
    /// </summary>
    /// <param name="request">The processing request containing counting ID</param>
    /// <returns>Response indicating success or failure of the processing operation</returns>
    /// <response code="200">Returns the process counting response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("process")]
    [RequireRolePermission(RoleType.CountingSupervisor)]
    [ProducesResponseType(typeof(ProcessInventoryCountingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProcessInventoryCountingResponse>> ProcessCounting([FromBody] ProcessInventoryCountingRequest request)
    {
        var sessionInfo = HttpContext.GetSession();
        var result = await service.ProcessCounting(request.ID, sessionInfo);
        return Ok(result);
    }

    /// <summary>
    /// Gets the content details for an inventory counting document
    /// </summary>
    /// <param name="request">The request containing counting content criteria</param>
    /// <returns>A list of inventory counting content items</returns>
    /// <response code="200">Returns the inventory counting content</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("countingContent")]
    [RequireAnyRole(RoleType.Counting, RoleType.CountingSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<InventoryCountingContentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IEnumerable<InventoryCountingContentResponse>> CountingContent([FromBody] InventoryCountingContentRequest request)
    {
        return await service.GetCountingContent(request);
    }

    /// <summary>
    /// Gets a summary report for an inventory counting document (supervisor only)
    /// </summary>
    /// <param name="id">The unique identifier of the inventory counting</param>
    /// <returns>The inventory counting summary report</returns>
    /// <response code="200">Returns the inventory counting summary report</response>
    /// <response code="404">If the inventory counting is not found</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("countingSummaryReport/{id:guid}")]
    [RequireRolePermission(RoleType.CountingSupervisor)]
    [ProducesResponseType(typeof(InventoryCountingSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<InventoryCountingSummaryResponse> GetCountingSummaryReport(Guid id)
    {
        return await service.GetCountingSummaryReport(id);
    }
}