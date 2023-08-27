using System;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.Models;
using Service.Shared;

namespace Service.API.General;

[Authorize]
public class GeneralController : LWApiController {
    private readonly Data data = new();

    [HttpGet]
    [ActionName("UserInfo")]
    public UserInfo GetUserInfo() {
        (string name, string branch) = data.GeneralData.GetEmployeeData(EmployeeID);
        return new UserInfo {
            ID             = EmployeeID,
            Name           = name,
            Branch         = branch,
            Authorizations = Global.UserAuthorizations[EmployeeID]
        };
    }

    [HttpGet]
    [ActionName("Vendors")]
    public IEnumerable<BusinessPartner> GetVendors() {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for vendors list");
        return data.GeneralData.GetVendors();
    }
}