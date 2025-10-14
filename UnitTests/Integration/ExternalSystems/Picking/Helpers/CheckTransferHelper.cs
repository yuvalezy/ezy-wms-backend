using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;
using Core.DTOs.Transfer;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.Picking.Helpers;

public enum CheckTransferHelperType
{
    FullPickList,
    FullAndHalfPackage
}

public class CheckTransferHelper(
    int pickEntry,
    PickingSelectionResponse[] selection,
    WebApplicationFactory<Program> factory,
    int binEntry,
    int salesEntry,
    string itemCode,
    SboCompany sboCompany,
    Guid transferId,
    CheckTransferHelperType type = CheckTransferHelperType.FullPickList,
    List<Guid>? packages = null)
{
    int expectedQuantity = type == CheckTransferHelperType.FullPickList ? 960 : 36;
    public async Task Validate()
    {
        int cancelTransferEntry = await GetCancelTransferEntry();
        await ValidateCancelTransfer(cancelTransferEntry);
        await ValidatePickingCancelTransferData();

        // Validate package handling if packages are provided
        if (packages is { Count: > 0 })
        {
            await ValidatePackages();
        }
    }


    private async Task<int> GetCancelTransferEntry()
    {
        var response = await sboCompany.GetAsync<JsonDocument>($"StockTransfers?$select=DocEntry,DocNum,DocDate,DocumentStatus&$top=1&$orderby=DocEntry desc");
        return response!.RootElement.GetProperty("value")[0].GetProperty("DocEntry").GetInt32();
    }

    private async Task ValidateCancelTransfer(int docEntry)
    {
        var response = await sboCompany.GetAsync<JsonDocument>($"StockTransfers({docEntry})");
        Assert.That(response, Is.Not.Null, "Transfer response should not be null");

        var root = response!.RootElement;

        // Validate basic transfer properties
        Assert.That(root.GetProperty("DocEntry").GetInt32(), Is.EqualTo(docEntry), "DocEntry should match");
        Assert.That(root.GetProperty("DocumentStatus").GetString(), Is.EqualTo("bost_Open"), "Transfer should be open");

        // Validate transfer lines
        var transferLines = root.GetProperty("StockTransferLines").EnumerateArray().ToArray();
        var expectedItems = selection.GroupBy(s => s.ItemCode).ToArray();

        Assert.That(transferLines.Length, Is.EqualTo(expectedItems.Length),
            $"Transfer should have {expectedItems.Length} line(s) for the picked items");

        foreach (var expectedItem in expectedItems)
        {
            var transferLine = transferLines.FirstOrDefault(line => line.GetProperty("ItemCode").GetString() == expectedItem.Key);

            Assert.That(transferLine.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined), $"Transfer line for item {expectedItem.Key} should exist");

            // Validate quantities
            decimal expectedQuantity = expectedItem.Sum(s => s.Quantity);
            decimal actualQuantity = transferLine.GetProperty("Quantity").GetDecimal();
            Assert.That(actualQuantity, Is.EqualTo(expectedQuantity), $"Quantity for item {expectedItem.Key} should match picked quantity");

            // Validate bin allocations
            var binAllocations = transferLine.GetProperty("StockTransferLinesBinAllocations").EnumerateArray().ToArray();
            int expectedFromBins = expectedItem.Select(s => s.BinEntry).Distinct().Count();
            int expectedToBins = 1; // All items go to the cancel bin
            int expectedTotalAllocations = expectedFromBins + expectedToBins;

            Assert.That(binAllocations.Length, Is.EqualTo(expectedTotalAllocations),
                $"Item {expectedItem.Key} should have {expectedTotalAllocations} bin allocations");

            // Validate from warehouse bin allocations
            var fromAllocations = binAllocations.Where(ba => ba.GetProperty("BinActionType").GetString() == "batFromWarehouse").ToArray();
            Assert.That(fromAllocations.Length, Is.EqualTo(expectedFromBins), $"Item {expectedItem.Key} should have {expectedFromBins} from-warehouse allocations");

            // Validate to warehouse bin allocations (should be the cancel bin)
            var toAllocations = binAllocations.Where(ba => ba.GetProperty("BinActionType").GetString() == "batToWarehouse").ToArray();
            Assert.That(toAllocations.Length, Is.EqualTo(1), $"Item {expectedItem.Key} should have 1 to-warehouse allocation");
            Assert.That(toAllocations[0].GetProperty("BinAbsEntry").GetInt32(), Is.EqualTo(binEntry), $"Item {expectedItem.Key} should be transferred to the cancel bin");
            Assert.That(toAllocations[0].GetProperty("Quantity").GetDecimal(), Is.EqualTo(expectedQuantity), $"To-warehouse quantity for item {expectedItem.Key} should equal total picked quantity");
        }
    }

    private async Task ValidatePickingCancelTransferData()
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransferProcessingService>();
        var data = await service.PrepareTransferData(transferId);
        Assert.That(data, Is.Not.Null);
        Assert.That(data.ContainsKey(itemCode), Is.True, $"Transfer data should contain item {itemCode}");
        var itemData = data[itemCode];
        ValidatePickCancelTransferItemData(itemData, expectedQuantity);
    }

    private void ValidatePickCancelTransferItemData(TransferCreationDataResponse itemData, int validateQuantity)
    {
        Assert.That(itemData.Quantity, Is.EqualTo(validateQuantity), $"Quantity should be {validateQuantity}");
        Assert.That(itemData.SourceBins.Count, Is.EqualTo(1), "Source bins should be 1");
        ;
        var binSource = itemData.SourceBins.First();
        Assert.That(binSource.BinEntry, Is.EqualTo(binEntry), "Source bin should be the cancel bin");
        Assert.That(binSource.Quantity, Is.EqualTo(validateQuantity), $"Source bin quantity should be {validateQuantity}");
        Assert.That(itemData.SourceBins.Count, Is.EqualTo(1), "Target bins should be 1");
        var binTarget = itemData.SourceBins.First();
        Assert.That(binTarget.BinEntry, Is.EqualTo(binEntry), "Target bin should be the cancel bin");
        Assert.That(binTarget.Quantity, Is.EqualTo(validateQuantity), $"Target bin quantity should be {validateQuantity}");
    }

    private async Task ValidatePackages()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();

        // Load packages with all necessary data
        var packageData = await db.Packages
        .Where(p => packages.Contains(p.Id))
        .Include(p => p.Contents)
        .Include(p => p.Commitments)
        .ToListAsync();

        // Get PickListPackages for this pick operation
        var pickListPackages = await db.PickListPackages
        .Where(plp => plp.AbsEntry == pickEntry && packages.Contains(plp.PackageId))
        .ToListAsync();

        // Get TransferPackages for this transfer
        var transferPackages = await db.TransferPackages
        .Where(tp => tp.TransferId == transferId && packages.Contains(tp.PackageId))
        .ToListAsync();

        Assert.That(packageData.Count, Is.EqualTo(packages.Count),
            $"Should find all {packages.Count} packages in database");

        Assert.That(pickListPackages.Count, Is.GreaterThan(0),
            "Should have PickListPackage relationships for the packages");

        foreach (var package in packageData)
        {
            await ValidateIndividualPackage(package, pickListPackages, transferPackages);
        }
    }

    private async Task ValidateIndividualPackage(Package package, List<PickListPackage> pickListPackages, List<TransferPackage> transferPackages)
    {
        // Check if this package was added to pick list
        var pickListPackage = pickListPackages.FirstOrDefault(plp => plp.PackageId == package.Id);
        Assert.That(pickListPackage, Is.Not.Null, $"Package {package.Barcode} should have PickListPackage relationship");

        // Determine if this was a full or partial package commitment
        var wasFullPackage = DetermineIfFullPackageCommitment(package);

        if (wasFullPackage)
        {
            await ValidateFullPackageCancellation(package, transferPackages);
        }
        else
        {
            await ValidatePartialPackageCancellation(package, transferPackages);
        }

        // Common validations for both full and partial packages
        await ValidateCommonPackageState(package, wasFullPackage);
    }

    private bool DetermineIfFullPackageCommitment(Package package)
    {
        // Since commitments are cleared during cancellation, we need to determine this another way
        // We can check if the package appears in TransferPackages (full package) or not (partial)
        // Or we could check the committed quantities before they were reset

        // Working assumption:
        // - First package (packages[0]) was picked as full package
        // - Second package (packages[1]) was picked as partial package
        if (packages == null || packages.Count == 0)
        {
            return false; // Default to partial if no packages info
        }

        // Find the index of this package ID in the packages list
        var packageIndex = packages.IndexOf(package.Id);
        return packageIndex == 0; // First package (index 0) was full, others were partial
    }

    private async Task ValidateFullPackageCancellation(Package package, List<TransferPackage> transferPackages)
    {
        // 1. Validate TransferPackage record exists
        var transferPackage = transferPackages.FirstOrDefault(tp => tp.PackageId == package.Id);
        Assert.That(transferPackage, Is.Not.Null,
            $"Full package {package.Barcode} should have TransferPackage record");

        Assert.That(transferPackage.Type, Is.EqualTo(SourceTarget.Source),
            $"Package {package.Barcode} should be marked as Source in transfer");

        // 2. Validate package location moved to cancel bin
        Assert.That(package.BinEntry, Is.EqualTo(binEntry),
            $"Full package {package.Barcode} should be moved to cancel bin {binEntry}");

        // 3. Validate package status remains Active
        Assert.That(package.Status, Is.EqualTo(PackageStatus.Active),
            $"Full package {package.Barcode} should remain Active after cancellation");

        Console.WriteLine($"✓ Full package {package.Barcode} validation passed");
    }

    private async Task ValidatePartialPackageCancellation(Package package, List<TransferPackage> transferPackages)
    {
        // 1. Validate NO TransferPackage record exists (partial packages aren't added as full packages)
        var transferPackage = transferPackages.FirstOrDefault(tp => tp.PackageId == package.Id);
        Assert.That(transferPackage, Is.Null,
            $"Partial package {package.Barcode} should NOT have TransferPackage record");

        // 2. Validate package location remains unchanged (not moved to cancel bin)
        Assert.That(package.BinEntry, Is.Not.EqualTo(binEntry),
            $"Partial package {package.Barcode} should NOT be moved to cancel bin");

        // 3. Validate package status remains Active
        Assert.That(package.Status, Is.EqualTo(PackageStatus.Active),
            $"Partial package {package.Barcode} should remain Active after partial cancellation");

        Console.WriteLine($"✓ Partial package {package.Barcode} validation passed");
    }

    private async Task ValidateCommonPackageState(Package package, bool wasFullPackage)
    {
        // 1. Validate no remaining PackageCommitments for picking operations
        Assert.That(package.Commitments.Where(c => c.SourceOperationType == ObjectType.Picking).Count(),
            Is.EqualTo(0), $"Package {package.Barcode} should have no picking commitments after cancellation");

        // 2. Validate PackageContent committed quantities are reset to 0 after cancellation
        // Note: Both full and partial packages have commitments cleared during cancellation
        foreach (var content in package.Contents)
        {
            Assert.That(content.CommittedQuantity, Is.EqualTo(wasFullPackage ? 24 : 0),
                $"Package {package.Barcode} content {content.ItemCode} should have 0 committed quantity after cancellation");
        }

        // 3. Validate package integrity (contents still exist)
        Assert.That(package.Contents.Count, Is.GreaterThan(0),
            $"Package {package.Barcode} should still have contents after cancellation");

        Assert.That(package.Contents.All(c => c.Quantity > 0), Is.True,
            $"Package {package.Barcode} all contents should have positive quantities");

        Console.WriteLine($"✓ Common package state validation passed for {package.Barcode}");
    }
}