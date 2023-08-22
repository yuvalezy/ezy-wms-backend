using System;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.GoodsReceipt.Models;
using Service.API.Models;
using Service.Shared;

namespace Service.API.GoodsReceipt;

[Authorize, RoutePrefix("api/GoodsReceipt")]
public class GoodsReceiptController : LWApiController {
    private readonly GoodsReceiptData data = new();
    
    [HttpPost]
    [ActionName("Create")]
    public int CreateDocument([FromBody] CreateParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Role.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for document creation");
        
        if (string.IsNullOrWhiteSpace(parameters.Name))
            throw new ArgumentException("Name cannot be empty", nameof(parameters.Name));

        return data.CreateDocument(parameters.Name, EmployeeID);
    }

    [HttpGet]
    [ActionName("Documents")]
    public IEnumerable<Document> GetDocuments([FromUri] FilterParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Role.GoodsReceipt, Role.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document");
        return data.GetDocuments(parameters);
    }
    [HttpGet]
    [Route("Document/{id:int}")]
    public Document GetDocument(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Role.GoodsReceipt, Role.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get document");
        return data.GetDocument(id);
    }
}