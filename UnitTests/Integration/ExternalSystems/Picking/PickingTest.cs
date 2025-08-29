using Adapters.Common.SBO.Services;
using Adapters.CrossPlatform.SBO.Helpers;
using Core.Entities;
using Core.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.Picking;

public class PickingTest : BaseExternalTest {
    private readonly string[] testItems = new string[3];
    private string testCustomer = string.Empty;
    private int salesEntry = -1;
    private int absEntry = -1;
    private SboDatabaseService databaseService;
    private ILoggerFactory loggerFactory;

    [OneTimeSetUp]
    new public void OneTimeSetUp() {
        base.OneTimeSetUp();

        // Get services from the factory's service provider
        using var scope = factory.Services.CreateScope();
        databaseService = new SboDatabaseService(settings);
        loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    }

    [Test]
    [Order(0)]
    public async Task PrepareData() {
        //Create Items
        var itemHelper = new CreateTestItem(sboCompany);
        testItems[0] = (await itemHelper.Execute()).ItemCode;
        testItems[1] = (await itemHelper.Execute()).ItemCode;
        testItems[2] = (await itemHelper.Execute()).ItemCode;

        var grpo = new CreateGoodsReceipt(sboCompany, settings, goodsReceiptSeries, factory, testItems);
        await grpo.Execute();

        //Get Customer
        testCustomer = await SboDataHelper.GetCustomer(sboCompany);

        var helper = new CreateSalesOrder(sboCompany, salesOrdersSeries, testCustomer, testItems);
        await helper.Execute();
        salesEntry = helper.SalesEntry;
        absEntry = helper.AbsEntry;
    }

    //UpdateReleaseAllocation only once to clear bin location
    
    #region Full Test
    //Pick Qty 480, Released Qty 480, PRev Qty 960, Bin Location 480
    [Test, Order(1)]
    public async Task Pick40Item() => await ExecutePick(0, 40);
    
    //Pick Qty 240, Released Qty 720, PRev Qty 480, Bin Location 720
    [Test, Order(2)]
    public async Task Pick20Item() => await ExecutePick(0, 20);
    
    //Pick Qty 120, Released Qty 840, PRev Qty 720, Bin Location 840
    [Test, Order(3)]
    public async Task Pick10Item() => await ExecutePick(0, 10);
    
    //Pick Qty 120, Released Qty 960, Prev Qty 840, Bin Location 960
    [Test, Order(4)]
    public async Task PickLast10Item() => await ExecutePick(0, 10);


    // [Test, Order(1)]
    // public async Task PickFirstItem() => await ExecutePick(0, 80);
    //
    // [Test, Order(2)]
    // public async Task PickSecondItem() => await ExecutePick(1, 80);
    //
    // [Test, Order(3)]
    // public async Task PickThirdItem() => await ExecutePick(2, 80);

    #endregion

    private async Task ExecutePick(int index, int quantity) {
        int testBinLocation = settings.GetInitialCountingBinEntry(TestConstants.Warehouse)!.Value;

        var pickUpdate = new PickingUpdate(absEntry, [
            new PickList {
                ItemCode = testItems[index],
                PickEntry = index,
                Quantity = quantity * 12,
                BinEntry = testBinLocation
            }
        ], sboCompany, databaseService, loggerFactory);

        await pickUpdate.Execute();
    }

    [OneTimeTearDown]
    new public async Task OneTimeTearDown() {
        await base.OneTimeTearDown();
        loggerFactory?.Dispose();
    }
}