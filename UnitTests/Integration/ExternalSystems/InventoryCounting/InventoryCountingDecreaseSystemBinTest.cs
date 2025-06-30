using Core.Enums;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Integration.ExternalSystems.InventoryCounting.InventoryCountingDecreaseSystemBinTestHelpers;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.InventoryCounting;

[TestFixture]
[Category("Integration")]
[Category("ExternalSystem")]
[Category("RequiresSapB1")]

public class InventoryCountingDecreaseSystemBinTest : BaseExternalTest {

    private const string TestWarehouse = "SM";

    private Guid       countingId;
    private int        countingEntry;

    private List<(int binEntry, string binCode, int quantity, UnitType unit)> binEntries;

    private string testItem = string.Empty;


    [Test]
    [Order(1)]
    public async Task Test_01_CreateTestItem_ShouldSucceed() {
        var helper = new CreateTestItem(sboCompany);
        testItem = (await helper.Execute()).ItemCode;
    }

    [Test]
    [Order(2)]
    public async Task Test_02_CreateGoodsReceipt_ShouldAddItemToSystemBin() {
        if (!settings.Filters.InitialCountingBinEntry.HasValue) {
            throw new Exception("InitialCountingBinEntry is not set in appsettings.json filters");
        }

        var helper = new CreateGoodsReceipt(sboCompany, testItem, settings, goodsReceiptSeries, factory);
        await helper.Execute();
    }

    [Test]
    [Order(3)]
    public async Task Test_03_CreateInventoryCounting_ShouldInitializeCountingDocument() {
        var helper = new CreateInventoryCounting(testItem, factory);
        countingId = await helper.Execute();
    }

    [Test]
    [Order(4)]
    public async Task Test_04_AddItemToInventoryCounting_ShouldIncludeTestItem() {
        var helper = new AddItems(countingId, testItem, TestWarehouse, factory, settings);
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
        var helper = new VerifyResult(countingEntry, sboCompany, testItem, TestWarehouse, binEntries, settings);
        await helper.Execute();
    }
}