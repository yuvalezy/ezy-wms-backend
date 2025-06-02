using Core.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

public class RolePermissionRequirement(RoleType role) : IAuthorizationRequirement {
    public RoleType        Role          { get; } = role;
}