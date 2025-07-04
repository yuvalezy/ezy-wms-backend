using System;
using System.Collections.Generic;
using System.Linq;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Service.Middlewares;

public class LicenseValidationMiddleware(RequestDelegate next, ILogger<LicenseValidationMiddleware> logger) {
    private readonly HashSet<string> allowedEndpoints = new(StringComparer.OrdinalIgnoreCase) {
        "/api/authentication/login",
        "/api/authentication/logout",
        "/api/authentication/companyname",
        "/api/users",
        "/api/authorization-groups",
        "/api/device",
        "/api/license/status",
        "/swagger",
        "/health"
    };

    public async Task InvokeAsync(HttpContext context) {
        string? path = context.Request.Path.Value;

        // Skip validation for allowed endpoints
        if (IsAllowedEndpoint(path)) {
            await next(context);
            return;
        }

        // Check if device is activated
        string? deviceUuid = GetDeviceUuid(context);
        if (string.IsNullOrEmpty(deviceUuid)) {
            await WriteErrorResponse(context, 400, "Device UUID not provided");
            return;
        }

        // Validate license for this device
        using var scope                    = context.RequestServices.CreateScope();
        var       licenseValidationService = scope.ServiceProvider.GetRequiredService<ILicenseValidationService>();

        bool isValidDevice = await licenseValidationService.ValidateDeviceAccessAsync(deviceUuid);
        if (!isValidDevice) {
            await WriteErrorResponse(context, 403, "Device not active or license invalid");
            return;
        }

        // Check system-wide access
        bool systemAccess = await licenseValidationService.ValidateSystemAccessAsync();
        if (!systemAccess) {
            await WriteErrorResponse(context, 403, "System access denied due to license status");
            return;
        }

        await next(context);
    }

    private bool IsAllowedEndpoint(string? path) {
        if (string.IsNullOrEmpty(path))
            return false;

        return allowedEndpoints.Any(endpoint =>
            path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private string? GetDeviceUuid(HttpContext context) {
        // Try to get from header first
        string? deviceUuid = context.Request.Headers["X-Device-UUID"].FirstOrDefault();

        // If not in header, try to get from session
        if (!string.IsNullOrEmpty(deviceUuid))
            return deviceUuid;
        var sessionInfo = context.GetSession();
        deviceUuid = sessionInfo.DeviceUuid;

        return deviceUuid;
    }

    private async Task WriteErrorResponse(HttpContext context, int statusCode, string message) {
        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/json";

        var response = new {
            error     = message,
            timestamp = DateTime.UtcNow,
            path      = context.Request.Path.Value
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        logger.LogWarning("License validation failed for path {Path}: {Message}",
            context.Request.Path.Value, message);
    }
}