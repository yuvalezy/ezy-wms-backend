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
//
//         [HttpGet("ItemByBarCode")]
//         public ActionResult<IEnumerable<Item>> ScanItemBarCode([FromQuery] string scanCode, [FromQuery] bool item = false)
//         {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptConfirmation, Authorization.TransferRequest))
//                 throw new UnauthorizedAccessException("You don't have access for Scan Item BarCode");
//             return Ok(data.General.ScanItemBarCode(scanCode, item));
//         }
//
//         [HttpPost("ItemCheck")]
//         public ActionResult<IEnumerable<ItemCheckResponse>> ItemCheck([FromBody] ItemBarCodeParameters parameters)
//         {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor, Authorization.CountingSupervisor, Authorization.TransferSupervisor, Authorization.PickingSupervisor,
//                     Authorization.GoodsReceiptConfirmation, Authorization.GoodsReceiptConfirmationSupervisor))
//                 throw new UnauthorizedAccessException("You don't have access for Item Check");
//             return Ok(data.General.ItemCheck(parameters.ItemCode, parameters.Barcode));
//         }
//
//         [HttpGet("BinCheck")]
//         public ActionResult<IEnumerable<BinContent>> BinCheck([FromQuery] int binEntry)
//         {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor, Authorization.CountingSupervisor, Authorization.TransferSupervisor, Authorization.PickingSupervisor,
//                     Authorization.GoodsReceiptConfirmation, Authorization.GoodsReceiptConfirmationSupervisor))
//                 throw new UnauthorizedAccessException("You don't have access for Bin Check");
//             return Ok(data.General.BinCheck(binEntry));
//         }
//
//         [HttpPost("ItemStock")]
//         public ActionResult<IEnumerable<ItemStockResponse>> ItemStock([FromBody] ItemBarCodeParameters parameters)
//         {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor, Authorization.CountingSupervisor, Authorization.TransferSupervisor, Authorization.GoodsReceiptConfirmation,
//                     Authorization.GoodsReceiptConfirmationSupervisor))
//                 throw new UnauthorizedAccessException("You don't have access for Item Stock");
//             string whsCode = Data.General.GetEmployeeData(EmployeeID).WhsCode;
//             return Ok(data.General.ItemStock(parameters.ItemCode, whsCode));
//         }
//
//         [HttpPost("UpdateItemBarCode")]
//         public ActionResult UpdateItemBarCode([FromBody] UpdateBarCodeParameters parameters)
//         {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor, Authorization.CountingSupervisor, Authorization.TransferSupervisor,
//                     Authorization.GoodsReceiptConfirmationSupervisor))
//                 throw new UnauthorizedAccessException("You don't have access for Update Item BarCode");
//             data.General.UpdateItemBarCode(parameters.ItemCode, parameters.Barcode, parameters.ID, parameters.Type);
//             return Ok();
//         }
//
//         [HttpPost("UpdateDetails")]
//         public ActionResult<UpdateLineResponse> UpdateDetails([FromBody] UpdateDetailParameters parameters)
//         {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.All))
//                 throw new UnauthorizedAccessException("You don't have access for Update Details");
//             return data.General.UpdateDetails(parameters.Bin, parameters.ID, parameters.Type, parameters.Table, parameters.SupplierRef, parameters.Quantity);
//         }
//
//         [HttpGet("ProcessDocument")]
//         public ActionResult<ProcessDocument> ProcessDocument([FromQuery] int docEntry, [FromQuery] ProcessType type)
//         {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptSupervisor, Authorization.GoodsReceiptConfirmation,
//                     Authorization.GoodsReceiptConfirmationSupervisor, Authorization.Counting, Authorization.CountingSupervisor, Authorization.Transfer, Authorization.TransferSupervisor,
//                     Authorization.Picking, Authorization.PickingSupervisor))
//                 throw new UnauthorizedAccessException("You don't have access for Process Document");
//             return data.General.ProcessDocument(docEntry, type);
//         }
//     }
// }
}