using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth;

public class SuperUserHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<SuperUserRequirement> {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SuperUserRequirement requirement) {
        // Get the HttpContext
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) {
            return Task.CompletedTask;
        }

        // Get the SessionData from HttpContext.Items
        if (!httpContext.Items.TryGetValue("SessionData", out object? sessionDataObj) || sessionDataObj is not SessionInfo sessionInfo) {
            return Task.CompletedTask;
        }

        // Check if the user is a super user
        if (sessionInfo.SuperUser) {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}