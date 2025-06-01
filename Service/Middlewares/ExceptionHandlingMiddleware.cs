using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Service.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context) {
        try {
            await next(context);
        }
        catch (Exception ex) {
            logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception) {
        var code = exception switch {
            UnauthorizedAccessException                     => HttpStatusCode.Unauthorized,
            System.Collections.Generic.KeyNotFoundException => HttpStatusCode.NotFound,
            FileNotFoundException                           => HttpStatusCode.NotFound,
            ValidationException                             => HttpStatusCode.BadRequest,
            ArgumentOutOfRangeException                     => HttpStatusCode.BadRequest,
            ArgumentException                               => HttpStatusCode.BadRequest,
            InvalidOperationException                       => HttpStatusCode.BadRequest,
            _                                               => HttpStatusCode.InternalServerError
        };

        string result = JsonSerializer.Serialize(new { error = exception.Message });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)code;
        return context.Response.WriteAsync(result);
    }
}