using Core.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

public class AnyRoleRequirement : IAuthorizationRequirement {
    public AnyRoleRequirement(params RoleType[] roles) {
        Roles = roles;
    }

    public RoleType[] Roles { get; }
}