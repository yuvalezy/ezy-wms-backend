using Core.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRolePermissionAttribute : AuthorizeAttribute {
    public RequireRolePermissionAttribute(Authorization role) {
        Role   = role;
        Policy = $"{role}";
    }

    public Authorization        Role          { get; }
}