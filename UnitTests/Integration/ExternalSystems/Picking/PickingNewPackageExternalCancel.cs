using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.Picking;

[TestFixture]
public class PickingNewPackageExternalCancel : BaseExternalTest {
    private readonly string[] testItems = new string[3];
    private string testItemNoPackage = string.Empty;
    private string testCustomer = string.Empty;
    private int salesEntry = -1;
    private int absEntry = -1;
    private Guid transferId = Guid.Empty;

    private Dictionary<string, List<Guid>> packages;
    private Guid packageId;
    private Guid pickListPackageId;
    private int deliveryNoteEntry;

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
        var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();

        // Get the created package with its contents and commitments
        var package = await db.Packages
        .Include(p => p.Contents)
        .Include(p => p.Commitments)
        .FirstOrDefaultAsync(p => p.Id == packageId);

        Assert.That(package, Is.Not.Null, "Target package should exist");
        Assert.That(package.Status, Is.EqualTo(PackageStatus.Init), "Target package should be init");
        Assert.That(package.BinEntry, Is.EqualTo(settings.Filters.StagingBinEntry!.Value), "Target package should be in staging bin");

        // Validate package contents - should have 3 items: 1 no-package item + 1 partial + 1 full package
        Assert.That(package.Contents.Count, Is.EqualTo(3), "Target package should contain 3 items");

        //Validate target package no source package (testItemNoPackage)
        var noPackageContent = package.Contents.FirstOrDefault(c => c.ItemCode == testItemNoPackage);
        Assert.That(noPackageContent, Is.Not.Null, "Target package should contain no-package item");
        Assert.That(noPackageContent.Quantity, Is.EqualTo(12), "No-package item quantity should be 12");
        Assert.That(noPackageContent.CommittedQuantity, Is.EqualTo(12), "No-package item committed quantity should equal quantity");
        Assert.That(noPackageContent.BinEntry, Is.EqualTo(settings.Filters.InitialCountingBinEntry!.Value), "No-package item should be in staging bin");

        //Validate target package partial package source (testItems[0])
        var partialContent = package.Contents.FirstOrDefault(c => c.ItemCode == testItems[0]);
        Assert.That(partialContent, Is.Not.Null, "Target package should contain partial item");
        Assert.That(partialContent.Quantity, Is.EqualTo(12), "Partial item quantity should be 12");
        Assert.That(partialContent.CommittedQuantity, Is.EqualTo(12), "Partial item committed quantity should equal quantity");
        Assert.That(partialContent.BinEntry, Is.EqualTo(settings.Filters.InitialCountingBinEntry!.Value), "Partial item should be in staging bin");

        // Verify source package for partial item has commitment
        var sourcePackageId = packages[testItems[0]][0];
        var sourcePackage = await db.Packages
        .Include(p => p.Contents)
        .Include(p => p.Commitments)
        .FirstOrDefaultAsync(p => p.Id == sourcePackageId);

        Assert.That(sourcePackage, Is.Not.Null, "Source package for partial item should exist");
        var sourceContent = sourcePackage.Contents.FirstOrDefault(c => c.ItemCode == testItems[0]);
        Assert.That(sourceContent, Is.Not.Null, "Source package should contain the item");
        Assert.That(sourceContent.CommittedQuantity, Is.EqualTo(12), "Source package item should have 12 committed quantity");

        var commitment = sourcePackage.Commitments.FirstOrDefault(c => c.ItemCode == testItems[0] && c.TargetPackageId == packageId);
        Assert.That(commitment, Is.Not.Null, "Source package should have commitment to target package");
        Assert.That(commitment.Quantity, Is.EqualTo(12), "Commitment quantity should be 12");
        Assert.That(commitment.SourceOperationType, Is.EqualTo(ObjectType.Picking), "Commitment should be for picking operation");

        //Validate target package full source package (testItems[1])
        var fullContent = package.Contents.FirstOrDefault(c => c.ItemCode == testItems[1]);
        Assert.That(fullContent, Is.Not.Null, "Target package should contain full package item");
        Assert.That(fullContent.Quantity, Is.EqualTo(24), "Full package item should have positive quantity");
        Assert.That(fullContent.CommittedQuantity, Is.EqualTo(24), "Full package item committed quantity should equal quantity");
        Assert.That(fullContent.BinEntry, Is.EqualTo(settings.Filters.InitialCountingBinEntry!.Value), "Full package item should be in staging bin");

        // Verify source package for full item is fully committed
        var fullSourcePackageId = packages[testItems[1]][0];
        var fullSourcePackage = await db.Packages
        .Include(p => p.Contents)
        .Include(p => p.Commitments)
        .FirstOrDefaultAsync(p => p.Id == fullSourcePackageId);

        Assert.That(fullSourcePackage, Is.Not.Null, "Source package for full item should exist");
        var fullSourceContent = fullSourcePackage.Contents.FirstOrDefault(c => c.ItemCode == testItems[1]);
        Assert.That(fullSourceContent, Is.Not.Null, "Full source package should contain the item");
        Assert.That(fullSourceContent.CommittedQuantity, Is.EqualTo(fullSourceContent.Quantity), "Full source package should be fully committed");

        var fullCommitment = fullSourcePackage.Commitments.FirstOrDefault(c => c.ItemCode == testItems[1] && c.TargetPackageId == packageId);
        Assert.That(fullCommitment, Is.Not.Null, "Full source package should have commitment to target package");
        Assert.That(fullCommitment.Quantity, Is.EqualTo(fullSourceContent.Quantity), "Full commitment quantity should match source content");
        Assert.That(fullCommitment.SourceOperationType, Is.EqualTo(ObjectType.Picking), "Full commitment should be for picking operation");

        // Validate PickListPackage records
        var pickListPackages = await db.PickListPackages
        .Where(plp => plp.AbsEntry == absEntry)
        .ToListAsync();

        // Should have 3 records: 1 target + 2 sources (partial and full)
        Assert.That(pickListPackages.Count, Is.EqualTo(3), "Should have 3 PickListPackage records");

        var targetPackageRecord = pickListPackages.FirstOrDefault(plp => plp.PackageId == packageId && plp.Type == SourceTarget.Target);
        Assert.That(targetPackageRecord, Is.Not.Null, "Should have target package record");
        Assert.That(targetPackageRecord.BinEntry, Is.EqualTo(settings.Filters.StagingBinEntry!.Value), "Target package record should be in staging bin");

        var sourcePackageRecords = pickListPackages.Where(plp => plp.Type == SourceTarget.Source).ToList();
        Assert.That(sourcePackageRecords.Count, Is.EqualTo(2), "Should have 2 source package records");
    }

    [Test]
    [Order(7)]
    public async Task Process_AssertPackagesMovements() {
        var scope = factory.Services.CreateScope();
        var sboCompany = scope.ServiceProvider.GetRequiredService<SboCompany>();
        Assert.That(await sboCompany.ConnectCompany());
        
        var closePickListData = new {
            PickList = new {
                Absoluteentry = absEntry
            }
        };
        
        var response = await sboCompany.PostAsync("PickListsService_Close", closePickListData);
        Assert.That(response.success, Is.True, response.errorMessage ?? "Unknown error occured");;

        var picklistDetailService = scope.ServiceProvider.GetRequiredService<IPickListDetailService>();
        await picklistDetailService.ProcessClosedPickListsWithPackages();
        
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        transferId = (await db.Transfers.OrderByDescending(v => v.CreatedAt).LastAsync()).Id;
    }

    [Test]
    [Order(8)]
    public async Task Validate_ValidateMovements() {
        var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        int cancelBinEntry = settings.Filters.CancelPickingBinEntry;

        // Validate source partial package (testItems[0])
        var sourcePartialPackageId = packages[testItems[0]][0];
        var sourcePartialPackage = await db.Packages
        .Include(p => p.Contents)
        .Include(p => p.Commitments)
        .Include(p => p.Transactions)
        .FirstOrDefaultAsync(p => p.Id == sourcePartialPackageId);

        Assert.That(sourcePartialPackage, Is.Not.Null, "Source partial package should exist");

        Assert.That(sourcePartialPackage.Status, Is.EqualTo(PackageStatus.Active), "Source partial package should still be Active");

        var partialContent = sourcePartialPackage.Contents.FirstOrDefault(c => c.ItemCode == testItems[0]);
        Assert.That(partialContent, Is.Not.Null, "Source partial package should contain the item");
        
        // After ProcessTargetPackageMovements, the partial source package should have reduced quantity
        Assert.That(partialContent.Quantity, Is.EqualTo(12), "Source partial package should have reduced quantity (24-12=12) after target package movements");
        Assert.That(partialContent.CommittedQuantity, Is.EqualTo(0), "Source partial package committed quantity should be 0 after ClearPickListCommitmentsAsync");
        Assert.That(partialContent.BinEntry, Is.EqualTo(settings.Filters.InitialCountingBinEntry!.Value), "Source partial package should be in original bin");

        Assert.That(sourcePartialPackage.Commitments.Count, Is.EqualTo(0), "Source partial package should have no commitments after clearing");

        // Check for removal transaction from ProcessTargetPackageMovements
        var partialRemovalTransactions = sourcePartialPackage.Transactions
            .Where(t => t.TransactionType == PackageTransactionType.Remove && 
                       t.SourceOperationType == ObjectType.PickingClosure)
            .ToList();
        Assert.That(partialRemovalTransactions.Count, Is.GreaterThan(0), "Source partial package should have removal transaction from ProcessTargetPackageMovements");

        // Validate source full package (testItems[1])
        var sourceFullPackageId = packages[testItems[1]][0];
        var sourceFullPackage = await db.Packages
        .Include(p => p.Contents)
        .Include(p => p.Commitments)
        .Include(p => p.Transactions)
        .FirstOrDefaultAsync(p => p.Id == sourceFullPackageId);

        Assert.That(sourceFullPackage, Is.Not.Null, "Source full package should exist");
        Assert.That(sourceFullPackage.Status, Is.EqualTo(PackageStatus.Closed), "Source full package should be Closed (empty) after target package movements");

        // Full package should have no contents after being emptied
        Assert.That(sourceFullPackage.Contents.Count, Is.EqualTo(0), "Source full package should have no contents after being emptied");
        Assert.That(sourceFullPackage.Commitments.Count, Is.EqualTo(0), "Source full package should have no commitments");

        // Check for removal transaction
        var fullRemovalTransactions = sourceFullPackage.Transactions
            .Where(t => t.TransactionType == PackageTransactionType.Remove && 
                       t.SourceOperationType == ObjectType.PickingClosure)
            .ToList();
        Assert.That(fullRemovalTransactions.Count, Is.GreaterThan(0), "Source full package should have removal transaction from ProcessTargetPackageMovements");

        // Validate target/new package
        var targetPackage = await db.Packages
        .Include(p => p.Contents)
        .Include(p => p.Commitments)
        .Include(p => p.Transactions)
        .FirstOrDefaultAsync(p => p.Id == packageId);

        Assert.That(targetPackage, Is.Not.Null, "Target package should exist");
        Assert.That(targetPackage.Status, Is.EqualTo(PackageStatus.Active), "Target package should be Active after ProcessTargetPackageMovements");
        Assert.That(targetPackage.BinEntry, Is.EqualTo(cancelBinEntry), "Target package should be moved to cancel bin");

        // Validate target package contents are in cancel bin
        foreach (var content in targetPackage.Contents) {
            Assert.That(content.BinEntry, Is.EqualTo(cancelBinEntry), $"Target package content {content.ItemCode} should be in cancel bin");
        }

        // Target package should have the consolidated content from source packages
        Assert.That(targetPackage.Contents.Count, Is.EqualTo(3), "Target package should have 3 items after consolidation");

        // Validate transfer was created and contains the target package
        var transfer = await db.Transfers.FirstOrDefaultAsync(t => t.Id == transferId);
        Assert.That(transfer, Is.Not.Null, "Transfer should be created");
        Assert.That(transfer.Name, Does.Contain($"Cancelación Picking {absEntry}"), "Transfer should have correct name");

        // Validate transfer contains target package in source selection
        var transferPackages = await db.TransferPackages
        .Where(tp => tp.TransferId == transferId && tp.PackageId == packageId)
        .ToListAsync();
        Assert.That(transferPackages.Count, Is.GreaterThan(0), "Target package should be added to transfer");

        // Verify all commitments are cleared
        var remainingCommitments = await db.PackageCommitments
        .Where(pc => pc.SourceOperationType == ObjectType.Picking &&
                     (pc.PackageId == sourcePartialPackageId || 
                      pc.PackageId == sourceFullPackageId ||
                      pc.TargetPackageId == packageId))
        .ToListAsync();
        Assert.That(remainingCommitments.Count, Is.EqualTo(0), "All package commitments should be cleared");

        await TestContext.Out.WriteLineAsync($"✓ Source partial package {sourcePartialPackageId}: Active with {partialContent.Quantity} remaining and CommittedQuantity = 0");
        await TestContext.Out.WriteLineAsync($"✓ Source full package {sourceFullPackageId}: Closed (empty) after target package movements");
        await TestContext.Out.WriteLineAsync($"✓ Target package {packageId}: Active and moved to cancel bin {cancelBinEntry}");
        await TestContext.Out.WriteLineAsync($"✓ Transfer {transferId}: Created with target package included");
        await TestContext.Out.WriteLineAsync($"✓ All commitments cleared successfully");
    }
}