using System;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Service.Testing;

public static class SboConnectionTester {
    public async static Task RunTest(IConfiguration configuration) {
        Console.WriteLine("=== SAP Business One Connection Test ===");
        Console.WriteLine();

        var settings = new Settings();
        configuration.Bind(settings);

        if (settings.SboSettings == null) {
            Console.WriteLine("❌ SboSettings not found in configuration");
            Environment.Exit(1);
            return;
        }

        // Display configuration (excluding password)
        Console.WriteLine("Configuration:");
        Console.WriteLine($"  Server: {settings.SboSettings.Server}");
        Console.WriteLine($"  Database: {settings.SboSettings.Database}");
        Console.WriteLine($"  User: {settings.SboSettings.User}");
        Console.WriteLine($"  Password: {new string('*', settings.SboSettings.Password?.Length ?? 0)}");
        Console.WriteLine($"  Server Type: {settings.SboSettings.ServerType}");
        Console.WriteLine($"  Service Layer URL: {settings.SboSettings.ServiceLayerUrl}");
        Console.WriteLine($"  Trusted Connection: {settings.SboSettings.TrustedConnection}");

        string adapterType = configuration["ExternalAdapter"] ?? "Unknown";
        Console.WriteLine($"  Adapter Type: {adapterType}");
        Console.WriteLine();

        // Test connection based on adapter type
        try {
            if (adapterType == "SboServiceLayer") {
                await TestServiceLayerConnection(settings);
            }
            else {
                Console.WriteLine("⚠️  Windows COM adapter testing not implemented in this test mode");
                Console.WriteLine("   (COM interop requires full Windows environment)");
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"❌ Connection test failed: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private async static Task TestServiceLayerConnection(ISettings settings) {
        Console.WriteLine("Testing Service Layer connection...");

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<Adapters.CrossPlatform.SBO.Services.SboCompany>();

        var sboCompany = new Adapters.CrossPlatform.SBO.Services.SboCompany(settings, logger);

        bool connected = await sboCompany.ConnectCompany();

        if (connected) {
            Console.WriteLine("✅ Successfully connected to SAP Business One Service Layer");
            Console.WriteLine($"   Session ID: {sboCompany.SessionId}");
        }
        else {
            Console.WriteLine("❌ Failed to connect to SAP Business One Service Layer");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}