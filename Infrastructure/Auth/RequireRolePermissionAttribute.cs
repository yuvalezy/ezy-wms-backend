using Core.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRolePermissionAttribute : AuthorizeAttribute {
    public RequireRolePermissionAttribute(RoleType role) {
        Role   = role;
        Policy = $"{role}";
    }

    public RoleType        Role          { get; }
}