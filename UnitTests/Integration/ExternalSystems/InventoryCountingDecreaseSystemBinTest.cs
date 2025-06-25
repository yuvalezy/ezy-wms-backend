using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Core.Interfaces;
using Core.Enums;
using Adapters.CrossPlatform.SBO.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using UnitTests.Integration.ExternalSystems.InventoryCountingDecreaseSystemBinTestHelpers;
using WebApi;

namespace UnitTests.Integration.ExternalSystems;

[TestFixture]
[Category("Integration")]
[Category("ExternalSystem")]
[Category("RequiresSapB1")]
[Explicit("Requires SAP B1 test database connection")]
public class InventoryCountingDecreaseSystemBinTest {
    private WebApplicationFactory<Program> factory;

    private const string TestWarehouse = "SM";

    private ISettings  settings;
    private SboCompany sboCompany;
    private Guid       countingId;
    private int        countingEntry;

    private List<(int binEntry, string binCode, int quantity, UnitType unit)> binEntries;

    private string testItem = string.Empty;

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

    [Test]
    [Order(1)]
    public async Task Test_01_CreateTestItem_ShouldSucceed() {
        var helper = new Test01CreateTestItem(sboCompany);
        testItem = (await helper.Execute()).ItemCode;
    }

    [Test]
    [Order(2)]
    public async Task Test_02_CreateGoodsReceipt_ShouldAddItemToSystemBin() {
        if (!settings.Filters.InitialCountingBinEntry.HasValue) {
            throw new Exception("InitialCountingBinEntry is not set in appsettings.json filters");
        }

        var helper = new Test02CreateGoodsReceipt(sboCompany, testItem, TestWarehouse, settings, factory);
        await helper.Execute();
    }

    [Test]
    [Order(3)]
    public async Task Test_03_CreateInventoryCounting_ShouldInitializeCountingDocument() {
        var helper = new Test03CreateInventoryCounting(testItem, factory);
        countingId = await helper.Execute();
    }

    [Test]
    [Order(4)]
    public async Task Test_04_AddItemToInventoryCounting_ShouldIncludeTestItem() {
        var helper = new Test04AddItems(countingId, testItem, TestWarehouse, factory, settings);
        binEntries = await helper.Execute();
    }

    [Test]
    [Order(5)]
    public async Task Test_05_ProcessInventoryCounting_ShouldUploadToSapB1() {
        using var scope    = factory.Services.CreateScope();
        var       service  = scope.ServiceProvider.GetRequiredService<IInventoryCountingsService>();
        var       response = await service.ProcessCounting(countingId, TestConstants.SessionInfo);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True, $"Processing failed: {response.ErrorMessage}");
        Assert.That(response.Status, Is.EqualTo(ResponseStatus.Ok));
        Assert.That(response.ErrorMessage, Is.Null.Or.Empty);
        Assert.That(response.ExternalEntry, Is.Not.Null, "External entry should be set");
        Assert.That(response.ExternalNumber, Is.Not.Null, "External number should be set");
        countingEntry = response.ExternalEntry.Value;
    }


    [Test]
    [Order(6)]
    public async Task Test_06_VerifyInventoryCountingDocumentInSapB1_ShouldExistWithCorrectData() {
        var helper = new Test06VerifyInventoryCountingDocumentInSapB1(countingEntry, sboCompany, testItem, TestWarehouse, binEntries, settings);
        await helper.Execute();
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
}