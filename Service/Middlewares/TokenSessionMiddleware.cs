using System;
using System.Threading.Tasks;
using Core;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Service.Middlewares;

public class TokenSessionMiddleware(RequestDelegate next, ISessionManager sessionManager) {
#if DEBUG
    //workaround to make mock work for AI Browser
    public static string MockSessionToken { get; set; }
#endif
    public async Task InvokeAsync(HttpContext context) {
        // Skip session check for SignalR hubs (they use JWT token authentication)
        if (context.Request.Path.StartsWithSegments("/hubs")) {
            await next(context);
            return;
        }

        var endpoint                       = context.GetEndpoint();
        var authorizeAttribute             = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>();
        var requireAnyRoleAttribute        = endpoint?.Metadata.GetMetadata<RequireAnyRoleAttribute>();
        var requireRolePermissionAttribute = endpoint?.Metadata.GetMetadata<RequireRolePermissionAttribute>();
        var requireSuperUserAttribute      = endpoint?.Metadata.GetMetadata<RequireSuperUserAttribute>();
        bool requiresAuth = authorizeAttribute != null ||
                            requireAnyRoleAttribute != null ||
                            requireRolePermissionAttribute != null ||
                            requireSuperUserAttribute != null;

        // Skip session check on public endpoints
        if (!requiresAuth) {
            await next(context);
            return;
        }

        // Check if this is the logout endpoint
        bool isLogoutEndpoint = context.Request.Path.StartsWithSegments("/api/Authentication/logout", StringComparison.OrdinalIgnoreCase);

        // Grab the session token from the cookie
        string? sessionToken = context.Request.Cookies[Const.SessionCookieName];
#if DEBUG
        //workaround to make mock work for AI Browser
        sessionToken ??= MockSessionToken;
#endif
        if (string.IsNullOrWhiteSpace(sessionToken)) {
            if (isLogoutEndpoint) {
                await next(context);
                return;
            }
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing session token.");
            return;
        }

        // Validate in Redis (or your session store)
        var sessionData = await sessionManager.GetSessionAsync(sessionToken);
        if (sessionData == null) {
            // Allow logout to proceed even with invalid/expired session
            if (isLogoutEndpoint) {
                await next(context);
                return;
            }


            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or expired session.");
            return;
        }

        // Attach session data for downstream handlers
        context.Items["SessionData"] = sessionData;

        await next(context);
    }
}

public static class SessionManagerExtensions {
    public static SessionInfo GetSession(this HttpContext httpContext) {
        var info = httpContext.Items["SessionData"] as SessionInfo ??
                   throw new UnauthorizedAccessException("Session information not found");
        return info;
    }

    public static string GetWarehouse(this HttpContext httpContext) {
        var info = httpContext.Items["SessionData"] as SessionInfo ??
                   throw new UnauthorizedAccessException("Session information not found");
        return info.Warehouse;
    }
}