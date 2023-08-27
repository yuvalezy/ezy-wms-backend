using System;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.Models;
using Service.Shared;

namespace Service.API.General;

[Authorize]
public class GeneralController : LWApiController {
    private readonly Data data = new Data();

    [HttpGet]
    [ActionName("UserInfo")]
    public UserInfo GetUserInfo() {
        string name = data.GeneralData.GetEmployeeName(EmployeeID);
        return new UserInfo {
            ID             = EmployeeID,
            Name           = name,
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