using Adapters.CrossPlatform.SBO.Services;
using Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApi;


namespace UnitTests.Integration.ExternalSystems;

public abstract class BaseExternalTest {
    internal WebApplicationFactory<Program> factory;
    internal ISettings                      settings;
    internal SboCompany                     sboCompany;

    [OneTimeSetUp]
    public void OneTimeSetUp() {
        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => {
                builder.UseEnvironment("IntegrationTests");

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();
            });

        // Create a service scope and resolve the service from it
        using var scope = factory.Services.CreateScope();
        settings = scope.ServiceProvider.GetRequiredService<ISettings>();
        Assert.That(settings.SboSettings != null, "settings.SboSettings != null");
        sboCompany = new SboCompany(settings, factory.Services.GetRequiredService<ILogger<SboCompany>>());
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        try {
            await factory.DisposeAsync();
        }
        catch (Exception ex) {
            await TestContext.Out.WriteLineAsync($"Cleanup failed: {ex.Message}");
        }
    }
    // Get document series for current period for new Inventory Goods Receipt
    internal const int salesOrdersSeries = 119;
    internal const int goodsReceiptSeries = 129;
    internal const int deliveryNoteSeries = 116;
}
