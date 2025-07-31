using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
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

    private Dictionary<string, List<Guid>> packages;
    private Guid packageId;
    private Guid pickListPackageId;

    [Test]
    [Order(0)]
    public async Task PrepareData() {
        var result = await PickNewPackageHelper.PrepareTestData(sboCompany, settings, goodsReceiptSeries, factory);
        testItems[0] = result.testItems[0];
        testItems[1] = result.testItems[1];
        testItems[2] = result.testItems[2];
        testItemNoPackage = result.testItemNoPackage;
        packages = result.packages;
        testCustomer = result.testCustomer;
        await TestContext.Out.WriteLineAsync($"Test customer: {testCustomer}");
    }

    [Test]
    [Order(1)]
    public async Task CreateSaleOrder_ReleaseToPicking() {
        var result = await PickNewPackageHelper.CreateSalesOrderAndReleaseToPickingAsync(sboCompany, salesOrdersSeries, testCustomer, testItemNoPackage, testItems);
        salesEntry = result.salesEntry;
        absEntry = result.absEntry;
        await TestContext.Out.WriteLineAsync($"Created sales order with DocEntry: {salesEntry}");
        Assert.That(salesEntry, Is.Not.EqualTo(-1), "Sales Entry should be created");
        Assert.That(absEntry, Is.Not.EqualTo(-1), "Pick Entry should be created");
    }

    [Test]
    [Order(2)]
    public async Task CreatePicking_NewPackage() {
        var result = await PickNewPackageHelper.CreatePickingNewPackageAsync(factory, settings, absEntry);
        packageId = result.packageId;
        pickListPackageId = result.pickListPackageId;
    }

    [Test]
    [Order(3)]
    public async Task AddItemNoContainer_IntoNewPackage() {
        await PickNewPackageHelper.AddItemNoContainerIntoNewPackageAsync(factory, settings, absEntry, salesEntry, testItemNoPackage, packageId);
    }

    [Test]
    [Order(4)]
    public async Task AddPartialFromPackage_IntoNewPackage() {
        await PickNewPackageHelper.AddPartialFromPackageIntoNewPackageAsync(factory, settings, absEntry, salesEntry, testItems, packages, packageId);
    }

    [Test]
    [Order(5)]
    public async Task AddFullPackage_IntoNewPackage() {
        await PickNewPackageHelper.AddFullPackageIntoNewPackageAsync(factory, settings, absEntry, salesEntry, testItems, packages, packageId);
    }

    [Test]
    [Order(6)]
    public async Task Validate_NewPackageContent() {
        await PickNewPackageHelper.ValidateNewPackageContentAsync(factory, settings, absEntry, packageId, testItemNoPackage, testItems, packages);
    }

    [Test]
    [Order(7)]
    public async Task Process_AssertPackagesMovements() {
        var scope = factory.Services.CreateScope();
        var pickingProcess = scope.ServiceProvider.GetRequiredService<IPickListProcessService>();
        await pickingProcess.ProcessPickList(absEntry, TestConstants.SessionInfo.Guid);
        var deliveryNote = new CreateDeliveryNote(sboCompany, absEntry, deliveryNoteSeries, testCustomer);
        await deliveryNote.Execute();
        var pickListDetailService = scope.ServiceProvider.GetRequiredService<IPickListDetailService>();
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
        Assert.That(partialSourcePackage.Status, Is.EqualTo(PackageStatus.Active),
            $"Partial source package {partialSourcePackage.Barcode} should still be active, it's status is {partialSourcePackage.Status}");

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

        // Package contents should be removed when quantity reaches 0
        Assert.That(fullSourcePackage.Contents.Count, Is.EqualTo(0), "Full source package should have no contents after being emptied");

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
        Assert.That(targetPackage.Status, Is.EqualTo(PackageStatus.Closed), "Target package should be Closed after delivery note processing");
        Assert.That(targetPackage.BinEntry, Is.EqualTo(settings.Filters.StagingBinEntry!.Value), "Target package should be in staging bin");

        // Validate package contents - should be empty (contents removed when quantity reaches 0)
        Assert.That(targetPackage.Contents.Count, Is.EqualTo(0), "Target package should have no contents after delivery");

        // Validate transaction audit trail - should show both incoming and outgoing movements
        // Check for incoming transactions from source packages during picking closure
        var addTransactions = targetPackage.Transactions
        .Where(t => t.TransactionType == PackageTransactionType.Add &&
                    t.SourceOperationType == ObjectType.Picking)
        .ToList();

        Assert.That(addTransactions.Count, Is.EqualTo(3), "Should have 3 add transactions (no-package + partial + full)");

        // Check for outgoing transactions from delivery note processing
        var removeTransactions = targetPackage.Transactions
        .Where(t => t.TransactionType == PackageTransactionType.Remove &&
                    t.SourceOperationType == ObjectType.PickingClosure &&
                    t.Notes.Contains("Delivery"))
        .ToList();

        Assert.That(removeTransactions.Count, Is.EqualTo(3), "Should have 3 removal transactions for delivery");

        // Validate specific item transactions
        var noPackageAddTransaction = addTransactions.FirstOrDefault(t => t.ItemCode == testItemNoPackage);
        Assert.That(noPackageAddTransaction, Is.Not.Null, "Should have add transaction for no-package item");
        Assert.That(noPackageAddTransaction.Quantity, Is.EqualTo(12), "No-package add transaction should be +12");

        var noPackageRemovalTransaction = removeTransactions.FirstOrDefault(t => t.ItemCode == testItemNoPackage);
        Assert.That(noPackageRemovalTransaction, Is.Not.Null, "Should have removal transaction for no-package item");
        Assert.That(noPackageRemovalTransaction.Quantity, Is.EqualTo(-12), "No-package removal transaction should be -12");

        var partialAddTransaction = addTransactions.FirstOrDefault(t => t.ItemCode == testItems[0]);
        Assert.That(partialAddTransaction, Is.Not.Null, "Should have add transaction for partial item");
        Assert.That(partialAddTransaction.Quantity, Is.EqualTo(12), "Partial add transaction should be +12");

        var partialRemovalTransaction = removeTransactions.FirstOrDefault(t => t.ItemCode == testItems[0]);
        Assert.That(partialRemovalTransaction, Is.Not.Null, "Should have removal transaction for partial item");
        Assert.That(partialRemovalTransaction.Quantity, Is.EqualTo(-12), "Partial removal transaction should be -12");

        var fullAddTransaction = addTransactions.FirstOrDefault(t => t.ItemCode == testItems[1]);
        Assert.That(fullAddTransaction, Is.Not.Null, "Should have add transaction for full item");
        Assert.That(fullAddTransaction.Quantity, Is.EqualTo(24), "Full add transaction should be +24");

        var fullRemovalTransaction = removeTransactions.FirstOrDefault(t => t.ItemCode == testItems[1]);
        Assert.That(fullRemovalTransaction, Is.Not.Null, "Should have removal transaction for full item");
        Assert.That(fullRemovalTransaction.Quantity, Is.EqualTo(-24), "Full removal transaction should be -24");

        // Validate PickListPackage record for target package
        var targetPickListPackage = await db.PickListPackages
        .FirstOrDefaultAsync(plp => plp.PackageId == packageId && plp.Type == SourceTarget.Target);

        Assert.That(targetPickListPackage, Is.Not.Null, "Target PickListPackage record should exist");
        Assert.That(targetPickListPackage.AbsEntry, Is.EqualTo(absEntry), "Target PickListPackage should reference correct pick list");
        Assert.That(targetPickListPackage.ProcessedAt, Is.Not.Null, "Target PickListPackage should be marked as processed");

        // Validate net transaction totals (add - remove should equal 0 for each item)
        var noPackageNetQuantity = addTransactions.Where(t => t.ItemCode == testItemNoPackage).Sum(t => t.Quantity) +
                                   removeTransactions.Where(t => t.ItemCode == testItemNoPackage).Sum(t => t.Quantity);

        var partialNetQuantity = addTransactions.Where(t => t.ItemCode == testItems[0]).Sum(t => t.Quantity) +
                                 removeTransactions.Where(t => t.ItemCode == testItems[0]).Sum(t => t.Quantity);

        var fullNetQuantity = addTransactions.Where(t => t.ItemCode == testItems[1]).Sum(t => t.Quantity) +
                              removeTransactions.Where(t => t.ItemCode == testItems[1]).Sum(t => t.Quantity);

        Assert.That(noPackageNetQuantity, Is.EqualTo(0), "Net quantity for no-package item should be 0 (12 added, -12 removed)");
        Assert.That(partialNetQuantity, Is.EqualTo(0), "Net quantity for partial item should be 0 (12 added, -12 removed)");
        Assert.That(fullNetQuantity, Is.EqualTo(0), "Net quantity for full item should be 0 (24 added, -24 removed)");

        // Validate all pick list commitments are cleared
        var remainingPickListCommitments = await db.PackageCommitments
        .Where(pc => pc.SourceOperationType == ObjectType.Picking)
        .ToListAsync();

        // Filter to only commitments related to our pick list
        var pickListIds = await db.PickLists
        .Where(p => p.AbsEntry == absEntry)
        .Select(p => p.Id)
        .ToListAsync();

        var ourPickListCommitments = remainingPickListCommitments
        .Where(pc => pickListIds.Contains(pc.SourceOperationId))
        .ToList();

        Assert.That(ourPickListCommitments.Count, Is.EqualTo(0), "All pick list commitments should be cleared after delivery closure");

        // Validate package barcode exists and follows expected pattern
        Assert.That(targetPackage.Barcode, Is.Not.Null.And.Not.Empty, "Target package should have a barcode");
        Assert.That(targetPackage.UpdatedAt, Is.Not.Null, "Target package should have been updated during processing");
    }
}