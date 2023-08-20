using System.Collections;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.Models;
using Service.Shared;
using Service.Shared.Data;

namespace Service.API.Authorizations;

[Authorize]
public class GeneralController : LWApiController {
    private readonly DataConnector data;

    public GeneralController() => data = Global.DataObject;

    [HttpGet]
    [ActionName("UserInfo")]
    public UserInfo GetUserInfo() {
        string query = $"""
                        select COALESCE("firstName", 'NO_NAME') {QueryHelper.Concat} ' ' {QueryHelper.Concat} COALESCE("lastName", 'NO_LAST_NAME')
                        from OHEM where "empID" = {EmployeeID}
                        """;
        string name = data.GetValue<string>(query);
        return new UserInfo {
            ID    = EmployeeID,
            Name  = name,
            Roles = Global.UserAuthorizations[EmployeeID]
        };
    }
}