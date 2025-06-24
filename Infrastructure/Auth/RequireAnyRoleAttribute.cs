using Core.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireAnyRoleAttribute : AuthorizeAttribute {
    public RequireAnyRoleAttribute(params RoleType[] roles) {
        Roles = roles;
        // Create a unique policy name for this combination of roles
        Policy = $"AnyRole_{string.Join("_", roles.Select(r => r.ToString()))}";
    }

    public RoleType[] Roles { get; }
}