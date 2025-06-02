using System;
using System.Linq;
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
using Service.Configuration;
using Service.Middlewares;

var builder = WebApplication.CreateBuilder(args);

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
    foreach (var role in Enum.GetValues<Authorization>()) {
        options.AddPolicy($"{role}", policy =>
            policy.Requirements.Add(new RolePermissionRequirement(role)));
    }
});

services.AddSingleton<IAuthorizationHandler, RolePermissionHandler>();
services.AddSingleton<IAuthorizationHandler, SuperUserHandler>();

//dependency injection here
services.ConfigureServices(settings, builder.Configuration);

services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
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
if (builder.Environment.IsDevelopment()) {
    services.AddSwaggerGen();
}

services.AddLogging(config => {
    config.AddConsole();
    config.AddDebug();
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

app.Services.EnsureDatabaseCreated();
// // Apply migrations and seed data
// using (var scope = app.Services.CreateScope()) {
//     try {
//         var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
//
// #if RELEASE
//         var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
//         logger.LogInformation("Applying database migrations...");
//         await dbContext.Database.MigrateAsync();
//         logger.LogInformation("Migrations applied successfully");
// #endif
//
//         // Seed data
//         logger.LogInformation("Starting database seeding...");
//         // await DatabaseSeeder.SeedAsync(app.Services);
//         logger.LogInformation("Database seeding completed successfully");
//     }
//     catch (Exception ex) {
//         var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
//         logger.LogError(ex, "An error occurred while migrating or seeding the database");
//         throw; // Re-throw to stop application startup if seeding fails
//     }
// }

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.ConfigureDevelopmentMiddleware();
}
else {
    app.ConfigureProductionMiddleware();
}


app.UseAuthorization();
app.MapControllers();

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


// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using Microsoft.IdentityModel.Tokens;
// using Microsoft.OpenApi.Models;
// using Service.API.General;
// using System;
// using System.IO;
// using System.Text;
//
// namespace Service {
//     public class Program {
//         public static void Main(string[] args) {
//             var builder = CreateHostBuilder(args);
//             var host    = builder.Build();
//             host.Run();
//         }
//
//         public static IHostBuilder CreateHostBuilder(string[] args) =>
//             Host.CreateDefaultBuilder(args)
//                 .UseWindowsService()
//                 .ConfigureWebHostDefaults(webBuilder => {
//                     webBuilder.UseStartup<Startup>();
//                     webBuilder.ConfigureKestrel(serverOptions => {
//                         // Configure Kestrel to listen on the port from configuration
//                         var configuration = serverOptions.ApplicationServices.GetRequiredService<IConfiguration>();
//                         var port          = configuration.GetValue<int>("Service:Port", 5000);
//                         serverOptions.ListenAnyIP(port);
//                     });
//                 })
//                 .ConfigureAppConfiguration((hostingContext, config) => {
//                     var env = hostingContext.HostingEnvironment;
//
//                     // Add appsettings.json files
//                     config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
//                         .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
//
//                     // Add legacy app.config support if needed
//                     config.AddXmlFile("App.config", optional: true, reloadOnChange: true);
//
//                     config.AddEnvironmentVariables();
//
//                     if (args != null) {
//                         config.AddCommandLine(args);
//                     }
//                 })
//                 .ConfigureLogging((hostingContext, logging) => {
//                     logging.ClearProviders();
//                     logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
//                     logging.AddConsole();
//                     logging.AddDebug();
//                     logging.AddEventLog();
//                 });
//     }
//
//     public class Startup {
//         public Startup(IConfiguration configuration) {
//             Configuration = configuration;
//         }
//
//         public IConfiguration Configuration { get; }
//
//         public void ConfigureServices(IServiceCollection services) {
//             // Add CORS
//             services.AddCors(options => {
//                 options.AddPolicy("AllowAll",
//                     builder => {
//                         builder.AllowAnyOrigin()
//                             .AllowAnyMethod()
//                             .AllowAnyHeader();
//                     });
//             });
//
//             // Add authentication
//
//             // Add controllers
//             services.AddControllers()
//                 .AddNewtonsoftJson();
//
//             // Add Swagger
//             services.AddEndpointsApiExplorer();
//             services.AddSwaggerGen(c => {
//                 c.SwaggerDoc("v1", new OpenApiInfo { Title = "LW Service API", Version = "v1" });
//
//                 // Add JWT Authentication
//                 c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
//                     Description = "JWT Authorization header using the Bearer scheme",
//                     Name        = "Authorization",
//                     In          = ParameterLocation.Header,
//                     Type        = SecuritySchemeType.ApiKey,
//                     Scheme      = "Bearer"
//                 });
//
//                 c.AddSecurityRequirement(new OpenApiSecurityRequirement {
//                     {
//                         new OpenApiSecurityScheme {
//                             Reference = new OpenApiReference {
//                                 Type = ReferenceType.SecurityScheme,
//                                 Id   = "Bearer"
//                             }
//                         },
//                         Array.Empty<string>()
//                     }
//                 });
//             });
//
//             // Register services
//             services.AddSingleton<IGlobalService, GlobalService>();
//             services.AddSingleton<IJwtAuthenticationService, JwtAuthenticationService>();
//             services.AddSingleton<Data>();
//             services.AddSingleton<CountingData>();
//             services.AddSingleton<GeneralData>();
//             services.AddSingleton<GoodsReceiptData>();
//             services.AddSingleton<PickingData>();
//             services.AddSingleton<TransferData>();
//
//             // Add hosted service for background tasks if needed
//             // services.AddHostedService<BackgroundService>();
//         }
//
//         public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
//             if (env.IsDevelopment()) {
//                 app.UseDeveloperExceptionPage();
//                 app.UseSwagger();
//                 app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LW Service API v1"));
//             }
//
//             app.UseStaticFiles();
//             app.UseRouting();
//             app.UseCors("AllowAll");
//             app.UseAuthentication();
//             app.UseAuthorization();
//
//             app.UseEndpoints(endpoints => {
//                 endpoints.MapControllers();
//
//                 // Map root to index.html
//                 endpoints.MapGet("/", async context => {
//                     context.Response.ContentType = "text/html";
//                     await context.Response.SendFileAsync(Path.Combine(env.WebRootPath, "index.html"));
//                 });
//             });
//         }
//     }
// }