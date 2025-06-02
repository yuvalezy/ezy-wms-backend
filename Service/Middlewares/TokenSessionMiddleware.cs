using System;
using System.Threading.Tasks;
using Core;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Service.Middlewares;

public class TokenSessionMiddleware(RequestDelegate next, ISessionManager sessionManager) {
#if DEBUG
    //workaround to make mock work for AI Browser
    public static string MockSessionToken { get; set; }
#endif
    public async Task InvokeAsync(HttpContext context) {
        var  endpoint     = context.GetEndpoint();
        bool requiresAuth = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>() != null;

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