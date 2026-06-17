using System;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Service.Middlewares;

namespace Service.Extensions;

public static class WebApplicationExtensions {
    // Static file caching for the SPA:
    //  - Content-hashed build assets (Vite emits e.g. /assets/index-D2TZ1-0b.js) never change for a
    //    given hash, so they can be cached forever ("immutable"). A new deploy produces new filenames.
    //  - Everything else - above all index.html - MUST NOT be cached, otherwise the browser keeps a
    //    stale entry document that points at deleted JS chunks. That is what caused the inactive-device
    //    login loop: clients never picked up the new build and had no way to clear their cache in prod.
    private static readonly Action<StaticFileResponseContext> SpaCacheHeaders = ctx => {
        var path = ctx.File.Name;
        var dir = ctx.Context.Request.Path.Value ?? string.Empty;
        var isHashedAsset = dir.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase);

        ctx.Context.Response.Headers[HeaderNames.CacheControl] = isHashedAsset
            ? "public, max-age=31536000, immutable"
            : "no-cache, no-store, must-revalidate";

        if (!isHashedAsset) {
            ctx.Context.Response.Headers[HeaderNames.Pragma] = "no-cache";
            ctx.Context.Response.Headers[HeaderNames.Expires] = "0";
        }
    };

    private static StaticFileOptions SpaStaticFileOptions => new() { OnPrepareResponse = SpaCacheHeaders };

    public static WebApplication ConfigureStaticFiles(this WebApplication app) {
        // Configure static files for React app
        app.UseDefaultFiles();
        app.UseStaticFiles(SpaStaticFileOptions);
        return app;
    }

    public static WebApplication ConfigureCorsIfConfigured(this WebApplication app, string[]? allowedOrigins) {
        if (allowedOrigins is { Length: > 0 }) {
            app.UseCors("AllowSpecificOrigins");
        }
        return app;
    }

    public static WebApplication ConfigureDatabase(this WebApplication app) {
        // Initialize database with error handling
        try {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting database initialization...");
            app.Services.EnsureDatabaseCreated();
            logger.LogInformation("Database initialization completed.");
        }
        catch (Exception ex) {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Database initialization failed: {Message}", ex.Message);
            throw;
        }
        return app;
    }

    public static WebApplication ConfigureRequestPipeline(this WebApplication app) {
        if (app.Environment.IsDevelopment()) {
            app.ConfigureDevelopmentMiddleware();
        }
        else {
            app.ConfigureProductionMiddleware();
        }

        app.UseAuthorization();
        app.MapControllers();

        // Map SignalR hubs
        app.MapHub<NotificationHub>("/hubs/notifications");

        // Fallback route for React app (SPA). Use the same cache policy so the fallback-served
        // index.html is also no-cache - otherwise deep links bypass the no-cache header above.
        app.MapFallbackToFile("index.html", SpaStaticFileOptions);

        return app;
    }

    public static WebApplication ConfigureDevelopmentSecurity(this WebApplication app) {
        // Add Content Security Policy for development
        if (app.Environment.IsDevelopment()) {
            app.Use(async (ctx, next) => {
                // Get the host dynamically to support WebSocket connections
                var host = ctx.Request.Host.Host;
                var port = ctx.Request.Host.Port;
                var wsOrigin = port.HasValue ? $"ws://{host}:{port} wss://{host}:{port}" : $"ws://{host} wss://{host}";
                var httpOrigin = port.HasValue ? $"http://{host}:{port} https://{host}:{port}" : $"http://{host} https://{host}";

                // Minimal policy that keeps most protections and allows WebSocket for SignalR
                var csp =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-eval'; " +   //  <— allows eval / new Function()
                    "style-src  'self' 'unsafe-inline'; " + //  Nitro injects inline <style>
                    "img-src    'self' data:; " +
                    $"connect-src 'self' {wsOrigin} {httpOrigin};";

                ctx.Response.Headers["Content-Security-Policy"] = csp;
                await next();
            });
        }
        return app;
    }
}