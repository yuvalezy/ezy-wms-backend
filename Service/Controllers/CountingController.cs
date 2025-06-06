using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs;
using Core.DTOs.InventoryCounting;
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
public class CountingController(IInventoryCountingsService inventoryCountingsService, ICancellationReasonService cancellationReasonService) : ControllerBase {
    
    [HttpPost("create")]
    [RequireRolePermission(RoleType.CountingSupervisor)]
    public async Task<InventoryCountingResponse> CreateCounting([FromBody] CreateInventoryCountingRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await inventoryCountingsService.CreateCounting(request, sessionInfo);
    }
    
    [HttpGet]
    [RequireAnyRole(RoleType.Counting, RoleType.CountingSupervisor)]
    public async Task<IEnumerable<InventoryCountingResponse>> GetCountings([FromQuery] InventoryCountingsRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await inventoryCountingsService.GetCountings(request, sessionInfo.Warehouse);
    }
    
    [HttpGet("{id:guid}")]
    [RequireAnyRole(RoleType.Counting, RoleType.CountingSupervisor)]
    public async Task<InventoryCountingResponse> GetCounting(Guid id) {
        return await inventoryCountingsService.GetCounting(id);
    }
    
    [HttpPost("addItem")]
    [RequireRolePermission(RoleType.Counting)]
    public async Task<InventoryCountingAddItemResponse> AddItem([FromBody] InventoryCountingAddItemRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await inventoryCountingsService.AddItem(sessionInfo, request);
    }
    
    [HttpPost("updateLine")]
    [RequireRolePermission(RoleType.Counting)]
    public async Task<UpdateLineResponse> UpdateLine([FromBody] InventoryCountingUpdateLineRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await inventoryCountingsService.UpdateLine(sessionInfo, request);
    }
    
    [HttpPost("cancel")]
    [RequireRolePermission(RoleType.CountingSupervisor)]
    public async Task<ActionResult<bool>> CancelCounting([FromBody] CancelInventoryCountingRequest request) {
        var sessionInfo = HttpContext.GetSession();
        return await inventoryCountingsService.CancelCounting(request.ID, sessionInfo);
    }
    
    [HttpPost("process")]
    [RequireRolePermission(RoleType.CountingSupervisor)]
    public async Task<ActionResult<ProcessInventoryCountingResponse>> ProcessCounting([FromBody] ProcessInventoryCountingRequest request) {
        var sessionInfo = HttpContext.GetSession();
        var result = await inventoryCountingsService.ProcessCounting(request.ID, sessionInfo);
        return Ok(result);
    }
    
    [HttpPost("countingContent")]
    [RequireAnyRole(RoleType.Counting, RoleType.CountingSupervisor)]
    public async Task<IEnumerable<InventoryCountingContentResponse>> CountingContent([FromBody] InventoryCountingContentRequest request) {
        return await inventoryCountingsService.GetCountingContent(request);
    }
    
    [HttpGet("countingSummaryReport/{id:guid}")]
    [RequireRolePermission(RoleType.CountingSupervisor)]
    public async Task<InventoryCountingSummaryResponse> GetCountingSummaryReport(Guid id) {
        return await inventoryCountingsService.GetCountingSummaryReport(id);
    }
}