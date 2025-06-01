using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Service;
public static class MiddlewareExtensions {
    public static IApplicationBuilder ConfigureDevelopmentMiddleware(this IApplicationBuilder app) {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseMiddleware<RequestLoggingMiddleware>();
        ConfigureCommonMiddlewares(app);

        
        return app;
    }

    public static IApplicationBuilder ConfigureProductionMiddleware(this IApplicationBuilder app) {
        app.UseForwardedHeaders();
        app.UseRateLimiter();
        app.UseHttpsRedirection();
        ConfigureCommonMiddlewares(app);
        return app;
    }

    private static void ConfigureCommonMiddlewares(IApplicationBuilder app) {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<TokenSessionMiddleware>();

        // Security Headers Middleware
        app.Use(async (context, next) => {
            // Use Add instead of Append to prevent duplicate headers
#pragma warning disable ASP0019
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Content-Security-Policy",
                "default-src 'self'; " +
                "img-src 'self' data: https:; " +    // If you need to load images
                "style-src 'self' 'unsafe-inline'"); // If you use inline styles
#pragma warning restore ASP0019

            await next();
        });

        // Request Size Limiting Middleware
        app.Use(async (context, next) => {
            long? contentLength = context.Request.ContentLength;
            if (contentLength is > 10 * 1024 * 1024) // 10MB limit
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsJsonAsync(new {
                    error = "Request size exceeds maximum allowed size of 10MB"
                });
                return;
            }

            await next();
        });
    }
}
public class RequestLoggingMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext context) {
        // Log request details
        Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");

        // Log query string
        if (context.Request.QueryString.HasValue) {
            Console.WriteLine($"Query String: {context.Request.QueryString.Value}");
        }

        // Log request body if it exists and is of a type that can have a body
        if (context.Request.ContentLength > 0 &&
            (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put || context.Request.Method == HttpMethods.Patch)) {
            context.Request.EnableBuffering(); // Allows us to read the request body without consuming it

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            string    body   = await reader.ReadToEndAsync();
            Console.WriteLine($"Body: {body}");

            // Reset the request body stream position so it can be read by the next middleware or controller
            context.Request.Body.Position = 0;
        }

        // Call the next middleware in the pipeline
        await next(context);
    }
}
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
public class TokenSessionMiddleware(RequestDelegate next, ISessionManager sessionManager) {
#if DEBUG
    //workaround to make mock work for AI Browser
    public static string MockSessionToken { get; set; }
#endif
    public async Task InvokeAsync(HttpContext context) {
        var  endpoint     = context.GetEndpoint();
        bool requiresAuth = endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>() != null;

        // Skip session check on public endpoints
        if (!requiresAuth) {
            await next(context);
            return;
        }

        // Check if this is the logout endpoint
        bool isLogoutEndpoint = context.Request.Path.StartsWithSegments("/api/PublicAccount/logout", StringComparison.OrdinalIgnoreCase);

        // Grab the session token from the cookie
        string? sessionToken = context.Request.Cookies[SessionInfo.SessionCookieName];
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
public interface ISessionManager {
    Task               SetValueAsync(string   token, string sessionData, TimeSpan expiration);
    Task<string?>      GetStringAsync(string  token);
    Task<SessionInfo?> GetSessionAsync(string token);
    Task               RemoveAsync(string     token);
}
