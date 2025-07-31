using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Integration.ExternalSystems.Picking.Helpers;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.Picking;

[TestFixture]
public class PickingCancellationNewPackage : BaseExternalTest {
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
        var pickingCancel = scope.ServiceProvider.GetRequiredService<IPickListCancelService>();
        var response = await pickingCancel.CancelPickListAsync(absEntry, TestConstants.SessionInfo);
        Assert.That(response.Status, Is.EqualTo(ResponseStatus.Ok), response.ErrorMessage ?? "Unknown error occured");
        Assert.That(response.TransferId, Is.Not.Null, "TransferId should be returned");
        transferId = response.TransferId.Value;
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