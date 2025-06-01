using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth;

public class RolePermissionHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<RolePermissionRequirement> {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolePermissionRequirement requirement) {
        // Get the HttpContext
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) {
            return Task.CompletedTask;
        }

        // Get the SessionData from HttpContext.Items
        if (!httpContext.Items.TryGetValue("SessionData", out object? sessionDataObj) || sessionDataObj is not SessionInfo sessionInfo) {
            return Task.CompletedTask;
        }

        // Super users have access to everything
        if (sessionInfo.SuperUser) {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if the user has the required permission
        if (sessionInfo.Authorizations.Any(p => p == requirement.Role)) {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}