using System.Collections;
using System.Collections.Generic;
using System.Web.Http;
using Service.Shared;
using Service.Shared.Data;

namespace Service.API.Authorizations;

[Authorize]
public class AuthorizationsController : LWApiController {
    private readonly DataConnector data;
    
    public AuthorizationsController() => data = Global.DataObject;

    [HttpGet]
    [ActionName("GetAuthorizations")]
    public IEnumerable<Role> GetAuthorizations() => Global.UserAuthorizations[EmployeeID];
}