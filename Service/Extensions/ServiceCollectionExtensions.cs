using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Core;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Infrastructure.Auth;
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
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Service.Configuration;
using Service.Swagger;

namespace Service.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration) {
        // Configure JWT settings using IOptions pattern with validation
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        
        // Add options validation
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateDataAnnotations()
            .Validate(settings => !string.IsNullOrEmpty(settings.Key), "JWT Key is required")
            .Validate(settings => !string.IsNullOrEmpty(settings.Issuer), "JWT Issuer is required") 
            .Validate(settings => !string.IsNullOrEmpty(settings.Audience), "JWT Audience is required")
            .Validate(settings => settings.ExpiresInMinutes > 0, "JWT ExpiresInMinutes must be greater than 0");
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
                var jwtSection = configuration.GetSection("Jwt");
                var jwtKey = jwtSection["Key"];
                var jwtIssuer = jwtSection["Issuer"];
                var jwtAudience = jwtSection["Audience"];
                
                if (string.IsNullOrEmpty(jwtKey)) {
                    throw new InvalidOperationException("JWT Key is not configured in appsettings.json");
                }
                if (string.IsNullOrEmpty(jwtIssuer)) {
                    throw new InvalidOperationException("JWT Issuer is not configured in appsettings.json");
                }
                if (string.IsNullOrEmpty(jwtAudience)) {
                    throw new InvalidOperationException("JWT Audience is not configured in appsettings.json");
                }
                
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtIssuer,
                    ValidAudience            = jwtAudience,
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };

                // Configure SignalR to accept JWT token from query string
                options.Events = new JwtBearerEvents {
                    OnMessageReceived = context => {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        // If the request is for our SignalR hub and has a token
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs")) {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
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

        return services;
    }

    public static IServiceCollection AddSwaggerServices(this IServiceCollection services, IHostEnvironment environment) {
        if (!environment.IsDevelopment()) return services;

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
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
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

        return services;
    }

    public static IServiceCollection AddCustomLogging(this IServiceCollection services, IHostEnvironment environment) {
        services.AddLogging(config => {
            if (environment.IsDevelopment()) {
                config.AddConsole();
                config.AddDebug();
            }
            // Add Windows Event Log only on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                config.AddEventLog();
            }
        });

        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration) {
        string[]? allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (allowedOrigins is { Length: > 0 }) {
            services.AddCors(options => {
                options.AddPolicy("AllowSpecificOrigins", policy => {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        services.ConfigureCorsPolicies();
        return services;
    }

    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services) {
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

        return services;
    }

    public static IServiceCollection AddServerConfiguration(this IServiceCollection services, IHostEnvironment environment) {
        // Add Forwarded Headers conditionally
        if (!environment.IsDevelopment()) {
            services.Configure<ForwardedHeadersOptions>(options => {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear(); // Clear known networks to trust all
                options.KnownProxies.Clear();  // Clear known proxies to trust all
            });
        }

        // Configure server options - IIS only available on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            services.AddWindowsServerConfiguration();
        }

        services.Configure<KestrelServerOptions>(options => {
            options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB in bytes
        });

        return services;
    }

    public static IServiceCollection AddWindowsServerConfiguration(this IServiceCollection services) {
        // Use reflection to avoid type loading issues on non-Windows platforms
        try {
            var iisOptionsType = Type.GetType("Microsoft.AspNetCore.Builder.IISServerOptions, Microsoft.AspNetCore.Server.IIS");
            if (iisOptionsType != null) {
                var configureMethod = typeof(OptionsConfigurationServiceCollectionExtensions)
                    .GetMethod("Configure", new[] { typeof(IServiceCollection), typeof(Action<>).MakeGenericType(iisOptionsType) });

                if (configureMethod != null) {
                    var configureGeneric = configureMethod.MakeGenericMethod(iisOptionsType);
                    var actionType = typeof(Action<>).MakeGenericType(iisOptionsType);

                    // Create action delegate
                    var parameter = Expression.Parameter(iisOptionsType, "options");
                    var property = Expression.Property(parameter, "MaxRequestBodySize");
                    var assignment = Expression.Assign(property, Expression.Constant(10 * 1024 * 1024));
                    var lambda = Expression.Lambda(actionType, assignment, parameter);
                    var action = lambda.Compile();

                    configureGeneric.Invoke(null, new object[] { services, action });
                }
            }
        } catch {
            // Silently ignore errors configuring IIS options on non-Windows platforms
        }

        return services;
    }

    public static IServiceCollection AddSessionManagement(this IServiceCollection services, ISettings settings) {
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

        return services;
    }

    public static IServiceCollection AddJsonConfiguration(this IServiceCollection services) {
        services.AddControllers()
            .AddJsonOptions(options => { 
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new Service.Models.UtcDateTimeConverter());
                options.JsonSerializerOptions.Converters.Add(new Service.Models.NullableUtcDateTimeConverter());
            });

        return services;
    }
}