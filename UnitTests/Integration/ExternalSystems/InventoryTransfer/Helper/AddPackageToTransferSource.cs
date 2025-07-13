using Core.DTOs.Transfer;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.InventoryTransfer.Helper;

public class AddPackageToTransferSource(Guid transferId, string testItem, WebApplicationFactory<Program> factory, Guid package, ISettings settings) {
    private readonly int binEntry = settings.Filters.InitialCountingBinEntry!.Value;

    public async Task Execute() {
        await Add();
        await Validate();
    }

    private async Task Add() {
        var scope       = factory.Services.CreateScope();
        var lineService = scope.ServiceProvider.GetRequiredService<ITransferLineService>();
        var request = new TransferAddItemRequest {
            ID                = transferId,
            ItemCode          = testItem,
            BinEntry          = binEntry,
            Unit              = UnitType.Dozen,
            Quantity          = 2,
            Type              = SourceTarget.Source,
            PackageId         = package,
            IsPackageTransfer = true
        };
        var response = await lineService.AddItem(TestConstants.SessionInfo, request);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.ErrorMessage, Is.Null, response.ErrorMessage ?? "No error message");
        Assert.That(response.IsPackageScan);
        Assert.That(response.PackageId, Is.EqualTo(package));;
        Assert.That(response.PackageContents, Is.Not.Null);
        Assert.That(response.PackageContents.Any());
        Assert.That(response.PackageContents.First().ItemCode, Is.EqualTo(testItem));
        Assert.That(response.PackageContents.First().Quantity, Is.EqualTo(24));
    }

    private async Task Validate() {
        var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var package = await service.Packages
            .Include(v => v.Contents)
            .Include(v => v.Commitments)
            .FirstAsync(v => v.Id == transferId);
        Assert.That(package, Is.Not.Null);
        Assert.That(package.Contents.Any());
        var packageContent = package.Contents.First();
        Assert.That(packageContent.ItemCode, Is.EqualTo(testItem));
        Assert.That(packageContent.Quantity, Is.EqualTo(24));
        Assert.That(package.Commitments.Any());
        var packageCommitment = package.Commitments.First();
        Assert.That(packageCommitment.Quantity, Is.EqualTo(24));
        Assert.That(packageCommitment.ItemCode, Is.EqualTo(testItem));;
        Assert.That(packageCommitment.SourceOperationType, Is.EqualTo(ObjectType.Transfer));
        Assert.That(packageCommitment.SourceOperationId, Is.EqualTo(transferId));
        Assert.That(packageCommitment.SourceOperationLineId, Is.EqualTo(packageContent.Id));
        Assert.That(packageCommitment.CommittedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
    }
}