using Core.DTOs.Items;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Integration.ExternalSystems.Picking.PickingCancellationHelpers;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.Picking;

[TestFixture]
public class PickingCancellation : BaseExternalTest {
    private string testItem     = string.Empty;
    private string testCustomer = string.Empty;
    private int    salesEntry   = -1;
    private int    absEntry    = -1;
    private Guid   transferId   = Guid.Empty;

    private PickingSelectionResponse[] selection = [];

    [Test]
    [Order(0)]
    public async Task PrepareData() {
        //Generate new item
        var itemHelper = new CreateTestItem(sboCompany);
        testItem = (await itemHelper.Execute()).ItemCode;


        var helper = new CreateGoodsReceipt(sboCompany, settings, goodsReceiptSeries, factory, testItem);
        await helper.Execute();


        //Get customer
        testCustomer = await SboDataHelper.GetCustomer(sboCompany);
        await TestContext.Out.WriteLineAsync($"Test customer: {testCustomer}");
    }

    [Test]
    [Order(1)]
    public async Task CreateSaleOrder_ReleaseToPicking() {
        var helper = new CreateSalesOrder(sboCompany, salesOrdersSeries, testCustomer, testItem);
        await helper.Execute();
        salesEntry = helper.SalesEntry;
        absEntry  = helper.AbsEntry;
        await TestContext.Out.WriteLineAsync($"Created sales order with DocEntry: {salesEntry}");
        Assert.That(salesEntry, Is.Not.EqualTo(-1), "Sales Entry should be created");
        Assert.That(absEntry, Is.Not.EqualTo(-1), "Pick Entry should be created");
    }

    [Test]
    [Order(2)]
    public async Task PickAll() {
        int binEntry = settings.Filters.InitialCountingBinEntry!.Value;
        var helper = new PickAllHelper(absEntry, factory, binEntry, salesEntry, testItem);
        await helper.PickAll();
    }

    [Test]
    [Order(5)]
    public async Task CancelPicking() {
        var scope = factory.Services.CreateScope();

        //save selection for validation
        var adapter = scope.ServiceProvider.GetRequiredService<IExternalSystemAdapter>();
        selection = (await adapter.GetPickingSelection(absEntry)).ToArray();

        //Cancel pick list
        var service  = scope.ServiceProvider.GetRequiredService<IPickListCancelService>();
        var response = await service.CancelPickListAsync(absEntry, TestConstants.SessionInfo);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Status, Is.EqualTo(ResponseStatus.Ok), response.ErrorMessage ?? "No error message");
        Assert.That(response.TransferId.HasValue);
        transferId = response.TransferId.Value;
    }

    [Test]
    [Order(6)]
    public async Task CheckTransfer() {
        int binEntry = settings.Filters.CancelPickingBinEntry;
        var helper   = new CheckTransferHelper(absEntry, selection, factory, binEntry, salesEntry, testItem, sboCompany, transferId);
        await helper.Validate();
    }
}