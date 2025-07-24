using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
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

    public void Dispose()
    {
        scope.Dispose();
        factory.Dispose();
    }
}