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

        if (string.IsNullOrWhiteSpace(parameters.CardCode))
            throw new ArgumentException("Card Code cannot be empty", nameof(parameters.CardCode));
        if (!data.GeneralData.ValidateVendor(parameters.CardCode))
            throw new ArgumentException($"Card Code {parameters.CardCode} is not aa valid vendor", nameof(parameters.CardCode));

        if (string.IsNullOrWhiteSpace(parameters.Name))
            throw new ArgumentException("Name cannot be empty", nameof(parameters.Name));

        int id = data.GoodsReceiptData.CreateDocument(parameters.CardCode, parameters.Name, EmployeeID);
        return data.GoodsReceiptData.GetDocument(id);
    }

    [HttpPost]
    [ActionName("AddItem")]
    public AddItemReturnValue AddItem([FromBody] AddItemParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt))
            throw new UnauthorizedAccessException("You don't have access for adding item to document");
        if (!parameters.Validate(data))
            return AddItemReturnValue.ClosedDocument;
        return data.GoodsReceiptData.AddItem(parameters.ID, parameters.ItemCode, parameters.BarCode, EmployeeID);
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
        return data.GoodsReceiptData.ProcessDocument(parameters.ID, EmployeeID);
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
    [Route("GoodsReceiptVSExitReport/{id:int}")]
    public List<GoodsReceiptVSExitReport> GetGoodsReceiptVSExitReport(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document report");
        return data.GoodsReceiptData.GetGoodsReceiptVSExitReport(id);
    }
}