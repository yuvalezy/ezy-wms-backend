using System;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Models.Settings;
using Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Service.Configuration;

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
        Console.WriteLine($"  Adapter Type: {settings.ExternalAdapter}");
        Console.WriteLine();

        // Test connection using the proper adapter
        try {
            await TestConnectionUsingAdapter(settings, configuration);
        }
        catch (Exception ex) {
            Console.WriteLine($"❌ Connection test failed: {ex.Message}");
            Environment.Exit(1);
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private async static Task TestConnectionUsingAdapter(Settings settings, IConfiguration configuration) {
        Console.WriteLine($"Testing connection using {settings.ExternalAdapter} adapter...");

        // Set up dependency injection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add settings
        services.AddSingleton<ISettings>(settings);

        // Configure the appropriate adapter
        switch (settings.ExternalAdapter) {
            case ExternalAdapterType.SboWindows:
                SboWindowsDependencyInjection.ConfigureServices(services);
                break;
            case ExternalAdapterType.SboServiceLayer:
                SboServiceLayerDependencyInjection.ConfigureServices(services);
                break;
            default:
                throw new ArgumentOutOfRangeException($"External Adapter {settings.ExternalAdapter} is not supported");
        }

        // Build service provider
        await using var serviceProvider = services.BuildServiceProvider();

        if (settings.ExternalAdapter == ExternalAdapterType.SboWindows) {
            await TestWindowsConnection(serviceProvider);
        }
        else {
            await TestServiceLayerConnection(serviceProvider);
        }
    }

    private async static Task TestWindowsConnection(IServiceProvider serviceProvider) {
        Console.WriteLine("Testing Windows COM connection...");
        
        try {
            var sboCompany = serviceProvider.GetRequiredService<Adapters.Windows.SBO.Services.SboCompany>();
            bool connected = sboCompany.ConnectCompany();
            
            if (connected) {
                Console.WriteLine("✅ Successfully connected to SAP Business One via COM");
            }
            else {
                Console.WriteLine("❌ Failed to connect to SAP Business One via COM");
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"❌ Windows COM connection failed: {ex.Message}", ex);
        }
    }

    private async static Task TestServiceLayerConnection(IServiceProvider serviceProvider) {
        Console.WriteLine("Testing Service Layer connection...");
        
        try {
            var sboCompany = serviceProvider.GetRequiredService<Adapters.CrossPlatform.SBO.Services.SboCompany>();
            bool connected = await sboCompany.ConnectCompany();
            
            if (connected) {
                Console.WriteLine("✅ Successfully connected to SAP Business One Service Layer");
                Console.WriteLine($"   Session ID: {sboCompany.SessionId}");
            }
            else {
                Console.WriteLine("❌ Failed to connect to SAP Business One Service Layer");
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"❌ Service Layer connection failed: {ex.Message}", ex);
        }
    }
}