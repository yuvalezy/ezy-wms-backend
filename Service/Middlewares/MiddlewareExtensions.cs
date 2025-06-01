using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Service.Middlewares;
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