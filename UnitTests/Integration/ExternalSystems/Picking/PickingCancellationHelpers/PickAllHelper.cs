using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.Picking.PickingCancellationHelpers;

public class PickAllHelper(int pickEntry, WebApplicationFactory<Program> factory, int binEntry, int salesEntry, string testItem) : IDisposable
{
    private readonly IServiceScope scope = factory.Services.CreateScope();

    public async Task PickAll()
    {
        var lineService = scope.ServiceProvider.GetRequiredService<IPickListLineService>();
        var request = new PickListAddItemRequest
        {
            ID = pickEntry,
            Type = 17,
            Entry = salesEntry,
            ItemCode = testItem,
            BinEntry = binEntry,
            Unit = UnitType.Pack
        };

        //Pick 20 boxes
        for (int i = 0; i < 20; i++)
        {
            request.Quantity = 1;
            var response = await lineService.AddItem(TestConstants.SessionInfo, request);
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo(ResponseStatus.Ok), response.ErrorMessage ?? "No error message");
        }

        //Test exceed error
        request.Quantity = 1;
        var errorResponse = await lineService.AddItem(TestConstants.SessionInfo, request);
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse.Status, Is.EqualTo(ResponseStatus.Error), errorResponse.ErrorMessage ?? "No error message");
        Assert.That(errorResponse.ErrorMessage, Is.EqualTo("Quantity exceeds bin available stock"));

        //Process
        var processService = scope.ServiceProvider.GetRequiredService<IPickListProcessService>();
        var processResponse = await processService.ProcessPickList(pickEntry, TestConstants.SessionInfo.Guid);
        Assert.That(processResponse, Is.Not.Null);
        Assert.That(processResponse.Status, Is.EqualTo(ResponseStatus.Ok), processResponse.ErrorMessage ?? "No error message");
    }

    public async Task PickFullAndHalfPackage(List<Guid> packages)
    {
        await PickFullPackage(packages);
        await AddHalfPackageItemToPickList(packages);
    }

    private async Task PickFullPackage(List<Guid> packages)
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
    private async Task AddHalfPackageItemToPickList(List<Guid> packages)
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