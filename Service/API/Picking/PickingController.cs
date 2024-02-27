using System;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.General.Models;
using Service.API.Picking.Models;
using Service.Shared;

namespace Service.API.Picking;

[Authorize, RoutePrefix("api/Picking")]
public class PickingController : LWApiController {
    [HttpGet]
    [ActionName("Pickings")]
    public IEnumerable<PickingDocument> GetPickings([FromUri] PickingParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Picking, Authorization.PickingSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get picking");
        string whsCode = Data.General.GetEmployeeData(EmployeeID).WhsCode;
        parameters.WhsCode = whsCode;
        return Data.Picking.GetPickings(parameters);
    }

    [HttpGet]
    [Route("Picking/{id:int}")]
    public PickingDocument GetPicking(
        int                                     id,
        [FromUri(Name = "type")]          int?  type          = null,
        [FromUri(Name = "entry")]         int?  entry         = null,
        [FromUri(Name = "availableBins")] bool? availableBins = false,
        [FromUri(Name = "binEntry")]      int?  binEntry      = null) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Picking, Authorization.PickingSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get picking");
        string whsCode = Data.General.GetEmployeeData(EmployeeID).WhsCode;
        return Data.Picking.GetPicking(id, whsCode, type, entry, availableBins, binEntry);
    }

    [HttpPost]
    [ActionName("AddItem")]
    public AddItemResponse AddItem([FromBody] AddItemParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Picking))
            throw new UnauthorizedAccessException("You don't have access for picking item");
        if (!parameters.Validate(Data, EmployeeID))
            return new AddItemResponse { ClosedDocument = true };
        return Data.Picking.AddItem(parameters, EmployeeID);
    }

    [HttpPost]
    [ActionName("Process")]
    public AddItemResponse Process([FromBody] ProcessDocument parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.PickingSupervisor))
            throw new UnauthorizedAccessException("You don't have access for picking supervisor");
        string whsCode = Data.General.GetEmployeeData(EmployeeID).WhsCode;
        return Data.Picking.Process(parameters.ID, whsCode);
    }
}