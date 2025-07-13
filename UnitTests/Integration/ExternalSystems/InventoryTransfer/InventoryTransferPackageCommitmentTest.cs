using Core.Enums;
using UnitTests.Integration.ExternalSystems.InventoryTransfer.Helper;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.InventoryTransfer;

[TestFixture]
[Category("Integration")]
[Category("ExternalSystem")]
[Category("RequiresSapB1")]
public class InventoryTransferPackageCommitmentTest : BaseExternalTest {
    private const string                                                            TestWarehouse = "SM";

    private string     testItem = string.Empty;
    private Guid       transferId;
    private List<Guid> createdPackages = [];

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

        var helper = new CreateGoodsReceipt(sboCompany, testItem, settings, goodsReceiptSeries, factory) {
            Package = true
        };
        await helper.Execute();
        createdPackages = helper.CreatedPackages;
    }
    
    [Test]
    [Order(3)]
    public async Task Test_03_CreateInventoryTransfer_ShouldInitializeTransferDocument() {
        var helper = new CreateTransferHelper(testItem, factory);
        transferId = await helper.Execute();
    }
    
    [Test]
    [Order(4)]
    public async Task Test_04_AddItemToTransfer_ShouldBeCommited() {
        var helper = new AddPackageToTransferSource(transferId, testItem, factory, createdPackages.First(), settings);
        await helper.Execute();
    }
    
    [Test]
    [Order(5)]
    public async Task Test_05_CancelTransfer_ShouldReleaseCommit() {
        var helper = new CancelTransferReleaseCommit(transferId, testItem, factory, createdPackages.First(), settings);
        await helper.Execute();
    }
}