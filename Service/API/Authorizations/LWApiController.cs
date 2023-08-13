using System.Security.Claims;
using System.Web.Http;

namespace Service.API.Authorizations;

public class LWApiController : ApiController {
    protected int EmployeeID {
        get {
            int empID      = -1;
            var identity   = (ClaimsIdentity)User.Identity;
            var empIDClaim = identity.FindFirst("EmployeeID");
            if (empIDClaim == null) 
                return empID;
            int.TryParse(empIDClaim.Value, out empID);
            return empID;
        }
    }
}