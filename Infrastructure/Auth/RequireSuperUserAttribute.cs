using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

public class RequireSuperUserAttribute : AuthorizeAttribute {
    public RequireSuperUserAttribute() => Policy = "SuperUserOnly";
}