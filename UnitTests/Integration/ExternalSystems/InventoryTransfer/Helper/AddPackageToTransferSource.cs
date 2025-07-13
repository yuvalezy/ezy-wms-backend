using Core.DTOs.Transfer;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.InventoryTransfer.Helper;

public class AddPackageToTransferSource(Guid transferId, string testItem, WebApplicationFactory<Program> factory, Guid packageId, ISettings settings) {
    private readonly int  binEntry = settings.Filters.InitialCountingBinEntry!.Value;
    
    private Guid[]? transferLines;

    public async Task Execute() {
        await Add();
        await Validate();
    }

    private async Task Add() {
        var scope       = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransferPackageService>();
        var request  = new TransferAddSourcePackageRequest {
            TransferId = transferId,
            PackageId  = packageId,
            BinEntry   = binEntry
        };
        var response = await service.HandleSourcePackageScanAsync(request, TestConstants.SessionInfo);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.ErrorMessage, Is.Null, response.ErrorMessage ?? "No error message");
        Assert.That(response.IsPackageScan);
        Assert.That(response.PackageId, Is.EqualTo(packageId));;
        Assert.That(response.PackageContents, Is.Not.Null);
        Assert.That(response.PackageContents.Any());
        Assert.That(response.PackageContents.First().ItemCode, Is.EqualTo(testItem));
        Assert.That(response.PackageContents.First().Quantity, Is.EqualTo(24));
        transferLines = response.LinesIds;
    }

    private async Task Validate() {
        var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var package = await service.Packages
            .Include(v => v.Contents)
            .Include(v => v.Commitments)
            .FirstAsync(v => v.Id == packageId);
        Assert.That(package, Is.Not.Null);
        Assert.That(package.Contents.Any());
        var packageContent = package.Contents.First();
        Assert.That(packageContent.ItemCode, Is.EqualTo(testItem));
        Assert.That(packageContent.Quantity, Is.EqualTo(24));
        Assert.That(packageContent.CommittedQuantity, Is.EqualTo(24));
        Assert.That(package.Commitments.Any());
        var packageCommitment = package.Commitments.First();
        Assert.That(packageCommitment.Quantity, Is.EqualTo(24));
        Assert.That(packageCommitment.ItemCode, Is.EqualTo(testItem));;
        Assert.That(packageCommitment.SourceOperationType, Is.EqualTo(ObjectType.Transfer));
        Assert.That(packageCommitment.SourceOperationId, Is.EqualTo(transferId));
        Assert.That(packageCommitment.SourceOperationLineId, Is.EqualTo(transferLines.First()));
        Assert.That(packageCommitment.CommittedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
    }
}