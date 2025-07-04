using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Core;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Infrastructure.Auth;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Service.Configuration;
using Service.Middlewares;
using Service.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Configure for Windows Service
builder.Host.UseWindowsService();

var settings = new Settings();
builder.Configuration.Bind(settings);


var services = builder.Services;
services.AddSingleton<ISettings>(settings);
services.AddRouting(options => options.LowercaseUrls = true);

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = settings.Jwt.Issuer,
            ValidAudience            = settings.Jwt.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Jwt.Key))
        };
    });

services.AddAuthorization(options => {
    // Register the super user policy
    options.AddPolicy("SuperUserOnly", policy =>
        policy.Requirements.Add(new SuperUserRequirement()));

    // Register policies for each role and access level combination
    foreach (var role in Enum.GetValues<RoleType>()) {
        options.AddPolicy($"{role}", policy =>
            policy.Requirements.Add(new RolePermissionRequirement(role)));
    }
});

services.AddSingleton<IAuthorizationHandler, RolePermissionHandler>();
services.AddSingleton<IAuthorizationHandler, SuperUserHandler>();
services.AddSingleton<IAuthorizationHandler, AnyRoleHandler>();
services.AddSingleton<IAuthorizationPolicyProvider, AnyRolePolicyProvider>();

//dependency injection here
services.ConfigureServices(settings, builder.Configuration);

services.AddControllers()
    .AddJsonOptions(options => { 
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new Service.Models.UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new Service.Models.NullableUtcDateTimeConverter());
    });
if (settings.SessionManagement.Type == SessionManagementType.Redis) {
    // Add Redis distributed cache
    services.AddStackExchangeRedisCache(options => {
        options.Configuration = $"{settings.SessionManagement.Redis.Host ?? "localhost"}:{settings.SessionManagement.Redis.Port ?? 6379}";
        options.InstanceName  = "EzyWms:";
    });
}
else {
    // Add in-memory distributed cache when not using Redis
    services.AddDistributedMemoryCache();
}

// Read allowed origins from appsettings.json
string[]? allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

// Add CORS policy
if (allowedOrigins is { Length: > 0 }) {
    services.AddCors(options => {
        options.AddPolicy("AllowSpecificOrigins", policy => {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // Add this if you're using credentials (e.g., cookies, authorization headers)
        });
    });
}

services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title = "EzyWMS API",
        Version = "v1",
        Description = "ASP.NET Core Web API for EzyWMS warehouse management system",
        Contact = new OpenApiContact {
            Name = "EzyWMS Support",
            Email = "support@ezywms.com"
        }
    });

    // Configure JWT Bearer authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Configure Device UUID header requirement
    c.AddSecurityDefinition("DeviceUUID", new OpenApiSecurityScheme {
        Description = "Device UUID header required for most endpoints (except authentication, users, device management, license status, swagger, and health). Example: \"X-Device-UUID: {device-uuid}\"",
        Name = "X-Device-UUID",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "DeviceUUID"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });

    // Include XML documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) {
        c.IncludeXmlComments(xmlPath);
    }

    // Include Core assembly XML documentation
    var coreXmlFile = "Core.xml";
    var coreXmlPath = Path.Combine(AppContext.BaseDirectory, coreXmlFile);
    if (File.Exists(coreXmlPath)) {
        c.IncludeXmlComments(coreXmlPath);
    }

    // Custom schema filter for enums
    c.SchemaFilter<EnumSchemaFilter>();
    
    // Operation filter for role-based authorization documentation
    c.OperationFilter<AuthorizeOperationFilter>();
    
    // Operation filter for Device UUID requirements
    c.OperationFilter<DeviceUuidOperationFilter>();
});

services.AddLogging(config => {
    config.AddConsole();
    config.AddDebug();
    config.AddEventLog(); // Add Windows Event Log for service
});
services.ConfigureCorsPolicies();

// Add Forwarded Headers conditionally
if (!builder.Environment.IsDevelopment()) {
    services.Configure<ForwardedHeadersOptions>(options => {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear(); // Clear known networks to trust all
        options.KnownProxies.Clear();  // Clear known proxies to trust all
    });
}

services.AddRateLimiter(opts => {
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Strict policy for public authentication endpoints
    opts.AddPolicy("PublicAuthPolicy", context => {
        string ip = context.Connection.RemoteIpAddress?.ToString()
                    ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                    ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions {
                PermitLimit       = 10,
                Window            = TimeSpan.FromMinutes(1),
                AutoReplenishment = true
            });
    });

    // Less strict policy for public plan info
    opts.AddPolicy("PublicPlanPolicy", context => {
        string ip = context.Connection.RemoteIpAddress?.ToString()
                    ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                    ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions {
                PermitLimit       = 30,
                Window            = TimeSpan.FromMinutes(1),
                AutoReplenishment = true
            });
    });

    // Set the authenticated policy as the default/global policy
    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => {
        string key = context.Request.Cookies[Const.SessionCookieName] ??
                     (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");


        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions {
                PermitLimit       = 100,
                Window            = TimeSpan.FromMinutes(1),
                AutoReplenishment = true
            });
    });
});
// In your services configuration
services.Configure<IISServerOptions>(options => {
    options.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB in bytes
});

// And/or
services.Configure<KestrelServerOptions>(options => {
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB in bytes
});
var app = builder.Build();
if (allowedOrigins is { Length: > 0 }) {
    app.UseCors("AllowSpecificOrigins");
}

app.UseAuthentication();

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

// Configure static files for React app
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
app.UseRouting();

if (app.Environment.IsDevelopment()) {
    app.ConfigureDevelopmentMiddleware();
}
else {
    app.ConfigureProductionMiddleware();
}


app.UseAuthorization();
app.MapControllers();

// Fallback route for React app (SPA)
app.MapFallbackToFile("index.html");

#if DEBUG
app.Use(async (ctx, next) => {
    // Minimal policy that keeps most protections
    const string csp =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-eval'; " +   //  <— allows eval / new Function()
        "style-src  'self' 'unsafe-inline'; " + //  Nitro injects inline <style>
        "img-src    'self' data:; " +
        "connect-src 'self' https://your-api-host.com;";

    ctx.Response.Headers["Content-Security-Policy"] = csp;
    await next();
});
#endif

app.Run();

namespace WebApi {
    public class Program;
}