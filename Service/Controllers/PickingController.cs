using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs.PickList;
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
public class PickingController(IPickListService pickListService) : ControllerBase {
    [HttpGet]
    [RequireAnyRole(RoleType.Picking, RoleType.PickingSupervisor)]
    public async Task<IEnumerable<PickListResponse>> GetPickings([FromQuery] PickListsRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await pickListService.GetPickLists(request, sessionInfo.Warehouse);
    }
    
    [HttpGet("{id:int}")]
    [RequireAnyRole(RoleType.Picking, RoleType.PickingSupervisor)]
    public async Task<ActionResult<PickListResponse>> GetPicking(
        int id,
        [FromQuery(Name = "type")] int? type = null,
        [FromQuery(Name = "entry")] int? entry = null,
        [FromQuery(Name = "availableBins")] bool? availableBins = false,
        [FromQuery(Name = "binEntry")] int? binEntry = null) {
        var sessionInfo = HttpContext.GetSession();
        
        var detailRequest = new PickListDetailRequest {
            Type = type,
            Entry = entry,
            AvailableBins = availableBins,
            BinEntry = binEntry
        };
        
        var result = await pickListService.GetPickList(id, detailRequest, sessionInfo.Warehouse);
        
        if (result == null) {
            return NotFound();
        }
        
        return Ok(result);
    }
    
    [HttpPost("addItem")]
    [RequireRolePermission(RoleType.Picking)]
    public async Task<PickListAddItemResponse> AddItem([FromBody] PickListAddItemRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await pickListService.AddItem(sessionInfo, request);
    }
    
    [HttpPost("process")]
    [RequireRolePermission(RoleType.PickingSupervisor)]
    public async Task<ProcessPickListResponse> Process([FromBody] ProcessPickListRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await pickListService.ProcessPickList(request.ID, sessionInfo);
    }
}