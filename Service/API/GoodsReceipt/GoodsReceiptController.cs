using System;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.GoodsReceipt.Models;
using Service.API.Models;
using Service.Shared;

namespace Service.API.GoodsReceipt;

[Authorize, RoutePrefix("api/GoodsReceipt")]
public class GoodsReceiptController : LWApiController {

    [HttpPost]
    [ActionName("Create")]
    public Document CreateDocument([FromBody] CreateParameters parameters) {
        var authorizations = new []{Authorization.GoodsReceiptSupervisor};
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
        if (!parameters.Validate(Data, EmployeeID))
            return new AddItemResponse { ClosedDocument = true };
        return Data.GoodsReceipt.AddItem(parameters.ID, parameters.ItemCode, parameters.BarCode, EmployeeID);
    }

    [HttpPost]
    [ActionName("UpdateLine")]
    public UpdateLineReturnValue UpdateLine([FromBody] UpdateLineParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt))
            throw new UnauthorizedAccessException("You don't have access for updating line in document");
        (var returnValue, int empID) = parameters.Validate(Data);
        if (returnValue != UpdateLineReturnValue.Ok)
            return returnValue;
        Data.GoodsReceipt.UpdateLine(parameters, empID);
        return returnValue;
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
        return Data.GoodsReceipt.ProcessDocument(parameters.ID, EmployeeID, Data.General.AlertUsers);
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
    [Route("GoodsReceiptVSExitReport/{id:int}")]
    public List<GoodsReceiptVSExitReport> GetGoodsReceiptVSExitReport(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document report");
        return Data.GoodsReceipt.GetGoodsReceiptVSExitReport(id);
    }
}