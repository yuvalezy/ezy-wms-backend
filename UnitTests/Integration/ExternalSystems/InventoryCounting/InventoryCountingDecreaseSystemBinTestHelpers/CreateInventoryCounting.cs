using Core.DTOs.InventoryCounting;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.InventoryCounting.InventoryCountingDecreaseSystemBinTestHelpers;

public class CreateInventoryCounting(string testItem, WebApplicationFactory<Program> factory) {
    private Guid id = Guid.Empty;

    public async Task<Guid> Execute() {
        var response = await CreateCounting();
        await ValidateGetCountings();
        return response.Id;
    }


    private async Task<InventoryCountingResponse> CreateCounting() {
        using var scope                     = factory.Services.CreateScope();
        var       inventoryCountingsService = scope.ServiceProvider.GetRequiredService<IInventoryCountingsService>();
        var request = new CreateInventoryCountingRequest {
            Name = $"Test {testItem}"
        };
        var response = await inventoryCountingsService.CreateCounting(request, TestConstants.SessionInfo);
        Assert.That(response, Is.Not.Null);
        Assert.That(!response.Error);
        Assert.That(response.Status == ObjectStatus.Open);
        id = response.Id;
        return response;
    }

    private async Task ValidateGetCountings() {
        using var scope                     = factory.Services.CreateScope();
        var       inventoryCountingsService = scope.ServiceProvider.GetRequiredService<IInventoryCountingsService>();
        var response = await inventoryCountingsService.GetCountings(new InventoryCountingsRequest {
            Statuses = [
                ObjectStatus.Open, ObjectStatus.InProgress
            ]
        }, TestConstants.SessionInfo.Warehouse);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Any(v => v.Id == id));
    }
}