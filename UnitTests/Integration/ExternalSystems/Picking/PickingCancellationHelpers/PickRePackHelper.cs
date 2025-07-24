using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.Picking.PickingCancellationHelpers;

public class PickRePackHelper(int pickEntry, WebApplicationFactory<Program> factory, int binEntry, int salesEntry, string testItem, List<Guid> packages) : IDisposable
{
    private readonly IServiceScope scope = factory.Services.CreateScope();

    public async Task PickFullAndHalfPackage()
    {
        await PickFullPackage();
        await AddHalfPackageItemToPickList();
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
    }


    public void Dispose()
    {
        scope.Dispose();
        factory.Dispose();
    }
}