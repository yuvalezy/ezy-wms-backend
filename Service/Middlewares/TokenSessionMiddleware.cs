using System;
using System.Threading.Tasks;
using Core;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Service.Middlewares;

public class TokenSessionMiddleware(RequestDelegate next, ISessionManager sessionManager, ISettings settings) {
#if DEBUG
    //workaround to make mock work for AI Browser
    public static string MockSessionToken { get; set; }
#endif
    public async Task InvokeAsync(HttpContext context) {
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

        // Grab the session token from the browser cookie or bearer header.
        string? cookieToken = context.Request.Cookies[Const.SessionCookieName];
        string? bearerToken = GetBearerToken(context);
#if DEBUG
        //workaround to make mock work for AI Browser
        cookieToken ??= MockSessionToken;
#endif
        if (string.IsNullOrWhiteSpace(cookieToken) && string.IsNullOrWhiteSpace(bearerToken)) {
            if (isLogoutEndpoint) {
                await next(context);
                return;
            }
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing session token.");
            return;
        }

        SessionInfo? sessionData = null;
        if (!string.IsNullOrWhiteSpace(cookieToken)) {
            sessionData = await sessionManager.GetSessionAsync(cookieToken);
            if (sessionData == null) {
                context.Response.Cookies.Delete(Const.SessionCookieName, GetCookieOptions(settings.SessionManagement.Cookie));
            }
        }

        if (sessionData == null &&
            !string.IsNullOrWhiteSpace(bearerToken) &&
            !string.Equals(cookieToken, bearerToken, StringComparison.Ordinal)) {
            sessionData = await sessionManager.GetSessionAsync(bearerToken);
        }

        if (sessionData == null) {
            // Allow logout to proceed even with invalid/expired session
            if (isLogoutEndpoint) {
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new {
                error = "session_expired",
                error_description = "Invalid or expired session."
            });
            return;
        }

        // Attach session data for downstream handlers
        context.Items["SessionData"] = sessionData;

        await next(context);
    }

    private static string? GetBearerToken(HttpContext context) {
        string? authorization = context.Request.Headers.Authorization;
        const string prefix = "Bearer ";
        if (!string.IsNullOrWhiteSpace(authorization) &&
            authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
            return authorization[prefix.Length..].Trim();
        }

        if (context.Request.Path.StartsWithSegments("/hubs") &&
            context.Request.Query.TryGetValue("access_token", out var accessToken) &&
            !string.IsNullOrWhiteSpace(accessToken)) {
            return accessToken;
        }

        return null;
    }

    private static CookieOptions GetCookieOptions(CookieSettings cookieSettings) {
        var sameSiteMode = cookieSettings.SameSite.ToLowerInvariant() switch {
            "strict" => SameSiteMode.Strict,
            "lax" => SameSiteMode.Lax,
            "none" => SameSiteMode.None,
            _ => SameSiteMode.Lax
        };

        var options = new CookieOptions {
            HttpOnly = cookieSettings.HttpOnly,
            Path     = "/",
            Secure   = cookieSettings.Secure,
            SameSite = sameSiteMode
        };

        if (!string.IsNullOrEmpty(cookieSettings.Domain)) {
            options.Domain = cookieSettings.Domain;
        }

        return options;
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
