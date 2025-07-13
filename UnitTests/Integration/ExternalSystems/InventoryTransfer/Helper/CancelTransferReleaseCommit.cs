using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.InventoryTransfer.Helper;

public class CancelTransferReleaseCommit(Guid transferId, string testItem, WebApplicationFactory<Program> factory, Guid packageId, ISettings settings) {
    private readonly int  binEntry = settings.Filters.InitialCountingBinEntry!.Value;

    public async Task Execute() {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransferService>();
        await service.CancelTransfer(transferId, TestConstants.SessionInfo);
        
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var package = await db.Packages
            .Include(v => v.Contents)
            .Include(v => v.Commitments)
            .FirstAsync(v => v.Id == packageId);
        
        Assert.That(package, Is.Not.Null);
        Assert.That(package.Contents.Any());
        var packageContent = package.Contents.First();
        Assert.That(packageContent.ItemCode, Is.EqualTo(testItem));
        Assert.That(packageContent.Quantity, Is.EqualTo(24));
        Assert.That(packageContent.CommittedQuantity, Is.EqualTo(0));
        Assert.That(!package.Commitments.Any());
    }
}