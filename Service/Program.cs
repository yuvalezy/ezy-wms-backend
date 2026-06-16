using System.Runtime.InteropServices;
using Core.Interfaces;
using Core.Models.Settings;
using Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Service.Configuration;
using Service.Extensions;
using Service.Services;
using Service.Testing;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load YAML configuration files (only present until migrated/archived). All
// optional: after migration the live config/ folder is archived and these are
// served from the database provider added below.
builder.Configuration.AddYamlFile("config/Configurations.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/PickingPostProcessing.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/ExternalCommands.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/PickingDetails.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/CustomFields.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/Item.yaml", optional: true, reloadOnChange: true);

// Database-backed configuration. Registered LAST so that, once the database holds
// configuration, it takes precedence over any leftover YAML. Tolerant of a missing
// table on a fresh database (yields no keys until the seed/migration step runs).
builder.Configuration.AddDatabaseConfiguration(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    builder.Configuration["Licensing:EncryptionKey"]);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Check for test mode arguments
if (args.Length > 0 && args[0] == "--test-sbo") {
    await SboConnectionTester.RunTest(builder.Configuration);
    return;
}

if (args.Length > 0 && args[0] == "--test-email") {
    await EmailTester.RunTest(builder.Configuration);
    return;
}

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
// Configure for Windows Service
    builder.Host.UseWindowsService();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
    builder.Host.UseSystemd();
}

// Boot-time snapshot used by startup/DI configuration below.
var settings = new Settings();
builder.Configuration.Bind(settings);

// Configure services
var services = builder.Services;

// Hot-reloadable settings: bind Settings through options so runtime consumers
// observe configuration changes (DB edits + reload) without a restart. Startup
// code keeps using the `settings` snapshot above.
services.Configure<Settings>(builder.Configuration);
services.AddSingleton<ISettings, ReloadableSettings>();
services.AddRouting(options => options.LowercaseUrls = true);

// Add authentication and authorization
services.AddAuthenticationAndAuthorization(builder.Configuration);

// Configure application services
services.ConfigureServices(settings, builder.Configuration, builder.Environment);
services.AddJsonConfiguration();
services.AddSessionManagement(settings);
services.AddCustomCors(builder.Configuration);
services.AddSwaggerServices(builder.Environment);
services.AddCustomLogging(builder.Environment);
services.AddServerConfiguration(builder.Environment);
services.AddCustomRateLimiting();

// Register Uptime Kuma heartbeat service
services.AddHostedService<UptimeKumaHeartbeatService>();

// Get CORS origins for later use
string[]? allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
// Build and configure the application
var app = builder.Build();

// Configure request pipeline
app.ConfigureCorsIfConfigured(allowedOrigins);
app.UseRouting();
app.UseAuthentication();
app.ConfigureDatabase()
.ConfigureStaticFiles()
.ConfigureRequestPipeline()
.ConfigureDevelopmentSecurity();

// Migrate/seed configuration from files into the database, verify, and archive.
// Must run after ConfigureDatabase() (tables exist) and before configuration use.
await app.InitializeConfigurationAsync();

await app.TestConfigurations(settings);

app.Run();

namespace WebApi {
    public class Program;
}
