// using System;
// using System.Collections.Generic;
// using System.Web.Http;
// using Service.API.General.Models;
// using Service.API.GoodsReceipt.Models;
// using Service.API.Models;
// using Service.API.Transfer.Models;
// using Service.Shared;
// using AddItemParameter = Service.API.GoodsReceipt.Models.AddItemParameter;
// using AddItemResponse = Service.API.GoodsReceipt.Models.AddItemResponse;
// using CreateParameters = Service.API.GoodsReceipt.Models.CreateParameters;
// using FilterParameters = Service.API.GoodsReceipt.Models.FilterParameters;
// using UpdateLineParameter = Service.API.GoodsReceipt.Models.UpdateLineParameter;
//

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GoodsReceiptController : ControllerBase {
//     [HttpPost]
//     [ActionName("Create")]
//     public Document CreateDocument([FromBody] CreateParameters parameters) {
//         var authorizations = parameters.Type != GoodsReceiptType.SpecificReceipts ? new[] { Authorization.GoodsReceiptSupervisor } : new[] { Authorization.GoodsReceiptConfirmationSupervisor };
//         if (!Global.GRPOCreateSupervisorRequired) {
//             Array.Resize(ref authorizations, authorizations.Length + 1);
//             authorizations[authorizations.Length - 1] = parameters.Type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceipt : Authorization.GoodsReceiptConfirmation;
//         }
//
//         if (!Global.ValidateAuthorization(EmployeeID, authorizations))
//             throw new UnauthorizedAccessException("You don't have access for document creation");
//
//         var validateReturnValue = parameters.Validate(Data, EmployeeID);
//         if (validateReturnValue != null)
//             return validateReturnValue;
//
//         int id = Data.GoodsReceipt.CreateDocument(parameters, EmployeeID);
//         return Data.GoodsReceipt.GetDocument(id);
//     }
//
//     [HttpPost]
//     [ActionName("AddItem")]
//     public AddItemResponse AddItem([FromBody] AddItemParameter parameters) {
//         var type = Data.GoodsReceipt.GetDocumentType(parameters.ID);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceipt : Authorization.GoodsReceiptConfirmation))
//             throw new UnauthorizedAccessException("You don't have access for adding item to document");
//         using var conn = Global.Connector;
//         try {
//             conn.BeginTransaction();
//             if (!parameters.Validate(conn, Data, EmployeeID))
//                 return new AddItemResponse { ClosedDocument = true };
//             var addItemResponse = Data.GoodsReceipt.AddItem(conn, parameters.ID, parameters.ItemCode, parameters.BarCode, EmployeeID, parameters.Unit!.Value);
//             if (string.IsNullOrWhiteSpace(addItemResponse.ErrorMessage))
//                 conn.CommitTransaction();
//             else
//                 conn.RollbackTransaction();
//             return addItemResponse;
//         }
//         catch (Exception e) {
//             conn.RollbackTransaction();
//             throw;
//         }
//     }
//
//     [HttpPost]
//     [ActionName("UpdateLine")]
//     public UpdateLineReturnValue UpdateLine([FromBody] UpdateLineParameter parameters) {
//         var type = Data.GoodsReceipt.GetDocumentType(parameters.ID);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceipt : Authorization.GoodsReceiptConfirmation))
//             throw new UnauthorizedAccessException("You don't have access for updating line in document");
//         using var conn = Global.Connector;
//         try {
//             conn.BeginTransaction();
//             (var returnValue, int empID) = parameters.Validate(conn, Data);
//             if (returnValue != UpdateLineReturnValue.Ok)
//                 return returnValue;
//             Data.GoodsReceipt.UpdateLine(conn, parameters, empID);
//             conn.CommitTransaction();
//             return returnValue;
//         }
//         catch {
//             conn.RollbackTransaction();
//             throw;
//         }
//     }
//
//     [HttpPost]
//     [ActionName("UpdateLineQuantity")]
//     public UpdateItemResponse UpdateLineQuantity([FromBody] UpdateLineQuantityParameter parameters) {
//         var type = Data.GoodsReceipt.GetDocumentType(parameters.ID);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceipt : Authorization.GoodsReceiptConfirmation))
//             throw new UnauthorizedAccessException("You don't have access for updating line in document");
//         using var conn = Global.Connector;
//         try {
//             conn.BeginTransaction();
//             (var returnValue, int empID) = parameters.Validate(conn, Data);
//             if (returnValue.ReturnValue != UpdateLineReturnValue.Ok)
//                 return returnValue;
//             var updateItemResponse = Data.GoodsReceipt.UpdateLineQuantity(conn, parameters, EmployeeID);
//             if (string.IsNullOrWhiteSpace(updateItemResponse.ErrorMessage))
//                 conn.CommitTransaction();
//             else
//                 conn.RollbackTransaction();
//             return updateItemResponse;
//         }
//         catch (Exception e) {
//             conn.RollbackTransaction();
//             throw;
//         }
//     }
//
//     [HttpPost]
//     [ActionName("Cancel")]
//     public bool CancelDocument([FromBody] IDParameters parameters) {
//         var type = Data.GoodsReceipt.GetDocumentType(parameters.ID);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceiptSupervisor : Authorization.GoodsReceiptConfirmationSupervisor))
//             throw new UnauthorizedAccessException("You don't have access for document cancellation");
//
//         return Data.GoodsReceipt.CancelDocument(parameters.ID, EmployeeID);
//     }
//
//     [HttpPost]
//     [ActionName("Process")]
//     public bool ProcessDocument([FromBody] IDParameters parameters) {
//         var type = Data.GoodsReceipt.GetDocumentType(parameters.ID);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceiptSupervisor : Authorization.GoodsReceiptConfirmationSupervisor))
//             throw new UnauthorizedAccessException("You don't have access for document cancellation");
//         bool enableBin = Data.General.GetEmployeeData(EmployeeID).EnableBin;
//         return Data.GoodsReceipt.ProcessDocument(parameters.ID, EmployeeID, enableBin, Data.General.AlertUsers);
//     }
//
//     [HttpPost]
//     [ActionName]
//     public IEnumerable<Document> GetDocuments([FromBody] FilterParameters parameters) {
//         var authorizations = parameters.Confirm != true
//             ? new[] { Authorization.GoodsReceipt, Authorization.GoodsReceiptSupervisor }
//             : new[] { Authorization.GoodsReceiptConfirmation, Authorization.GoodsReceiptConfirmationSupervisor };
//         if (!Global.ValidateAuthorization(EmployeeID, authorizations))
//             throw new UnauthorizedAccessException("You don't have access to get document");
//         parameters.WhsCode = Data.General.GetEmployeeData(EmployeeID).WhsCode;
//         return Data.GoodsReceipt.GetDocuments(parameters);
//     }
//
//     [HttpGet]
//     [Route("{id:int}")]
//     public Document GetDocument(int id) {
//         var document = Data.GoodsReceipt.GetDocument(id);
//         if (document.Type != GoodsReceiptType.SpecificReceipts) {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptSupervisor))
//                 throw new UnauthorizedAccessException("You don't have access to get document");
//         }
//         else {
//             if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptConfirmation, Authorization.GoodsReceiptConfirmationSupervisor))
//                 throw new UnauthorizedAccessException("You don't have access to get document");
//         }
//
//         return document;
//     }
//
//     [HttpGet]
//     [Route("GoodsReceiptAll/{id:int}")]
//     public List<GoodsReceiptReportAll> GetGoodsReceiptAllReport(int id) {
//         var type = Data.GoodsReceipt.GetDocumentType(id);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceiptSupervisor : Authorization.GoodsReceiptConfirmationSupervisor))
//             throw new UnauthorizedAccessException("You don't have access to get document report");
//         return Data.GoodsReceipt.GetGoodsReceiptAllReport(id);
//     }
//
//     [HttpGet]
//     [Route("GoodsReceiptAll/{id:int}/{item}")]
//     public List<GoodsReceiptReportAllDetails> GetGoodsReceiptAllReportDetails(int id, string item) {
//         var type = Data.GoodsReceipt.GetDocumentType(id);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceiptSupervisor : Authorization.GoodsReceiptConfirmationSupervisor))
//             throw new UnauthorizedAccessException("You don't have access to get document report");
//         return Data.GoodsReceipt.GetGoodsReceiptAllReportDetails(id, item);
//     }
//
//     [HttpPost]
//     [ActionName("UpdateGoodsReceiptAll")]
//     public void UpdateGoodsReceiptAll([FromBody] UpdateDetailParameters parameters) {
//         var type = Data.GoodsReceipt.GetDocumentType(parameters.ID);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceiptSupervisor : Authorization.GoodsReceiptConfirmationSupervisor))
//             throw new UnauthorizedAccessException("You don't have access for document cancellation");
//         Data.GoodsReceipt.UpdateGoodsReceiptAll(parameters, EmployeeID);
//     }
//
//     [HttpGet]
//     [Route("GoodsReceiptVSExitReport/{id:int}")]
//     public List<GoodsReceiptVSExitReport> GetGoodsReceiptVSExitReport(int id) {
//         var type = Data.GoodsReceipt.GetDocumentType(id);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceiptSupervisor : Authorization.GoodsReceiptConfirmationSupervisor))
//             throw new UnauthorizedAccessException("You don't have access to get document report");
//         return Data.GoodsReceipt.GetGoodsReceiptVSExitReport(id);
//     }
//
//     [HttpGet]
//     [Route("GoodsReceiptValidateProcess/{id:int}")]
//     public List<GoodsReceiptValidateProcess> GetGoodsReceiptValidateProcess(int id) {
//         var type = Data.GoodsReceipt.GetDocumentType(id);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceiptSupervisor : Authorization.GoodsReceiptConfirmationSupervisor))
//             throw new UnauthorizedAccessException("You don't have access to get document report");
//         return Data.GoodsReceipt.GetGoodsReceiptValidateProcess(id);
//     }
//
//     [HttpPost]
//     [Route("GoodsReceiptValidateProcessLineDetails")]
//     public List<GoodsReceiptValidateProcessLineDetails> GetGoodsReceiptValidateProcessDetails([FromBody] GoodsReceiptValidateProcessLineDetailsParameters parameters) {
//         var type = Data.GoodsReceipt.GetDocumentType(parameters.ID);
//         if (!Global.ValidateAuthorization(EmployeeID, type != GoodsReceiptType.SpecificReceipts ? Authorization.GoodsReceiptSupervisor : Authorization.GoodsReceiptConfirmationSupervisor))
//             throw new UnauthorizedAccessException("You don't have access to get document report");
//         return Data.GoodsReceipt.GetGoodsReceiptValidateProcessLineDetails(parameters);
//     }
}