using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Service.Middlewares;
using Service.Services;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PickingController(IPickListService service, IPickListProcessService processService, IServiceProvider serviceProvider) : ControllerBase {
    [HttpGet]
    [RequireAnyRole(RoleType.Picking, RoleType.PickingSupervisor)]
    public async Task<IEnumerable<PickListResponse>> GetPickings([FromQuery] PickListsRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await service.GetPickLists(request, sessionInfo.Warehouse);
    }

    [HttpGet("{id:int}")]
    [RequireAnyRole(RoleType.Picking, RoleType.PickingSupervisor)]
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

    [HttpPost("addItem")]
    [RequireRolePermission(RoleType.Picking)]
    public async Task<PickListAddItemResponse> AddItem([FromBody] PickListAddItemRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await service.AddItem(sessionInfo, request);
    }

    [HttpPost("process")]
    [RequireRolePermission(RoleType.PickingSupervisor)]
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

    [HttpPost("cancel")]
    [RequireRolePermission(RoleType.PickingSupervisor)]
    public async Task<ProcessPickListCancelResponse> Cancel([FromBody] ProcessPickListRequest request) {
        var sessionInfo = HttpContext.GetSession();
        var response    = await processService.CancelPickList(request.ID, sessionInfo);
        return response;
    }
}