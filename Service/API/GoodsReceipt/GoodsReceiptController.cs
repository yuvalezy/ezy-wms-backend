using System;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.GoodsReceipt.Models;
using Service.API.Models;
using Service.Shared;

namespace Service.API.GoodsReceipt;

[Authorize, RoutePrefix("api/GoodsReceipt")]
public class GoodsReceiptController : LWApiController {
    private readonly Data data = new();

    [HttpPost]
    [ActionName("Create")]
    public Document CreateDocument([FromBody] CreateParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for document creation");

        var validateReturnValue = parameters.Validate(data, EmployeeID);
        if (validateReturnValue != null)
            return validateReturnValue;

        int id = data.GoodsReceiptData.CreateDocument(parameters, EmployeeID);
        return data.GoodsReceiptData.GetDocument(id);
    }

    [HttpPost]
    [ActionName("AddItem")]
    public AddItemResponse AddItem([FromBody] AddItemParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt))
            throw new UnauthorizedAccessException("You don't have access for adding item to document");
        if (!parameters.Validate(data, EmployeeID))
            return new AddItemResponse { ClosedDocument = true };
        return data.GoodsReceiptData.AddItem(parameters.ID, parameters.ItemCode, parameters.BarCode, EmployeeID);
    }

    [HttpPost]
    [ActionName("UpdateLine")]
    public UpdateLineReturnValue UpdateLine([FromBody] UpdateLineParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt))
            throw new UnauthorizedAccessException("You don't have access for updating line in document");
        (var returnValue, int empID) = parameters.Validate(data);
        if (returnValue != UpdateLineReturnValue.Ok)
            return returnValue;
        data.GoodsReceiptData.UpdateLine(parameters, empID);
        return returnValue;
    }

    [HttpPost]
    [ActionName("Cancel")]
    public bool CancelDocument([FromBody] IDParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for document cancellation");

        return data.GoodsReceiptData.CancelDocument(parameters.ID, EmployeeID);
    }

    [HttpPost]
    [ActionName("Process")]
    public bool ProcessDocument([FromBody] IDParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for document cancellation");
        return data.GoodsReceiptData.ProcessDocument(parameters.ID, EmployeeID, data.GeneralData.AlertUsers);
    }

    [HttpGet]
    [ActionName("CancelReasons")]
    public IEnumerable<ValueDescription<int>> GetCancelReasons() {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt))
            throw new UnauthorizedAccessException("You don't have access to get cancel reasons");
        var values = new List<ValueDescription<int>>();
        Global.DataObject.ExecuteReader("select \"Code\", \"Name\" from \"@LW_YUVAL08_GRPO_CR\" order by 2", dr => {
            var value = new ValueDescription<int>((int)dr["Code"], (string)dr["Name"]);
            values.Add(value);
        });
        return values;
    }

    [HttpGet]
    [ActionName("Documents")]
    public IEnumerable<Document> GetDocuments([FromUri] FilterParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document");
        parameters.WhsCode = data.GeneralData.GetEmployeeData(EmployeeID).WhsCode;
        return data.GoodsReceiptData.GetDocuments(parameters);
    }

    [HttpGet]
    [Route("Document/{id:int}")]
    public Document GetDocument(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document");
        return data.GoodsReceiptData.GetDocument(id);
    }

    [HttpGet]
    [Route("GoodsReceiptAll/{id:int}")]
    public List<GoodsReceiptReportAll> GetGoodsReceiptAllReport(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document report");
        return data.GoodsReceiptData.GetGoodsReceiptAllReport(id);
    }

    [HttpGet]
    [Route("GoodsReceiptVSExitReport/{id:int}")]
    public List<GoodsReceiptVSExitReport> GetGoodsReceiptVSExitReport(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document report");
        return data.GoodsReceiptData.GetGoodsReceiptVSExitReport(id);
    }
}