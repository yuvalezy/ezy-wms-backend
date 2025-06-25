using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs;
using Core.DTOs.Items;
using Core.DTOs.Settings;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Middlewares;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeneralController(IPublicService publicService) : ControllerBase {
    [HttpGet("Warehouses")]
    public async Task<ActionResult<IEnumerable<ExternalValue<string>>>> GetWarehouses([FromQuery] string[]? filter = null) {
        var warehouses = await publicService.GetWarehousesAsync(filter);
        return Ok(warehouses);
    }

    [HttpGet("HomeInfo")]
    public async Task<ActionResult<HomeInfoResponse>> GetHomeInfo() {
        var response = await publicService.GetHomeInfoAsync(HttpContext.GetSession().Warehouse);
        return Ok(response);
    }

    [HttpGet("UserInfo")]
    public async Task<ActionResult<UserInfoResponse>> GetUserInfo() {
        return await publicService.GetUserInfoAsync(HttpContext.GetSession());
    }

    [HttpGet("Vendors")]
    [RequireAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor, RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor)]
    public async Task<ActionResult<IEnumerable<ExternalValue<string>>>> GetVendors() {
        var response = await publicService.GetVendorsAsync();
        return Ok(response);
    }

    [HttpGet("ScanBinLocation")]
    [RequireAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptConfirmation, RoleType.Counting)]
    public async Task<ActionResult<BinLocationResponse?>> ScanBinLocation([FromQuery] string bin) {
        var response = await publicService.ScanBinLocationAsync(bin);
        
        return response != null ? Ok(response) : NotFound();
    }

    [HttpGet("ItemByBarCode")]
    [RequireAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptConfirmation, RoleType.TransferRequest)]
    public async Task<ActionResult<IEnumerable<ItemInfoResponse>>> ScanItemBarCode([FromQuery] string scanCode, [FromQuery] bool item = false) {
        return Ok(await publicService.ScanItemBarCodeAsync(scanCode, item));
    }

    [HttpPost("ItemCheck")]
    [RequireAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor, RoleType.PickingSupervisor,
        RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor)]
    public async Task<ActionResult<IEnumerable<ItemCheckResponse>>> ItemCheck([FromBody] ItemBarCodeRequest request) {
        return Ok(await publicService.ItemCheckAsync(request.ItemCode, request.Barcode));
    }

    [HttpGet("BinCheck")]
    [RequireAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor, RoleType.PickingSupervisor,
        RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor)]
    public async Task<ActionResult<IEnumerable<BinContentResponse>>> BinCheck([FromQuery] int binEntry) {
        return Ok(await publicService.BinCheckAsync(binEntry));
    }

    [HttpPost("ItemStock")]
    [RequireAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor,
        RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor)]
    public async Task<ActionResult<IEnumerable<ItemBinStockResponse>>> ItemStock([FromBody] ItemBarCodeRequest request) {
        if (string.IsNullOrWhiteSpace(request.ItemCode)) {
            return BadRequest("Item code is required.");
        }

        string warehouse = HttpContext.GetSession().Warehouse;
        return Ok(await publicService.ItemStockAsync(request.ItemCode, warehouse));
    }

    [HttpPost("UpdateItemBarCode")]
    [RequireAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor,
        RoleType.GoodsReceiptConfirmationSupervisor)]
    public async Task<UpdateItemBarCodeResponse> UpdateItemBarCode([FromBody] UpdateBarCodeRequest request) {
        return await publicService.UpdateItemBarCode(HttpContext.GetSession().UserId, request);
    }
}