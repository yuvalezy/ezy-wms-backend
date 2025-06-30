using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.Picking;

[TestFixture]
public class PickingCancellation : BaseExternalTest {
    private string testItem     = string.Empty;
    private string testCustomer = string.Empty;
    private int    salesEntry   = -1;
    private int    pickEntry    = -1;

    [Test]
    [Order(0)]
    public async Task PrepareData() {
        //Generate new item
        var itemHelper = new CreateTestItem(sboCompany);
        testItem = (await itemHelper.Execute()).ItemCode;


        var helper = new CreateGoodsReceipt(sboCompany, testItem, settings, goodsReceiptSeries, factory);
        await helper.Execute();


        //Get customer
        testCustomer = await SboDataHelper.GetCustomer(sboCompany);
        await TestContext.Out.WriteLineAsync($"Test customer: {testCustomer}");
    }

    [Test]
    [Order(1)]
    public async Task CreateSaleOrder_ReleaseToPicking() {
        var helper = new CreateSalesOrder(sboCompany, testItem, salesOrdersSeries, testCustomer);
        await helper.Execute();
        salesEntry = helper.SalesEntry;
        pickEntry  = helper.PickEntry;
        await TestContext.Out.WriteLineAsync($"Created sales order with DocEntry: {salesEntry}");
        Assert.That(salesEntry, Is.Not.EqualTo(-1), "Sales Entry should be created");
        Assert.That(pickEntry, Is.Not.EqualTo(-1), "Pick Entry should be created");
    }

    [Test]
    [Order(2)]
    public async Task PickAll() {
        var scope    = factory.Services.CreateScope();
        var service  = scope.ServiceProvider.GetRequiredService<IPickListService>();
        int binEntry = settings.Filters.InitialCountingBinEntry!.Value;
        var request = new PickListAddItemRequest {
            ID        = pickEntry,
            Type      = 17,
            Entry     = salesEntry,
            ItemCode  = testItem,
            BinEntry  = binEntry,
            Unit      = UnitType.Pack
        };
        //Pick 20 boxes
        for (int i = 0; i < 20; i++) {
            request.Quantity = 1;
            var response = await service.AddItem(TestConstants.SessionInfo, request);
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo(ResponseStatus.Ok), response.ErrorMessage ?? "No error message");
        }
        
        //Test exceed error
        request.Quantity = 1;
        var errorResponse = await service.AddItem(TestConstants.SessionInfo, request);
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse.Status, Is.EqualTo(ResponseStatus.Error), errorResponse.ErrorMessage ?? "No error message");
        Assert.That(errorResponse.ErrorMessage, Is.EqualTo("Quantity exceeds bin available stock"));
        
        //Process
        var processService = scope.ServiceProvider.GetRequiredService<IPickListProcessService>();
        var processResponse = await processService.ProcessPickList(pickEntry, TestConstants.SessionInfo.Guid);
        Assert.That(processResponse, Is.Not.Null);
        Assert.That(processResponse.Status, Is.EqualTo(ResponseStatus.Ok), processResponse.ErrorMessage ?? "No error message");
    }

    [Test]
    [Order(5)]
    public async Task CancelPicking() {
        var scope    = factory.Services.CreateScope();
        var service  = scope.ServiceProvider.GetRequiredService<IPickListProcessService>();
        var response = await service.CancelPickList(pickEntry, TestConstants.SessionInfo.Guid);;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Status, Is.EqualTo(ResponseStatus.Ok), response.ErrorMessage ?? "No error message");
    }

    [Test]
    [Order(6)]
    public void CheckTransfer() {
        // TODO: Implement transfer verification
    }
}