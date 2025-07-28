using Core.DTOs.Items;
using Core.DTOs.Package;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Integration.ExternalSystems.Picking.Helpers;
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
        var pickingProcess = scope.ServiceProvider.GetRequiredService<IPickListProcessService>();
        await pickingProcess.ProcessPickList(absEntry, TestConstants.SessionInfo.Guid);
        var deliveryNote = new CreateDeliveryNote(sboCompany, absEntry, deliveryNoteSeries, testCustomer);
        await deliveryNote.Execute();
        deliveryNoteEntry = deliveryNote.DeliveryEntry;
        var pickListDetailService = scope.ServiceProvider.GetRequiredService<PickListDetailService>();
        await pickListDetailService.ProcessClosedPickListsWithPackages();
    }

    [Test]
    [Order(8)]
    public async Task Validate_SourcePackages_AfterDeliveryNoteCreated_PickListFinished() {
        var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        
        // Validate partial source package (testItems[0]) - should have reduced quantity
        var partialSourcePackageId = packages[testItems[0]][0];
        var partialSourcePackage = await db.Packages
            .Include(p => p.Contents)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == partialSourcePackageId);
        
        Assert.That(partialSourcePackage, Is.Not.Null, "Partial source package should exist");
        Assert.That(partialSourcePackage.Status, Is.EqualTo(PackageStatus.Active), $"Partial source package {partialSourcePackage.Barcode} should still be active, it's status is {partialSourcePackage.Status}");
        
        var partialSourceContent = partialSourcePackage.Contents.FirstOrDefault(c => c.ItemCode == testItems[0]);
        Assert.That(partialSourceContent, Is.Not.Null, "Partial source package should contain the item");
        // Original quantity was 24, we picked 12, so 12 should remain
        Assert.That(partialSourceContent.Quantity, Is.EqualTo(12), "Partial source package should have reduced quantity (24-12=12)");
        Assert.That(partialSourceContent.CommittedQuantity, Is.EqualTo(0), "Partial source package should have no remaining commitments");
        
        // Check audit trail for partial source package - should have removal transaction
        var partialRemovalTransaction = partialSourcePackage.Transactions
            .FirstOrDefault(t => t.ItemCode == testItems[0] && 
                                t.TransactionType == PackageTransactionType.Remove &&
                                t.SourceOperationType == ObjectType.PickingClosure);
        Assert.That(partialRemovalTransaction, Is.Not.Null, "Partial source package should have removal transaction");
        Assert.That(partialRemovalTransaction.Quantity, Is.EqualTo(-12), "Removal transaction should show -12 quantity");
        Assert.That(partialRemovalTransaction.Notes, Does.Contain("Moved to target package"), "Removal transaction should indicate movement to target package");
        
        // Validate full source package (testItems[1]) - should be closed and empty
        var fullSourcePackageId = packages[testItems[1]][0];
        var fullSourcePackage = await db.Packages
            .Include(p => p.Contents)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == fullSourcePackageId);
        
        Assert.That(fullSourcePackage, Is.Not.Null, "Full source package should exist");
        Assert.That(fullSourcePackage.Status, Is.EqualTo(PackageStatus.Closed), "Full source package should be closed (empty)");
        
        var fullSourceContent = fullSourcePackage.Contents.FirstOrDefault(c => c.ItemCode == testItems[1]);
        Assert.That(fullSourceContent, Is.Not.Null, "Full source package should contain the item record");
        Assert.That(fullSourceContent.Quantity, Is.EqualTo(0), "Full source package should have zero quantity");
        Assert.That(fullSourceContent.CommittedQuantity, Is.EqualTo(0), "Full source package should have no remaining commitments");
        
        // Check audit trail for full source package - should have removal transaction
        var fullRemovalTransaction = fullSourcePackage.Transactions
            .FirstOrDefault(t => t.ItemCode == testItems[1] && 
                                t.TransactionType == PackageTransactionType.Remove &&
                                t.SourceOperationType == ObjectType.PickingClosure);
        Assert.That(fullRemovalTransaction, Is.Not.Null, "Full source package should have removal transaction");
        Assert.That(fullRemovalTransaction.Quantity, Is.EqualTo(-24), "Removal transaction should show -24 quantity (full package)");
        Assert.That(fullRemovalTransaction.Notes, Does.Contain("Moved to target package"), "Removal transaction should indicate movement to target package");
        
        // Validate that all package commitments are cleared
        var remainingCommitments = await db.PackageCommitments
            .Where(pc => pc.SourceOperationType == ObjectType.Picking &&
                         (pc.PackageId == partialSourcePackageId || pc.PackageId == fullSourcePackageId))
            .ToListAsync();
        
        Assert.That(remainingCommitments.Count, Is.EqualTo(0), "All package commitments should be cleared after processing");
        
        // Validate PickListPackage records are marked as processed
        var pickListPackages = await db.PickListPackages
            .Where(plp => plp.AbsEntry == absEntry)
            .ToListAsync();
        
        Assert.That(pickListPackages.All(plp => plp.ProcessedAt != null), Is.True, "All PickListPackage records should be marked as processed");
    }

    [Test]
    [Order(9)]
    public async Task Validate_TargetPackage_AfterDeliveryNoteCreated_PickListFinished() {
        var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        
        // Get the target package with its contents and transactions
        var targetPackage = await db.Packages
            .Include(p => p.Contents)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == packageId);
        
        Assert.That(targetPackage, Is.Not.Null, "Target package should exist");
        Assert.That(targetPackage.Status, Is.EqualTo(PackageStatus.Closed), "Target package should be Closed after processing");
        Assert.That(targetPackage.BinEntry, Is.EqualTo(settings.Filters.StagingBinEntry!.Value), "Target package should be in staging bin");
        
        // Validate package contents - should still have 3 items with same quantities
        Assert.That(targetPackage.Contents.Count, Is.EqualTo(3), "Target package should contain 3 items");
        
        // Validate no-package item (testItemNoPackage) - should remain unchanged
        var noPackageContent = targetPackage.Contents.FirstOrDefault(c => c.ItemCode == testItemNoPackage);
        Assert.That(noPackageContent, Is.Not.Null, "Target package should contain no-package item");
        Assert.That(noPackageContent.Quantity, Is.EqualTo(12), "No-package item quantity should remain 12");
        Assert.That(noPackageContent.CommittedQuantity, Is.EqualTo(0), "No-package item should have no commitments");
        
        // Check audit trail for no-package item - should have original add transaction
        var noPackageAddTransaction = targetPackage.Transactions
            .FirstOrDefault(t => t.ItemCode == testItemNoPackage && 
                                t.TransactionType == PackageTransactionType.Add &&
                                t.SourceOperationType == ObjectType.PickingClosure);
        Assert.That(noPackageAddTransaction, Is.Not.Null, "No-package item should have add transaction from closure");
        Assert.That(noPackageAddTransaction.Quantity, Is.EqualTo(12), "Add transaction should show +12 quantity");
        Assert.That(noPackageAddTransaction.Notes, Does.Not.Contain("source package"), "No-package item should not reference source package");
        
        // Validate partial package item (testItems[0]) - moved from source package
        var partialContent = targetPackage.Contents.FirstOrDefault(c => c.ItemCode == testItems[0]);
        Assert.That(partialContent, Is.Not.Null, "Target package should contain partial item");
        Assert.That(partialContent.Quantity, Is.EqualTo(12), "Partial item quantity should be 12");
        Assert.That(partialContent.CommittedQuantity, Is.EqualTo(0), "Partial item should have no commitments");
        
        // Check audit trail for partial item - should have add transaction from source package
        var partialAddTransaction = targetPackage.Transactions
            .FirstOrDefault(t => t.ItemCode == testItems[0] && 
                                t.TransactionType == PackageTransactionType.Add &&
                                t.SourceOperationType == ObjectType.PickingClosure);
        Assert.That(partialAddTransaction, Is.Not.Null, "Partial item should have add transaction from source package");
        Assert.That(partialAddTransaction.Quantity, Is.EqualTo(12), "Add transaction should show +12 quantity");
        Assert.That(partialAddTransaction.Notes, Does.Contain("Moved from source package"), "Add transaction should reference source package");
        
        // Validate full package item (testItems[1]) - moved from source package
        var fullContent = targetPackage.Contents.FirstOrDefault(c => c.ItemCode == testItems[1]);
        Assert.That(fullContent, Is.Not.Null, "Target package should contain full package item");
        Assert.That(fullContent.Quantity, Is.EqualTo(24), "Full package item quantity should be 24");
        Assert.That(fullContent.CommittedQuantity, Is.EqualTo(0), "Full package item should have no commitments");
        
        // Check audit trail for full package item - should have add transaction from source package
        var fullAddTransaction = targetPackage.Transactions
            .FirstOrDefault(t => t.ItemCode == testItems[1] && 
                                t.TransactionType == PackageTransactionType.Add &&
                                t.SourceOperationType == ObjectType.PickingClosure);
        Assert.That(fullAddTransaction, Is.Not.Null, "Full package item should have add transaction from source package");
        Assert.That(fullAddTransaction.Quantity, Is.EqualTo(24), "Add transaction should show +24 quantity");
        Assert.That(fullAddTransaction.Notes, Does.Contain("Moved from source package"), "Add transaction should reference source package");
        
        // Validate PickListPackage record for target package
        var targetPickListPackage = await db.PickListPackages
            .FirstOrDefaultAsync(plp => plp.PackageId == packageId && plp.Type == SourceTarget.Target);
        
        Assert.That(targetPickListPackage, Is.Not.Null, "Target PickListPackage record should exist");
        Assert.That(targetPickListPackage.AbsEntry, Is.EqualTo(absEntry), "Target PickListPackage should reference correct pick list");
        Assert.That(targetPickListPackage.ProcessedAt, Is.Not.Null, "Target PickListPackage should be marked as processed");
        
        // Validate total package contents match expected picking results
        var totalNoPackageQuantity = targetPackage.Contents.Where(c => c.ItemCode == testItemNoPackage).Sum(c => c.Quantity);
        var totalPartialQuantity = targetPackage.Contents.Where(c => c.ItemCode == testItems[0]).Sum(c => c.Quantity);
        var totalFullQuantity = targetPackage.Contents.Where(c => c.ItemCode == testItems[1]).Sum(c => c.Quantity);
        
        Assert.That(totalNoPackageQuantity, Is.EqualTo(12), "Total no-package quantity should be 12");
        Assert.That(totalPartialQuantity, Is.EqualTo(12), "Total partial quantity should be 12");
        Assert.That(totalFullQuantity, Is.EqualTo(24), "Total full package quantity should be 24");
        
        // Validate no remaining commitments for target package
        var targetCommitments = await db.PackageCommitments
            .Where(pc => pc.TargetPackageId == packageId)
            .ToListAsync();
        
        Assert.That(targetCommitments.Count, Is.EqualTo(0), "Target package should have no remaining commitments");
        
        // Validate package barcode exists and follows expected pattern
        Assert.That(targetPackage.Barcode, Is.Not.Null.And.Not.Empty, "Target package should have a barcode");
        Assert.That(targetPackage.UpdatedAt, Is.Not.Null, "Target package should have been updated during processing");
    }
}