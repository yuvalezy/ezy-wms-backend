using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.API.General.Models;
using Service.API.Models;
using Service.Middlewares;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PublicController(
    IPublicService         publicService,
    IExternalSystemAdapter externalSystemAdapter,
    ISessionManager        sessionManager,
    ISettings              settings) : ControllerBase {
    [HttpGet("Warehouses")]
    public async Task<ActionResult<IEnumerable<ExternalValue>>> GetWarehouses([FromQuery] string[]? filter = null) {
        var warehouses = await publicService.GetWarehousesAsync(filter);
        return Ok(warehouses);
    }

    [HttpGet("HomeInfo")]
    public async Task<ActionResult<HomeInfo>> GetHomeInfo() {
        var response = await publicService.GetHomeInfoAsync(HttpContext.SessionInfo().Warehouse);
        return Ok(response);
    }

    [HttpGet("UserInfo")]
    public async Task<ActionResult<UserInfoResponse>> GetUserInfo() {
        return await publicService.GetUserInfoAsync(HttpContext.SessionInfo());
    }

    [HttpGet("Vendors")]
    public ActionResult<IEnumerable<ExternalValue>> GetVendors() {
        HttpContext.HasAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor, RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor);
        return Ok(publicService.GetVendorsAsync());
    }

    [HttpGet("ScanBinLocation")]
    public async Task<ActionResult<BinLocation?>> ScanBinLocation([FromQuery] string bin) {
        HttpContext.HasAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptConfirmation, RoleType.Counting);
        return await publicService.ScanBinLocationAsync(bin);
    }

    [HttpGet("ItemByBarCode")]
    public async Task<ActionResult<IEnumerable<Item>>> ScanItemBarCode([FromQuery] string scanCode, [FromQuery] bool item = false) {
        HttpContext.HasAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptConfirmation, RoleType.TransferRequest);
        return Ok(await publicService.ScanItemBarCodeAsync(scanCode, item));
    }

    [HttpPost("ItemCheck")]
    public async Task<ActionResult<IEnumerable<ItemCheckResponse>>> ItemCheck([FromBody] ItemBarCodeParameters parameters) {
        HttpContext.HasAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor, RoleType.PickingSupervisor,
            RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor);
        return Ok(await publicService.ItemCheckAsync(parameters.ItemCode, parameters.Barcode));
    }

    [HttpGet("BinCheck")]
    public async Task<ActionResult<IEnumerable<BinContent>>> BinCheck([FromQuery] int binEntry) {
        HttpContext.HasAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor, RoleType.PickingSupervisor,
            RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor);
        return Ok(await publicService.BinCheckAsync(binEntry));
    }

    [HttpPost("ItemStock")]
    public async Task<ActionResult<IEnumerable<ItemStockResponse>>> ItemStock([FromBody] ItemBarCodeParameters parameters) {
        HttpContext.HasAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor,
            RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor);
        string warehouse = HttpContext.SessionInfo().Warehouse;
        return Ok(await publicService.ItemStockAsync(parameters.ItemCode, warehouse));
    }

    [HttpPost("UpdateItemBarCode")]
    public async Task<UpdateItemBarCodeResponse> UpdateItemBarCode([FromBody] UpdateBarCodeRequest request) {
        HttpContext.HasAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor,
                RoleType.GoodsReceiptConfirmationSupervisor);
        return await publicService.UpdateItemBarCode(HttpContext.SessionInfo().UserId, request);
    }
}