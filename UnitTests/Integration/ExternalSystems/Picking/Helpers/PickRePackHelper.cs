using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.Picking.Helpers;

public class PickRePackHelper(int pickEntry, WebApplicationFactory<Program> factory, int binEntry, int salesEntry, string testItem, List<Guid> packages) : IDisposable
{
    private readonly IServiceScope scope = factory.Services.CreateScope();

    public async Task PickFullAndHalfPackage()
    {
        await PickFullPackage();
        await AddHalfPackageItemToPickList();
        
        //Process
        var processService = scope.ServiceProvider.GetRequiredService<IPickListProcessService>();
        var processResponse = await processService.ProcessPickList(pickEntry, TestConstants.SessionInfo.Guid);
        Assert.That(processResponse, Is.Not.Null);
        Assert.That(processResponse.Status, Is.EqualTo(ResponseStatus.Ok), processResponse.ErrorMessage ?? "No error message");
    }

    private async Task PickFullPackage()
    {
        var fullPackages = packages[0];
        var packageService = scope.ServiceProvider.GetRequiredService<IPickListPackageService>();
        var pickListAddPackageRequest = new PickListAddPackageRequest
        {
            ID = pickEntry,
            Type = 17,
            Entry = salesEntry,
            PackageId = fullPackages,
            BinEntry = binEntry
        };

        var addPackageResponse = await packageService.AddPackageAsync(pickListAddPackageRequest, TestConstants.SessionInfo);

        Assert.That(addPackageResponse, Is.Not.Null);
        Assert.That(addPackageResponse.Status, Is.EqualTo(ResponseStatus.Ok), addPackageResponse.ErrorMessage ?? "No error message");
        Assert.That(addPackageResponse.PackageId, Is.EqualTo(fullPackages));
        Assert.That(addPackageResponse.PickListIds, Is.Not.Null);
        Assert.That(addPackageResponse.PickListIds.Length, Is.GreaterThan(0));
        Assert.That(addPackageResponse.PackageContents, Is.Not.Null);
        Assert.That(addPackageResponse.PackageContents.Count, Is.GreaterThan(0));

        var packageContent = addPackageResponse.PackageContents[0];
        Assert.That(packageContent.ItemCode, Is.EqualTo(testItem));
        Assert.That(packageContent.Quantity, Is.EqualTo(24));

        // Validate database state after full package addition
        await ValidateFullPackageDbState(fullPackages, addPackageResponse.PickListIds);
    }
    private async Task AddHalfPackageItemToPickList()
    {
        var lineService = scope.ServiceProvider.GetRequiredService<IPickListLineService>();
        var halfPackages = packages[1];
        var pickListAddItemRequest = new PickListAddItemRequest
        {
            ID = pickEntry,
            Type = 17,
            Entry = salesEntry,
            ItemCode = testItem,
            Quantity = 1,
            BinEntry = binEntry,
            Unit = UnitType.Dozen,
            PickEntry = null,
            PackageId = halfPackages
        };

        var addItemResponse = await lineService.AddItem(TestConstants.SessionInfo, pickListAddItemRequest);
        
        Assert.That(addItemResponse, Is.Not.Null);
        Assert.That(addItemResponse.Status, Is.EqualTo(ResponseStatus.Ok), addItemResponse.ErrorMessage ?? "No error message");

        // Validate database state after half package item addition
        await ValidateHalfPackageDbState(halfPackages);
    }

    private async Task ValidateFullPackageDbState(Guid packageId, Guid[] pickListIds)
    {
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();

        // 1. Validate PickList entries were created correctly
        var pickListEntries = await db.PickLists
            .Where(pl => pickListIds.Contains(pl.Id))
            .ToListAsync();

        Assert.That(pickListEntries.Count, Is.EqualTo(pickListIds.Length), "All PickList entries should be created");
        
        foreach (var pickList in pickListEntries)
        {
            Assert.That(pickList.AbsEntry, Is.EqualTo(pickEntry), "PickList should have correct AbsEntry");
            Assert.That(pickList.ItemCode, Is.EqualTo(testItem), "PickList should have correct ItemCode");
            Assert.That(pickList.Status, Is.EqualTo(ObjectStatus.Open), "PickList should be Open");
            Assert.That(pickList.SyncStatus, Is.EqualTo(SyncStatus.Pending), "PickList should be Pending sync");
            Assert.That(pickList.Unit, Is.EqualTo(UnitType.Unit), "Full package uses Unit type");
            Assert.That(pickList.BinEntry, Is.EqualTo(binEntry), "PickList should have correct bin entry");
        }

        // 2. Validate PickListPackage relationship was created
        var pickListPackage = await db.PickListPackages
            .FirstOrDefaultAsync(plp => plp.AbsEntry == pickEntry && plp.PackageId == packageId);

        Assert.That(pickListPackage, Is.Not.Null, "PickListPackage relationship should exist");
        Assert.That(pickListPackage.Type, Is.EqualTo(SourceTarget.Source), "Package should be marked as Source");
        Assert.That(pickListPackage.BinEntry, Is.EqualTo(binEntry), "PickListPackage should have correct bin entry");
        Assert.That(pickListPackage.ProcessedAt, Is.Null, "Package should not be processed yet");

        // 3. Validate PackageContent committed quantities were updated
        var packageContents = await db.PackageContents
            .Where(pc => pc.PackageId == packageId)
            .ToListAsync();

        Assert.That(packageContents.Count, Is.GreaterThan(0), "Package should have contents");
        
        foreach (var content in packageContents)
        {
            Assert.That(content.ItemCode, Is.EqualTo(testItem), "Package content should have correct item");
            Assert.That(content.CommittedQuantity, Is.EqualTo(content.Quantity), 
                $"For full package, committed quantity should equal total quantity for item {content.ItemCode}");
        }

        // 4. Validate PackageCommitments were created
        var packageCommitments = await db.PackageCommitments
            .Where(pc => pc.PackageId == packageId && 
                        pc.SourceOperationType == ObjectType.Picking &&
                        pickListIds.Contains(pc.SourceOperationId))
            .ToListAsync();

        Assert.That(packageCommitments.Count, Is.EqualTo(pickListIds.Length), 
            "Should have one commitment per PickList entry");

        foreach (var commitment in packageCommitments)
        {
            Assert.That(commitment.ItemCode, Is.EqualTo(testItem), "Commitment should have correct item");
            Assert.That(commitment.SourceOperationType, Is.EqualTo(ObjectType.Picking), 
                "Commitment should be for Picking operation");
            Assert.That(pickListIds, Contains.Item(commitment.SourceOperationId), 
                "Commitment should reference a valid PickList ID");
            
            // Find corresponding package content to validate quantity
            var correspondingContent = packageContents.FirstOrDefault(pc => pc.ItemCode == commitment.ItemCode);
            Assert.That(correspondingContent, Is.Not.Null, $"Should find package content for item {commitment.ItemCode}");
            Assert.That(commitment.Quantity, Is.EqualTo(correspondingContent.Quantity), 
                "Commitment quantity should match package content quantity");
        }

        // 5. Validate Package status remains Active
        var package = await db.Packages.FindAsync(packageId);
        Assert.That(package, Is.Not.Null, "Package should exist");
        Assert.That(package.Status, Is.EqualTo(PackageStatus.Active), "Package should remain Active after full commitment");
    }

    private async Task ValidateHalfPackageDbState(Guid packageId)
    {
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();

        // 1. Validate PickList entry was created for partial package
        var pickListEntries = await db.PickLists
            .Where(pl => pl.AbsEntry == pickEntry && pl.ItemCode == testItem)
            .OrderBy(pl => pl.CreatedAt)
            .ToListAsync();

        // Should have at least one entry from the half package addition
        var halfPackagePickList = pickListEntries.LastOrDefault();
        Assert.That(halfPackagePickList, Is.Not.Null, "Should have PickList entry for half package");
        Assert.That(halfPackagePickList.Unit, Is.EqualTo(UnitType.Dozen), "Half package uses Dozen unit type");
        Assert.That(halfPackagePickList.Quantity, Is.EqualTo(12), "Half package should pick 1 dozen = 12 units");
        Assert.That(halfPackagePickList.Status, Is.EqualTo(ObjectStatus.Open), "PickList should be Open");
        Assert.That(halfPackagePickList.SyncStatus, Is.EqualTo(SyncStatus.Pending), "PickList should be Pending sync");

        // 2. Validate PickListPackage relationship exists (may be created or updated)
        var pickListPackage = await db.PickListPackages
            .FirstOrDefaultAsync(plp => plp.AbsEntry == pickEntry && plp.PackageId == packageId);

        Assert.That(pickListPackage, Is.Not.Null, "PickListPackage relationship should exist for partial package");
        Assert.That(pickListPackage.Type, Is.EqualTo(SourceTarget.Source), "Package should be marked as Source");

        // 3. Validate PackageContent committed quantity was updated for partial commitment
        var packageContent = await db.PackageContents
            .FirstOrDefaultAsync(pc => pc.PackageId == packageId && pc.ItemCode == testItem);

        Assert.That(packageContent, Is.Not.Null, "Package content should exist");
        Assert.That(packageContent.CommittedQuantity, Is.EqualTo(12), 
            "Committed quantity should be 12 for half package (1 dozen)");
        Assert.That(packageContent.CommittedQuantity, Is.LessThan(packageContent.Quantity), 
            "For partial package, committed quantity should be less than total quantity");

        // 4. Validate PackageCommitment was created for partial package
        var packageCommitment = await db.PackageCommitments
            .FirstOrDefaultAsync(pc => pc.PackageId == packageId && 
                                      pc.SourceOperationType == ObjectType.Picking &&
                                      pc.SourceOperationId == halfPackagePickList.Id);

        Assert.That(packageCommitment, Is.Not.Null, "Should have commitment for partial package");
        Assert.That(packageCommitment.ItemCode, Is.EqualTo(testItem), "Commitment should have correct item");
        Assert.That(packageCommitment.Quantity, Is.EqualTo(12), "Commitment should be for 12 units (1 dozen)");
        Assert.That(packageCommitment.SourceOperationType, Is.EqualTo(ObjectType.Picking), 
            "Commitment should be for Picking operation");

        // 5. Validate Package status remains Active (partial commitment doesn't close it)
        var package = await db.Packages.FindAsync(packageId);
        Assert.That(package, Is.Not.Null, "Package should exist");
        Assert.That(package.Status, Is.EqualTo(PackageStatus.Active), "Package should remain Active after partial commitment");

        // 6. Validate available quantity calculation
        var availableQuantity = packageContent.Quantity - packageContent.CommittedQuantity;
        Assert.That(availableQuantity, Is.GreaterThan(0), "Should have remaining available quantity in package");
        Assert.That(availableQuantity, Is.EqualTo(packageContent.Quantity - 12), 
            "Available quantity should be total minus committed (12)");
    }

    public void Dispose()
    {
        scope.Dispose();
        factory.Dispose();
    }
}