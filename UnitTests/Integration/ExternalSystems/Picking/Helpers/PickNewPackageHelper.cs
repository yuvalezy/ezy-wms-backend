using Core.DTOs.PickList;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Integration.ExternalSystems.Shared;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.Picking.Helpers;

public static class PickNewPackageHelper {
    public async static Task<(string[] testItems, string testItemNoPackage, Dictionary<string, List<Guid>> packages, string testCustomer)> PrepareTestData(
        Adapters.CrossPlatform.SBO.Services.SboCompany sboCompany, ISettings settings,
        int goodsReceiptSeries,
        WebApplicationFactory<Program> factory) {
        var testItems = new string[3];

        // Generate new items
        var itemHelper = new CreateTestItem(sboCompany);
        testItems[0] = (await itemHelper.Execute()).ItemCode;
        testItems[1] = (await itemHelper.Execute()).ItemCode;
        testItems[2] = (await itemHelper.Execute()).ItemCode;

        var testItemNoPackage = (await itemHelper.Execute()).ItemCode;

        // Create GRPO for no Package Item
        var helper = new CreateGoodsReceipt(sboCompany, settings, goodsReceiptSeries, factory, testItemNoPackage);
        await helper.Execute();

        // Create GRPO for 3 package managed items
        helper = new CreateGoodsReceipt(sboCompany, settings, goodsReceiptSeries, factory, testItems) {
            Package = true
        };

        await helper.Execute();
        var packages = helper.CreatedPackages;

        // Get customer
        var testCustomer = await SboDataHelper.GetCustomer(sboCompany);

        return (testItems, testItemNoPackage, packages, testCustomer);
    }

    public async static Task<(int salesEntry, int absEntry)> CreateSalesOrderAndReleaseToPickingAsync(Adapters.CrossPlatform.SBO.Services.SboCompany sboCompany, int salesOrdersSeries,
        string testCustomer, string testItemNoPackage, string[] testItems) {
        var orderItems = new[] { testItemNoPackage }.Concat(testItems).ToArray();
        var helper = new CreateSalesOrder(sboCompany, salesOrdersSeries, testCustomer, orderItems);
        await helper.Execute();

        return (helper.SalesEntry, helper.AbsEntry);
    }

    public async static Task<(Guid packageId, Guid pickListPackageId)> CreatePickingNewPackageAsync(WebApplicationFactory<Program> factory, ISettings settings, int absEntry) {
        var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPickListPackageService>();
        var response = await service.CreatePackageAsync(absEntry, TestConstants.SessionInfo);
        var packageId = response.Id;
        var pickListPackageId = response.PickListPackageId!.Value;
        var stageBinEntry = settings.GetStagingBinEntry(TestConstants.Warehouse)!.Value;

        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var package = await db.Packages.FindAsync(packageId);
        Assert.That(package, Is.Not.Null);
        Assert.That(package.SourceOperationType, Is.EqualTo(ObjectType.Picking));
        Assert.That(package.SourceOperationId, Is.EqualTo(pickListPackageId));
        Assert.That(package.Status, Is.EqualTo(PackageStatus.Init));
        Assert.That(package.BinEntry, Is.EqualTo(stageBinEntry));

        var pickListPackage = await db.PickListPackages.FindAsync(pickListPackageId);
        Assert.That(pickListPackage, Is.Not.Null);
        Assert.That(pickListPackage.PackageId, Is.EqualTo(packageId));
        Assert.That(pickListPackage.BinEntry, Is.EqualTo(stageBinEntry));
        Assert.That(pickListPackage.Type, Is.EqualTo(SourceTarget.Target));
        Assert.That(pickListPackage.PickEntry, Is.Null);

        return (packageId, pickListPackageId);
    }

    public async static Task AddItemNoContainerIntoNewPackageAsync(
        WebApplicationFactory<Program> factory,
        ISettings settings,
        int absEntry,
        int salesEntry,
        string testItemNoPackage,
        Guid packageId) {
        var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPickListLineService>();
        var request = new PickListAddItemRequest {
            ID = absEntry,
            Type = 17,
            Entry = salesEntry,
            ItemCode = testItemNoPackage,
            Quantity = 1,
            BinEntry = settings.GetInitialCountingBinEntry(TestConstants.Warehouse)!.Value,
            Unit = UnitType.Dozen,
            PickEntry = 0,
            PackageId = null,
            PickingPackageId = packageId
        };

        await service.AddItem(TestConstants.SessionInfo, request);
    }

    public async static Task AddPartialFromPackageIntoNewPackageAsync(
        WebApplicationFactory<Program> factory,
        ISettings settings,
        int absEntry,
        int salesEntry,
        string[] testItems,
        Dictionary<string, List<Guid>> packages,
        Guid packageId) {
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
            BinEntry = settings.GetInitialCountingBinEntry(TestConstants.Warehouse)!.Value,
            Unit = UnitType.Dozen,
            PickEntry = 1,
            PackageId = itemPackages[0],
            PickingPackageId = packageId
        };

        await service.AddItem(TestConstants.SessionInfo, request);
    }

    public async static Task AddFullPackageIntoNewPackageAsync(
        WebApplicationFactory<Program> factory,
        ISettings settings,
        int absEntry,
        int salesEntry,
        string[] testItems,
        Dictionary<string, List<Guid>> packages,
        Guid packageId) {
        var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPickListPackageService>();
        var itemCode = testItems[1];
        var itemPackages = packages[itemCode];
        var request = new PickListAddPackageRequest {
            ID = absEntry,
            Type = 17,
            Entry = salesEntry,
            PackageId = itemPackages[0],
            BinEntry = settings.GetInitialCountingBinEntry(TestConstants.Warehouse)!.Value,
            PickingPackageId = packageId
        };

        await service.AddPackageAsync(request, TestConstants.SessionInfo);
    }

    public async static Task ValidateNewPackageContentAsync(
        WebApplicationFactory<Program> factory,
        ISettings settings,
        int absEntry,
        Guid packageId,
        string testItemNoPackage,
        string[] testItems,
        Dictionary<string, List<Guid>> packages) {
        var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        int countBinEntry = settings.GetInitialCountingBinEntry(TestConstants.Warehouse)!.Value;

        // Get the created package with its contents and commitments
        var package = await db.Packages
        .Include(p => p.Contents)
        .Include(p => p.Commitments)
        .FirstOrDefaultAsync(p => p.Id == packageId);

        Assert.That(package, Is.Not.Null, "Target package should exist");
        Assert.That(package.Status, Is.EqualTo(PackageStatus.Init), "Target package should be init");
        Assert.That(package.BinEntry, Is.EqualTo(settings.GetStagingBinEntry(TestConstants.Warehouse)!.Value), "Target package should be in staging bin");

        // Validate package contents - should have 3 items: 1 no-package item + 1 partial + 1 full package
        Assert.That(package.Contents.Count, Is.EqualTo(3), "Target package should contain 3 items");

        //Validate target package no source package (testItemNoPackage)
        var noPackageContent = package.Contents.FirstOrDefault(c => c.ItemCode == testItemNoPackage);
        Assert.That(noPackageContent, Is.Not.Null, "Target package should contain no-package item");
        Assert.That(noPackageContent.Quantity, Is.EqualTo(12), "No-package item quantity should be 12");
        Assert.That(noPackageContent.CommittedQuantity, Is.EqualTo(12), "No-package item committed quantity should equal quantity");
        Assert.That(noPackageContent.BinEntry, Is.EqualTo(countBinEntry), "No-package item should be in staging bin");

        //Validate target package partial package source (testItems[0])
        var partialContent = package.Contents.FirstOrDefault(c => c.ItemCode == testItems[0]);
        Assert.That(partialContent, Is.Not.Null, "Target package should contain partial item");
        Assert.That(partialContent.Quantity, Is.EqualTo(12), "Partial item quantity should be 12");
        Assert.That(partialContent.CommittedQuantity, Is.EqualTo(12), "Partial item committed quantity should equal quantity");
        Assert.That(partialContent.BinEntry, Is.EqualTo(countBinEntry), "Partial item should be in staging bin");

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
        Assert.That(fullContent.BinEntry, Is.EqualTo(countBinEntry), "Full package item should be in staging bin");

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
        Assert.That(targetPackageRecord.BinEntry, Is.EqualTo(settings.GetStagingBinEntry(TestConstants.Warehouse)!.Value), "Target package record should be in staging bin");

        var sourcePackageRecords = pickListPackages.Where(plp => plp.Type == SourceTarget.Source).ToList();
        Assert.That(sourcePackageRecords.Count, Is.EqualTo(2), "Should have 2 source package records");
    }
}