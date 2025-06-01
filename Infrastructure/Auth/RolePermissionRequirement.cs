using Core.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

public class RolePermissionRequirement(Authorization role) : IAuthorizationRequirement {
    public Authorization        Role          { get; } = role;
}