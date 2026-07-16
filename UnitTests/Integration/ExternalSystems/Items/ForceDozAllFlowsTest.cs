using Adapters.Common.SBO.Repositories;
using Adapters.Common.SBO.Services;
using Core.DTOs.GoodsReceipt;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.Items;

/// <summary>
/// ForceDoz rewrites OITM."NumInBuy" in eleven queries spread across every flow — goods receipt, picking,
/// transfer and counting. The rewrite injects a CASE that reads a second column ("BuyUnitMsr"), which is
/// exactly the kind of change that compiles fine and then throws at runtime: an aggregate query whose
/// GROUP BY does not list the new column is a SQL error, and a bare (unaliased) column becomes an unnamed
/// one the reader cannot find.
///
/// So every rewritten query is executed here against the real SAP database with ForceDoz ON. Several need
/// documents or stock that this environment cannot create (posting SAP documents via the Service Layer is
/// broken here — see CreateGoodsReceipt / InventoryGenEntries 404), so for those the assertion is that the
/// query *executes*; an empty result still proves the SQL is valid, which is the failure mode being guarded.
/// Factor values themselves are asserted in ForceDozTest.
/// </summary>
public class ForceDozAllFlowsTest : BaseExternalTest {
    private SboDatabaseService databaseService;
    private ILoggerFactory loggerFactory;
    private int testBinLocation;
    private string dozenBrokenItem = string.Empty;

    [OneTimeSetUp]
    public new void OneTimeSetUp() {
        base.OneTimeSetUp();

        using var scope = factory.Services.CreateScope();
        databaseService = new SboDatabaseService(scope.ServiceProvider.GetRequiredService<IConfiguration>());
        loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        testBinLocation = settings.GetInitialCountingBinEntry(TestConstants.Warehouse)!.Value;
    }

    private ISettings ForceDozOn => new Settings {
        Options = settings.Options with { ForceDoz = true },
        Filters = settings.Filters,
        CustomFields = settings.CustomFields,
        Item = settings.Item
    };

    [Test, Order(0)]
    public async Task PrepareData() {
        dozenBrokenItem = (await new CreateTestItem(sboCompany).Execute(itemsPerUnit: 1, unit: "PZA", qtyPerPackUnit: 72)).ItemCode;
    }

    // --- Items: scan / stock lookups shared by picking, transfer and counting ---

    [Test, Order(1)]
    public void ItemQueries_ForceDozOn_AllExecute() {
        var repo = new SboItemRepository(databaseService, ForceDozOn);

        Assert.Multiple(() => {
            Assert.DoesNotThrowAsync(async () => await repo.ItemCheckAsync(dozenBrokenItem, null), "BuildItemQuery");
            Assert.DoesNotThrowAsync(async () => await repo.ItemCheckAsync(null, dozenBrokenItem), "barcode variant");
            Assert.DoesNotThrowAsync(async () => await repo.BulkItemCheckAsync([dozenBrokenItem]), "bulk item check");
            Assert.DoesNotThrowAsync(async () => await repo.ItemStockAsync(dozenBrokenItem, TestConstants.Warehouse), "item stock");
            Assert.DoesNotThrowAsync(async () => await repo.ItemBinStockAsync(dozenBrokenItem, TestConstants.Warehouse), "item bin stock");
            Assert.DoesNotThrowAsync(async () => await repo.ItemsWarehouseStockAsync(TestConstants.Warehouse, [dozenBrokenItem]), "warehouse stock");
            Assert.DoesNotThrowAsync(async () => await repo.GetItemValidationInfo(dozenBrokenItem, null, TestConstants.Warehouse, testBinLocation, true), "validation info (with bin)");
            Assert.DoesNotThrowAsync(async () => await repo.GetItemPurchaseUnits(dozenBrokenItem), "purchase units");
            Assert.DoesNotThrowAsync(async () => await repo.ScanItemBarCodeAsync(dozenBrokenItem), "barcode scan");
        });
    }

    // --- General: bin content / bin check, used by counting and transfer ---

    [Test, Order(2)]
    public void BinQueries_ForceDozOn_AllExecute() {
        var repo = new SboGeneralRepository(databaseService, ForceDozOn);

        Assert.Multiple(() => {
            Assert.DoesNotThrowAsync(async () => await repo.BinCheckAsync(testBinLocation), "bin check");
            Assert.DoesNotThrowAsync(async () => await repo.BulkBinCheckAsync([testBinLocation]), "bulk bin check");
        });
    }

    // --- Goods receipt ---

    [Test, Order(3)]
    public void GoodsReceiptQueries_ForceDozOn_AllExecute() {
        var repo = new SboGoodsReceiptRepository(databaseService, loggerFactory, ForceDozOn);

        Assert.Multiple(() => {
            Assert.DoesNotThrowAsync(
                async () => await repo.ValidateGoodsReceiptAddItem(dozenBrokenItem, null, TestConstants.Warehouse, [], false),
                "validate add item");

            // LoadGoodsReceiptItemData: the read that re-derives base quantity from the current factor.
            Assert.DoesNotThrowAsync(async () => {
                var data = new Dictionary<string, List<GoodsReceiptCreationDataResponse>> {
                    [dozenBrokenItem] = [new GoodsReceiptCreationDataResponse { ItemCode = dozenBrokenItem, Unit = UnitType.Dozen, ScannedQuantity = 1, Quantity = 1 }]
                };
                await repo.LoadGoodsReceiptItemData(data);
            }, "load goods receipt item data");
        });
    }

    [Test, Order(4)]
    public async Task LoadGoodsReceiptItemData_ForceDozOn_RederivesBaseFromTheSubstitutedFactor() {
        var repo = new SboGoodsReceiptRepository(databaseService, loggerFactory, ForceDozOn);

        // One dozen scanned. Stored base is stale (1); the current factor must re-derive it.
        var data = new Dictionary<string, List<GoodsReceiptCreationDataResponse>> {
            [dozenBrokenItem] = [new GoodsReceiptCreationDataResponse {
                ItemCode = dozenBrokenItem, Unit = UnitType.Dozen, ScannedQuantity = 1, Quantity = 1, UseBaseUnit = true, Sources = []
            }]
        };

        await repo.LoadGoodsReceiptItemData(data);

        // UseBaseUnit sends base pieces as-is: 1 dozen of the converted rung = 12 pieces.
        Assert.That(data[dozenBrokenItem][0].Quantity, Is.EqualTo(12m));
    }

    // --- Picking ---

    [Test, Order(5)]
    public void PickingQueries_ForceDozOn_AllExecute() {
        var detail = new SboPickingDetailRepository(databaseService, ForceDozOn);
        var bin = new SboPickingBinRepository(databaseService, ForceDozOn);

        // Same parameter set production builds (PickListDetailService.GetPickListItemDetailsExecute);
        // 17 is the sales-order base object. A non-existent AbsEntry just yields no rows.
        Dictionary<string, object> Parameters() => new() { ["@AbsEntry"] = -1, ["@Type"] = 17, ["@Entry"] = -1 };

        Assert.Multiple(() => {
            // Aggregate query whose GROUP BY must list BuyUnitMsr, or SQL Server rejects the CASE.
            Assert.DoesNotThrowAsync(async () => await bin.GetPickingSelection(-1), "picking selection (GROUP BY)");
            Assert.DoesNotThrowAsync(async () => await bin.GetPickingDetailItemsBins(Parameters()), "picking detail item bins");
            Assert.DoesNotThrowAsync(async () => await detail.GetPickingDetails(Parameters()), "picking details");
            Assert.DoesNotThrowAsync(async () => await detail.GetPickingDetailItems(Parameters()), "picking detail items (GROUP BY)");
        });
    }

    // --- The facade every flow actually resolves through DI ---

    [Test, Order(6)]
    public void PickingFacade_ForceDozOn_WiresSettingsIntoEveryChildRepository() {
        var facade = new SboPickingRepository(databaseService, ForceDozOn);

        // SboPickingBinRepository was the one repo constructed without ISettings; if that regressed, its
        // queries would silently ignore ForceDoz rather than fail.
        Assert.DoesNotThrowAsync(async () => await facade.GetPickingSelection(-1));
    }

    [OneTimeTearDown]
    public new async Task OneTimeTearDown() {
        await base.OneTimeTearDown();
        loggerFactory?.Dispose();
    }
}
