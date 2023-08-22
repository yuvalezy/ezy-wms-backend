using System.Web.Http;
using Service.API.Models;

namespace Service.API.General;

[Authorize]
public class GeneralController : LWApiController {
    private readonly GeneralData data = new();

    [HttpGet]
    [ActionName("UserInfo")]
    public UserInfo GetUserInfo() {
        string name = data.GetEmployeeName(EmployeeID);
        return new UserInfo {
            ID    = EmployeeID,
            Name  = name,
            Roles = Global.UserAuthorizations[EmployeeID]
        };
    }
}