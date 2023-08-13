using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http.Cors;
using Microsoft.Owin.Security.OAuth;

namespace Service.API; 

[EnableCors("*", "*", "*")]
public class ApplicationAuthProvider : OAuthAuthorizationServerProvider {
    public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context) {
        context.Validated();
    }

    public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context) {
        bool   valid;
        string userName = context.UserName;
        string password = context.Password;
        if (!string.IsNullOrWhiteSpace(context.UserName)) {
            valid = Global.RestAPISettings.ValidateAccess(context.UserName, context.Password);
        }
        else {
            valid    = IsLocalIP(context.Request.RemoteIpAddress);
            userName = "localhost";
            password = "localhost";
        }

        if (valid) {
            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim("Username", userName));
            identity.AddClaim(new Claim("Password", password));
            context.Validated(identity);
        }
        else {
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