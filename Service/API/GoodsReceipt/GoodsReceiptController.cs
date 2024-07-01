using System;
using System.Collections.Generic;
using System.Web.Http;
using SAPbobsCOM;
using Service.API.General.Models;
using Service.API.GoodsReceipt.Models;
using Service.API.Models;
using Service.API.Transfer.Models;
using Service.Shared;
using AddItemParameter = Service.API.GoodsReceipt.Models.AddItemParameter;
using AddItemResponse = Service.API.GoodsReceipt.Models.AddItemResponse;
using CreateParameters = Service.API.GoodsReceipt.Models.CreateParameters;
using FilterParameters = Service.API.GoodsReceipt.Models.FilterParameters;
using UpdateLineParameter = Service.API.GoodsReceipt.Models.UpdateLineParameter;

namespace Service.API.GoodsReceipt;

[Authorize, RoutePrefix("api/GoodsReceipt")]
public class GoodsReceiptController : LWApiController {
    [HttpPost]
    [ActionName("Create")]
    public Document CreateDocument([FromBody] CreateParameters parameters) {
        var authorizations = new[] { Authorization.GoodsReceiptSupervisor };
        if (!Global.GRPOCreateSupervisorRequired) {
            Array.Resize(ref authorizations, authorizations.Length + 1);
            authorizations[authorizations.Length - 1] = Authorization.GoodsReceipt;
        }

        if (!Global.ValidateAuthorization(EmployeeID, authorizations))
            throw new UnauthorizedAccessException("You don't have access for document creation");

        var validateReturnValue = parameters.Validate(Data, EmployeeID);
        if (validateReturnValue != null)
            return validateReturnValue;

        int id = Data.GoodsReceipt.CreateDocument(parameters, EmployeeID);
        return Data.GoodsReceipt.GetDocument(id);
    }

    [HttpPost]
    [ActionName("AddItem")]
    public AddItemResponse AddItem([FromBody] AddItemParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt))
            throw new UnauthorizedAccessException("You don't have access for adding item to document");
        using var conn = Global.Connector;
        try {
            conn.BeginTransaction();
            if (!parameters.Validate(conn, Data, EmployeeID))
                return new AddItemResponse { ClosedDocument = true };
            var addItemResponse = Data.GoodsReceipt.AddItem(conn, parameters.ID, parameters.ItemCode, parameters.BarCode, EmployeeID);
            if (string.IsNullOrWhiteSpace(addItemResponse.ErrorMessage))
                conn.CommitTransaction();
            else 
                conn.RollbackTransaction();
            return addItemResponse;
        }
        catch (Exception e) {
            conn.RollbackTransaction();
            throw;
        }
    }

    [HttpPost]
    [ActionName("UpdateLine")]
    public UpdateLineReturnValue UpdateLine([FromBody] UpdateLineParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt))
            throw new UnauthorizedAccessException("You don't have access for updating line in document");
        using var conn = Global.Connector;
        try {
            conn.BeginTransaction();
            (var returnValue, int empID) = parameters.Validate(conn, Data);
            if (returnValue != UpdateLineReturnValue.Ok)
                return returnValue;
            Data.GoodsReceipt.UpdateLine(conn, parameters, empID);
            conn.CommitTransaction();
            return returnValue;
        }
        catch {
            conn.RollbackTransaction();
            throw;
        }
    }
    [HttpPost]
    [ActionName("UpdateLineQuantity")]
    public UpdateItemResponse UpdateLineQuantity([FromBody] UpdateLineQuantityParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt))
            throw new UnauthorizedAccessException("You don't have access for updating line in document");
        using var conn = Global.Connector;
        try {
            conn.BeginTransaction();
            (var returnValue, int empID) = parameters.Validate(conn, Data);
            if (returnValue.ReturnValue != UpdateLineReturnValue.Ok)
                return returnValue;
            var updateItemResponse = Data.GoodsReceipt.UpdateLineQuantity(conn, parameters, EmployeeID);
            if (string.IsNullOrWhiteSpace(updateItemResponse.ErrorMessage))
                conn.CommitTransaction();
            else 
                conn.RollbackTransaction();
            return updateItemResponse;
        }
        catch (Exception e) {
            conn.RollbackTransaction();
            throw;
        }
    }

    [HttpPost]
    [ActionName("Cancel")]
    public bool CancelDocument([FromBody] IDParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for document cancellation");

        return Data.GoodsReceipt.CancelDocument(parameters.ID, EmployeeID);
    }

    [HttpPost]
    [ActionName("Process")]
    public bool ProcessDocument([FromBody] IDParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for document cancellation");
        bool enableBin = Data.General.GetEmployeeData(EmployeeID).EnableBin;
        return Data.GoodsReceipt.ProcessDocument(parameters.ID, EmployeeID, enableBin, Data.General.AlertUsers);
    }

    [HttpGet]
    [ActionName("CancelReasons")]
    public IEnumerable<ValueDescription<int>> GetCancelReasons() {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt))
            throw new UnauthorizedAccessException("You don't have access to get cancel reasons");
        return Data.General.GetCancelReasons(ReasonType.GoodsReceipt);
    }

    [HttpGet]
    [ActionName("Documents")]
    public IEnumerable<Document> GetDocuments([FromUri] FilterParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document");
        parameters.WhsCode = Data.General.GetEmployeeData(EmployeeID).WhsCode;
        return Data.GoodsReceipt.GetDocuments(parameters);
    }

    [HttpGet]
    [Route("Document/{id:int}")]
    public Document GetDocument(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document");
        return Data.GoodsReceipt.GetDocument(id);
    }

    [HttpGet]
    [Route("GoodsReceiptAll/{id:int}")]
    public List<GoodsReceiptReportAll> GetGoodsReceiptAllReport(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document report");
        return Data.GoodsReceipt.GetGoodsReceiptAllReport(id);
    }

    [HttpGet]
    [Route("GoodsReceiptAll/{id:int}/{item}")]
    public List<GoodsReceiptReportAllDetails> GetGoodsReceiptAllReportDetails(int id, string item) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document report");
        return Data.GoodsReceipt.GetGoodsReceiptAllReportDetails(id, item);
    }

    [HttpPost]
    [ActionName("UpdateGoodsReceiptAll")]
    public void UpdateGoodsReceiptAll([FromBody] UpdateDetailParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for document cancellation");
        Data.GoodsReceipt.UpdateGoodsReceiptAll(parameters, EmployeeID);
    }

    [HttpGet]
    [Route("GoodsReceiptVSExitReport/{id:int}")]
    public List<GoodsReceiptVSExitReport> GetGoodsReceiptVSExitReport(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document report");
        return Data.GoodsReceipt.GetGoodsReceiptVSExitReport(id);
    }
    
    [HttpGet]
    [Route("GoodsReceiptValidateProcess/{id:int}")]
    public List<GoodsReceiptValidateProcess> GetGoodsReceiptValidateProcess(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document report");
        return Data.GoodsReceipt.GetGoodsReceiptValidateProcess(id);
    }
}