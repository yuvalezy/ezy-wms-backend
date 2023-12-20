using System;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.Picking.Models;
using Service.Shared;

namespace Service.API.Picking;

[Authorize, RoutePrefix("api/Picking")]
public class PickingController : LWApiController {
    private readonly Data data = new();

    [HttpGet]
    [ActionName("Pickings")]
    public IEnumerable<PickingDocument> GetPickings([FromUri] PickingParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Picking, Authorization.PickingSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get picking");
        string whsCode = data.GeneralData.GetEmployeeData(EmployeeID).WhsCode;
        parameters.WhsCode = whsCode;
        return data.PickingData.GetPickings(parameters);
    }

    [HttpGet]
    [Route("Picking/{id:int}")]
    public PickingDocument GetPicking(int id, [FromUri(Name = "type")] int? type = null, [FromUri(Name = "entry")] int? entry = null) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Picking, Authorization.PickingSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get picking");
        string whsCode = data.GeneralData.GetEmployeeData(EmployeeID).WhsCode;

        return data.PickingData.GetPicking(id, whsCode, type, entry);
    }

    [HttpPost]
    [ActionName("AddItem")]
    public AddItemResponse AddItem([FromBody] AddItemParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Picking))
            throw new UnauthorizedAccessException("You don't have access for picking item");
        if (!parameters.Validate(data, EmployeeID))
            return new AddItemResponse { ClosedDocument = true };
        return data.PickingData.AddItem(parameters.ID, parameters.PickEntry, parameters.Quantity, EmployeeID);
    }
}