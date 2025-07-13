using Core.DTOs.Transfer;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.InventoryTransfer.Helper;

public class CreateTransferHelper(string testItem, WebApplicationFactory<Program> factory) {
    private Guid id = Guid.Empty;

    public async Task<Guid> Execute() {
        var response = await CreateTransfer();
        await ValidateGetTransfers();
        return response.Id;
    }


    private async Task<TransferResponse> CreateTransfer() {
        using var scope                     = factory.Services.CreateScope();
        var       inventoryTransfersService = scope.ServiceProvider.GetRequiredService<ITransferService>();
        var       request                   = new CreateTransferRequest {Name = $"Test {testItem}" };
        var       response                  = await inventoryTransfersService.CreateTransfer(request, TestConstants.SessionInfo);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Status, Is.EqualTo(ObjectStatus.Open));
        Assert.That(response.Status == ObjectStatus.Open);
        id = response.Id;
        return response;
    }

    private async Task ValidateGetTransfers() {
        using var scope                     = factory.Services.CreateScope();
        var       inventoryTransfersService = scope.ServiceProvider.GetRequiredService<ITransferService>();
        var       transfersRequest          = new TransfersRequest {Status = [ObjectStatus.Open, ObjectStatus.InProgress]};
        var       response                  = await inventoryTransfersService.GetTransfers(transfersRequest, TestConstants.SessionInfo.Warehouse);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Any(v => v.Id == id));
    }
}