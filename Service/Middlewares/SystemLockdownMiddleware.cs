using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Service.Middlewares;

/// <summary>
/// While the system is not ready (SAP/SBO not configured or unreachable), blocks
/// business API endpoints with 503 so stock operations cannot run against an
/// unverified backend. Auth, configuration, system status, health, swagger and the
/// SPA shell stay reachable so a superuser can recover from the configuration UI.
/// </summary>
public class SystemLockdownMiddleware(RequestDelegate next, ISystemStatusService status) {
    private static readonly string[] AllowedApiPrefixes = {
        "/api/system",
        "/api/health",
        "/api/authentication",
        "/api/configuration",
        "/swagger"
    };

    public async Task InvokeAsync(HttpContext context) {
        if (status.Current.Ready) {
            await next(context);
            return;
        }

        string path = context.Request.Path.Value ?? string.Empty;
        bool isApi = path.StartsWith("/api", StringComparison.OrdinalIgnoreCase);
        bool isAllowed = AllowedApiPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        // Block only business API calls; let the SPA shell and recovery endpoints through.
        if (isApi && !isAllowed) {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new {
                error = "system_locked_down",
                error_description = status.Current.Detail ?? "The system is not configured."
            });
            return;
        }

        await next(context);
    }
}
