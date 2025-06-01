// using System;
// using System.Collections.Generic;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Authorization;
// using Service.API.General.Models;
// using Service.API.Models;
// using Service.Shared;
//
// namespace Service.API.General
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class PublicController : LWApiController
//     {
//         private readonly Data data = new();
//
//         [HttpGet("CompanyInfo")]
//         public ActionResult<CompanyInfo> GetCompanyInfo() =>
//             new CompanyInfo
//             {
//                 Name = Global.CompanyName,
//             };
//     }
//
//     [Authorize]
//     [ApiController]
//     [Route("api/[controller]")]
//     public class GeneralController : LWApiController
//     {
//         private readonly Data data = new();
//
//         [HttpGet("HomeInfo")]
//         public ActionResult<HomeInfo> GetHomeInfo()
//         {
//             var employeeData = data.General.GetEmployeeData(EmployeeID);
//             return data.General.GetHomeInfo(employeeData);
//         }
//
//         [HttpGet("UserInfo")]
//         public ActionResult<UserInfo> GetUserInfo()
//         {
//             var employeeData = data.General.GetEmployeeData(EmployeeID);
//             return new UserInfo
//             {
//                 ID = EmployeeID,
//                 Name = employeeData.Name,
//                 Branch = employeeData.WhsName,
//                 BinLocations = employeeData.EnableBin,
//                 Authorizations = Global.UserAuthorizations[EmployeeID],
//                 Settings = new ApplicationSettings
//                 {
//                     GRPOModificationSupervisor = Global.GRPOModificationsRequiredSupervisor,
//                     GRPOCreateSupervisorRequired = Global.GRPOCreateSupervisorRequired,
//                     TransferTargetItems = Global.TransferTargetItems
//                 }
//             };
//         }
//
//         [HttpGet("Vendors")]
//         public ActionResult<IEnumerable<BusinessPartner>> GetVendors()
//         {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptSupervisor, Authorization.GoodsReceiptConfirmation,
//                     Authorization.GoodsReceiptConfirmationSupervisor))
//                 throw new UnauthorizedAccessException("You don't have access for vendors list");
//             return Ok(data.General.GetVendors());
//         }
//
//         [HttpGet("ScanBinLocation")]
//         public ActionResult<BinLocation> ScanBinLocation([FromQuery] string bin)
//         {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptConfirmation, Authorization.Counting))
//                 throw new UnauthorizedAccessException("You don't have access for Scan Bin Location");
//             return data.General.ScanBinLocation(bin);
//         }
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