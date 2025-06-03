using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth;

public class AnyRoleHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<AnyRoleRequirement> {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AnyRoleRequirement requirement) {
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

        // Check if the user has any of the required roles
        if (requirement.Roles.Any(role => sessionInfo.Roles.Contains(role))) {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}