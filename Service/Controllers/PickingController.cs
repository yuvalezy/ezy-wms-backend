using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Service.Middlewares;
using Service.Services;

namespace Service.Controllers;

/// <summary>
/// Picking Controller - Manages pick list operations including retrieval, item picking, processing, and cancellation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PickingController(IPickListService service, IPickListLineService lineService, IPickListProcessService processService, IPickListPackageService packageService, IServiceProvider serviceProvider) : ControllerBase {
    /// <summary>
    /// Gets a list of pick lists with optional filtering
    /// </summary>
    /// <param name="request">The request containing filter criteria</param>
    /// <returns>A list of pick lists matching the filter criteria</returns>
    /// <response code="200">Returns the list of pick lists</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet]
    [RequireAnyRole(RoleType.Picking, RoleType.PickingSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<PickListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IEnumerable<PickListResponse>> GetPickings([FromQuery] PickListsRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await service.GetPickLists(request, sessionInfo.Warehouse);
    }

    /// <summary>
    /// Gets a specific pick list by its ID with optional detail parameters
    /// </summary>
    /// <param name="id">The unique identifier of the pick list</param>
    /// <param name="type">Optional type filter for pick list details</param>
    /// <param name="entry">Optional entry filter for pick list details</param>
    /// <param name="availableBins">Include available bin information in the response</param>
    /// <param name="binEntry">Optional bin entry filter for pick list details</param>
    /// <returns>The pick list details with optional additional data</returns>
    /// <response code="200">Returns the pick list details</response>
    /// <response code="404">If the pick list is not found</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id:int}")]
    [RequireAnyRole(RoleType.Picking, RoleType.PickingSupervisor)]
    [ProducesResponseType(typeof(PickListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PickListResponse>> GetPicking(
        int                                       id,
        [FromQuery(Name = "type")]          int?  type          = null,
        [FromQuery(Name = "entry")]         int?  entry         = null,
        [FromQuery(Name = "availableBins")] bool? availableBins = false,
        [FromQuery(Name = "binEntry")]      int?  binEntry      = null) {
        var sessionInfo = HttpContext.GetSession();

        var detailRequest = new PickListDetailRequest {
            Type          = type,
            Entry         = entry,
            AvailableBins = availableBins,
            BinEntry      = binEntry
        };

        var result = await service.GetPickList(id, detailRequest, sessionInfo.Warehouse);

        if (result == null) {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Adds an item to a pick list
    /// </summary>
    /// <param name="request">The request containing item details and pick list ID</param>
    /// <returns>Response indicating success or failure of the operation</returns>
    /// <response code="200">Returns the add item response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("addItem")]
    [RequireRolePermission(RoleType.Picking)]
    [ProducesResponseType(typeof(PickListAddItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<PickListAddItemResponse> AddItem([FromBody] PickListAddItemRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await lineService.AddItem(sessionInfo, request);
    }

    /// <summary>
    /// Adds an entire package to a pick list
    /// </summary>
    /// <param name="request">The request containing package details and pick list ID</param>
    /// <returns>Response indicating success or failure of the operation</returns>
    /// <response code="200">Returns the add package response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("addPackage")]
    [RequireRolePermission(RoleType.Picking)]
    [ProducesResponseType(typeof(PickListPackageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<PickListPackageResponse> AddPackage([FromBody] PickListAddPackageRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await packageService.AddPackageAsync(request, sessionInfo);
    }

    /// <summary>
    /// Processes a pick list, finalizing the picking operation and triggering background sync (supervisor only)
    /// </summary>
    /// <param name="request">The processing request containing pick list ID</param>
    /// <returns>Response indicating success or failure of the processing operation</returns>
    /// <response code="200">Returns the process pick list response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <remarks>
    /// This operation finalizes the pick list and triggers an immediate background synchronization if the background service is available.
    /// </remarks>
    [HttpPost("process")]
    [RequireRolePermission(RoleType.PickingSupervisor)]
    [ProducesResponseType(typeof(ProcessPickListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ProcessPickListResponse> Process([FromBody] ProcessPickListRequest request) {
        var sessionInfo = HttpContext.GetSession();
        var response    = await processService.ProcessPickList(request.ID, sessionInfo.Guid);

        // Trigger immediate background sync if available
        var backgroundService = serviceProvider.GetService<BackgroundPickListSyncService>();
        if (backgroundService != null) {
            _ = Task.Run(async () => await backgroundService.TriggerSync());
        }

        return response;
    }

    /// <summary>
    /// Cancels a pick list (supervisor only)
    /// </summary>
    /// <param name="request">The cancellation request containing pick list ID</param>
    /// <returns>Response indicating success or failure of the cancellation operation</returns>
    /// <response code="200">Returns the cancel pick list response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("cancel")]
    [RequireRolePermission(RoleType.PickingSupervisor)]
    [ProducesResponseType(typeof(ProcessPickListCancelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ProcessPickListCancelResponse> Cancel([FromBody] ProcessPickListRequest request) {
        var sessionInfo = HttpContext.GetSession();
        var response    = await processService.CancelPickList(request.ID, sessionInfo);
        return response;
    }
}