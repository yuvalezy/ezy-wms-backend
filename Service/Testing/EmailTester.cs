using System;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Models.Settings;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Service.Testing;

public static class EmailTester {
    public static async Task RunTest(IConfiguration configuration) {
        Console.WriteLine("=== Email Configuration Test ===");
        Console.WriteLine();

        var settings = new Settings();
        configuration.Bind(settings);

        // Display SMTP configuration
        DisplaySmtpConfiguration(settings);

        if (!settings.Smtp.Enabled) {
            Console.WriteLine("⚠️  SMTP is not enabled in configuration");
            Console.WriteLine();
            Console.WriteLine("To enable SMTP, set 'Smtp:Enabled' to true in appsettings.json");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Validate required settings
        if (!ValidateSmtpSettings(settings)) {
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Prompt for test email address
        Console.WriteLine();
        Console.Write("Enter email address to send test email to: ");
        string? testEmail = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(testEmail)) {
            Console.WriteLine("❌ No email address provided");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Validate email format
        if (!IsValidEmail(testEmail)) {
            Console.WriteLine($"❌ Invalid email address format: {testEmail}");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Sending test email...");
        Console.WriteLine();

        // Test email sending
        try {
            await SendTestEmail(settings, testEmail);
        }
        catch (Exception ex) {
            Console.WriteLine($"❌ Email test failed: {ex.Message}");
            if (ex.InnerException != null) {
                Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
            }
            Environment.Exit(1);
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static void DisplaySmtpConfiguration(Settings settings) {
        Console.WriteLine("SMTP Configuration:");
        Console.WriteLine($"  Enabled: {settings.Smtp.Enabled}");
        Console.WriteLine($"  Host: {settings.Smtp.Host}");
        Console.WriteLine($"  Port: {settings.Smtp.Port}");
        Console.WriteLine($"  Enable SSL: {settings.Smtp.EnableSsl}");
        Console.WriteLine($"  Username: {settings.Smtp.Username ?? "(not set)"}");
        Console.WriteLine($"  Password: {(string.IsNullOrEmpty(settings.Smtp.Password) ? "(not set)" : new string('*', 8))}");
        Console.WriteLine($"  From Email: {settings.Smtp.FromEmail}");
        Console.WriteLine($"  From Name: {settings.Smtp.FromName}");
        Console.WriteLine($"  Timeout: {settings.Smtp.TimeoutSeconds}s");
        Console.WriteLine();
    }

    private static bool ValidateSmtpSettings(Settings settings) {
        bool isValid = true;

        if (string.IsNullOrEmpty(settings.Smtp.Host)) {
            Console.WriteLine("❌ SMTP Host is not configured");
            isValid = false;
        }

        if (settings.Smtp.Port <= 0 || settings.Smtp.Port > 65535) {
            Console.WriteLine($"❌ Invalid SMTP Port: {settings.Smtp.Port}");
            isValid = false;
        }

        if (string.IsNullOrEmpty(settings.Smtp.FromEmail)) {
            Console.WriteLine("❌ From Email is not configured");
            isValid = false;
        }
        else if (!IsValidEmail(settings.Smtp.FromEmail)) {
            Console.WriteLine($"❌ Invalid From Email format: {settings.Smtp.FromEmail}");
            isValid = false;
        }

        return isValid;
    }

    private static bool IsValidEmail(string email) {
        try {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch {
            return false;
        }
    }

    private static async Task SendTestEmail(Settings settings, string recipientEmail) {
        // Set up dependency injection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add settings
        services.AddSingleton<ISettings>(settings);

        // Add email services
        services.AddScoped<IEmailService, EmailService>();

        // Build service provider
        await using var serviceProvider = services.BuildServiceProvider();

        // Get email service
        var emailService = serviceProvider.GetRequiredService<IEmailService>();

        // Check if SMTP is configured
        if (!emailService.IsSmtpConfigured()) {
            Console.WriteLine("❌ SMTP is not properly configured");
            return;
        }

        Console.WriteLine("✅ SMTP configuration validated");
        Console.WriteLine($"   Connecting to {settings.Smtp.Host}:{settings.Smtp.Port}...");

        // Send test email
        bool success = await emailService.TestSmtpConnectionAsync(recipientEmail);

        if (success) {
            Console.WriteLine("✅ Test email sent successfully!");
            Console.WriteLine($"   Check inbox at: {recipientEmail}");
            Console.WriteLine();
            Console.WriteLine("   If you don't see the email:");
            Console.WriteLine("   1. Check your spam/junk folder");
            Console.WriteLine("   2. Verify the SMTP credentials are correct");
            Console.WriteLine("   3. Ensure your email provider allows SMTP connections");
        }
        else {
            Console.WriteLine("❌ Failed to send test email");
            Console.WriteLine();
            Console.WriteLine("   Common issues:");
            Console.WriteLine("   1. Incorrect SMTP host or port");
            Console.WriteLine("   2. Invalid username or password");
            Console.WriteLine("   3. SSL/TLS settings mismatch");
            Console.WriteLine("   4. Firewall blocking SMTP port");
            Console.WriteLine("   5. Email provider requires app-specific password");
        }
    }
}
