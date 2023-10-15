using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http.Cors;
using Microsoft.Owin.Security.OAuth;
using Service.Shared.Utils;

namespace Service.API;

[EnableCors("*", "*", "*")]
public class ApplicationAuthProvider : OAuthAuthorizationServerProvider {
    public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context) {
        context.Validated();
    }

    public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context) {
        bool   valid    = false, isValidBranch = false;
        string userName = context.UserName;
        string password = context.Password;
        int    empID    = -1;
        if (!string.IsNullOrWhiteSpace(context.UserName))
            valid = Data.ValidateAccess(context.UserName, out empID, out isValidBranch);

        if (valid) {
            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim("Username", userName));
            identity.AddClaim(new Claim("EmployeeID", empID.ToString()));

            //set the expiration token to midnight
            var now            = DateTime.Now;
            var midnight       = now.Date.AddDays(1);
            var timeToMidnight = midnight - now;
            context.Options.AccessTokenExpireTimeSpan = timeToMidnight;

            context.Validated(identity);
        }
        else {
            if (empID > 0 && !isValidBranch) {
                context.SetError("invalid_grant", "The user does not have a valid branch defined.");
                return;
            }

            context.SetError("invalid_grant", "The user name or password is incorrect.");
        }
    }



    private static bool IsLocalIP(string ipAddress) {
        try {
            return ipAddress is "127.0.0.1" or "::1" || Dns.GetHostAddresses(Dns.GetHostName()).Any(v => v.ToString() == ipAddress);
        }
        catch {
            return false;
        }
    }
}