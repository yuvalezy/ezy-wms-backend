using Core.DTOs.Items;
using Core.DTOs.Package;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Integration.ExternalSystems.Picking.PickingCancellationHelpers;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.Picking;

[TestFixture]
public class PickingNewPackage : BaseExternalTest {
    private string[] testItems = new string[3];
    private string testItemNoPackage = string.Empty;
    private string testCustomer = string.Empty;
    private int salesEntry = -1;
    private int absEntry = -1;
    private Guid transferId = Guid.Empty;

    private PickingSelectionResponse[] selection = [];
    private Dictionary<string, List<Guid>> packages;
    private Guid packageId;
    private Guid pickListPackageId;

    [Test]
    [Order(0)]
    public async Task PrepareData() {
        //Generate new item
        var itemHelper = new CreateTestItem(sboCompany);
        testItems[0] = (await itemHelper.Execute()).ItemCode;
        testItems[1] = (await itemHelper.Execute()).ItemCode;
        testItems[2] = (await itemHelper.Execute()).ItemCode;

        testItemNoPackage = (await itemHelper.Execute()).ItemCode;

        // Create GRPO for no Package Item
        var helper = new CreateGoodsReceipt(sboCompany, settings, goodsReceiptSeries, factory, testItemNoPackage);
        await helper.Execute();

        // Create GRPO for 3 package managed items
        helper = new CreateGoodsReceipt(sboCompany, settings, goodsReceiptSeries, factory, testItems) {
            Package = true
        };

        await helper.Execute();
        packages = helper.CreatedPackages;


        //Get customer
        testCustomer = await SboDataHelper.GetCustomer(sboCompany);
        await TestContext.Out.WriteLineAsync($"Test customer: {testCustomer}");
    }

    [Test]
    [Order(1)]
    public async Task CreateSaleOrder_ReleaseToPicking() {
        var orderItems = new[] { testItemNoPackage }.Concat(testItems).ToArray();
        var helper = new CreateSalesOrder(sboCompany, salesOrdersSeries, testCustomer, orderItems);
        await helper.Execute();
        salesEntry = helper.SalesEntry;
        absEntry = helper.AbsEntry;
        await TestContext.Out.WriteLineAsync($"Created sales order with DocEntry: {salesEntry}");
        Assert.That(salesEntry, Is.Not.EqualTo(-1), "Sales Entry should be created");
        Assert.That(absEntry, Is.Not.EqualTo(-1), "Pick Entry should be created");
    }

    [Test]
    [Order(2)]
    public async Task CreatePicking_NewPackage() {
        var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPickListPackageService>();
        var response = await service.CreatePackageAsync(absEntry, TestConstants.SessionInfo);
        packageId = response.Id;
        pickListPackageId = response.PickListPackageId!.Value;

        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var package = await db.Packages.FindAsync(packageId);
        Assert.That(package, Is.Not.Null);
        Assert.That(package.SourceOperationType, Is.EqualTo(ObjectType.Picking));
        Assert.That(package.SourceOperationId, Is.EqualTo(pickListPackageId));
        Assert.That(package.Status, Is.EqualTo(PackageStatus.Init));
        Assert.That(package.BinEntry, Is.EqualTo(settings.Filters.StagingBinEntry!.Value));

        var pickListPackage = await db.PickListPackages.FindAsync(pickListPackageId);
        Assert.That(pickListPackage, Is.Not.Null);
        Assert.That(pickListPackage.PackageId, Is.EqualTo(packageId));
        Assert.That(pickListPackage.BinEntry, Is.EqualTo(settings.Filters.StagingBinEntry!.Value));
        Assert.That(pickListPackage.Type, Is.EqualTo(SourceTarget.Target));
        Assert.That(pickListPackage.PickEntry, Is.Null);
    }

    [Test]
    [Order(3)]
    public async Task AddItemNoContainer_IntoNewPackage() {
        var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPickListLineService>();
        var request = new PickListAddItemRequest {
            ID = absEntry,
            Type = 17,
            Entry = salesEntry,
            ItemCode = testItemNoPackage,
            Quantity = 1,
            BinEntry = settings.Filters.InitialCountingBinEntry!.Value,
            Unit = UnitType.Dozen,
            PickEntry = 0,
            PackageId = null,
            PickingPackageId = packageId
        };
        await service.AddItem(TestConstants.SessionInfo, request);
    }
    
    [Test]
    [Order(4)]
    public async Task AddPartialFromPackage_IntoNewPackage() {
        var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPickListLineService>();
        var itemCode = testItems[0];
        var itemPackages = packages[itemCode];
        var request = new PickListAddItemRequest {
            ID = absEntry,
            Type = 17,
            Entry = salesEntry,
            ItemCode = itemCode,
            Quantity = 1,
            BinEntry = settings.Filters.InitialCountingBinEntry!.Value,
            Unit = UnitType.Dozen,
            PickEntry = 1,
            PackageId = itemPackages[0],
            PickingPackageId = packageId
        };
        await service.AddItem(TestConstants.SessionInfo, request);
    }
    [Test]
    [Order(5)]
    public async Task AddFullPackage_IntoNewPackage() {
        var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPickListPackageService>();
        var itemCode = testItems[1];
        var itemPackages = packages[itemCode];
        var request = new PickListAddPackageRequest {
            ID = absEntry,
            Type = 17,
            Entry = salesEntry,
            PackageId = itemPackages[0],
            BinEntry = settings.Filters.InitialCountingBinEntry!.Value,
            PickingPackageId = packageId
        };
        await service.AddPackageAsync(request, TestConstants.SessionInfo);
    }

    [Test]
    [Order(6)]
    public async Task Validate_NewPackageContent() {
        Assert.Fail("Not Implemented!");
    }

    [Test]
    [Order(7)]
    public async Task Process_AssertPackagesMovements() {
        Assert.Fail("Not Implemented!");
    }
}