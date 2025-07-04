using System;
using Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Service.Middlewares;

public class LicenseErrorHandlingMiddleware(RequestDelegate next, ILogger<LicenseErrorHandlingMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context) {
        try {
            await next(context);
        }
        catch (LicenseValidationException ex) {
            await HandleLicenseExceptionAsync(context, ex);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Unexpected error in license middleware");
            throw;
        }
    }

    private async Task HandleLicenseExceptionAsync(HttpContext context, LicenseValidationException ex) {
        context.Response.StatusCode  = 403;
        context.Response.ContentType = "application/json";

        var response = new {
            error         = "License validation failed",
            message       = ex.Message,
            licenseStatus = ex.LicenseStatus,
            timestamp     = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        logger.LogWarning("License validation exception: {Message}", ex.Message);
    }
}