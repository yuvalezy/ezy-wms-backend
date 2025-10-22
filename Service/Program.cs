using System.Runtime.InteropServices;
using Core.Interfaces;
using Core.Models.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Service.Configuration;
using Service.Extensions;
using Service.Testing;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

// Load YAML configuration files before binding
builder.Configuration.AddYamlFile("config/Configurations.yaml", optional: false, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/PickingPostProcessing.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/ExternalCommands.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/PickingDetails.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/CustomFields.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/Item.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile("config/Package.yaml", optional: true, reloadOnChange: true);

var settings = new Settings();
builder.Configuration.Bind(settings);

// Log configurations that will be moved to YAML files
settings.LogYamlMigrationCandidates();

// Configure services
var services = builder.Services;
services.AddSingleton<ISettings>(settings);
services.AddRouting(options => options.LowercaseUrls = true);

// Add authentication and authorization
services.AddAuthenticationAndAuthorization(builder.Configuration);

// Configure application services
services.ConfigureServices(settings, builder.Configuration);
services.AddJsonConfiguration();
services.AddSessionManagement(settings);
services.AddCustomCors(builder.Configuration);
services.AddSwaggerServices(builder.Environment);
services.AddCustomLogging(builder.Environment);
services.AddServerConfiguration(builder.Environment);
services.AddCustomRateLimiting();

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

await app.TestConfigurations(settings);

app.Run();

namespace WebApi {
    public class Program;
}