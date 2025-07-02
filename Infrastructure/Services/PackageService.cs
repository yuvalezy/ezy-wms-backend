using Core.DTOs.Package;
using Core.Entities;
using Core.Enums;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Core.Interfaces;

namespace Infrastructure.Services;

public class PackageService(
    SystemDbContext           context,
    ISettings                 settings,
    ILogger<PackageService>   logger,
    IPackageContentService    contentService,
    IPackageValidationService validationService,
    IPackageLocationService   locationService) : IPackageService {
    public async Task<Package> CreatePackageAsync(SessionInfo sessionInfo, CreatePackageRequest request) {
        if (!settings.Options.EnablePackages) {
            throw new InvalidOperationException("Package feature is not enabled");
        }

        string barcode = await validationService.GeneratePackageBarcodeAsync();

        string whsCode = sessionInfo.Warehouse;
        var    userId  = sessionInfo.Guid;
        var package = new Package {
            Id               = Guid.NewGuid(),
            Barcode          = barcode,
            Status           = PackageStatus.Init,
            WhsCode          = whsCode,
            BinEntry         = request.BinEntry,
            CreatedBy        = userId,
            ClosedAt         = null,
            ClosedBy         = null,
            Notes            = null,
            CustomAttributes = SerializeCustomAttributes(request.CustomAttributes)
        };

        context.Packages.Add(package);

        await locationService.LogLocationMovementAsync(package.Id, PackageMovementType.Created,
            null, null, whsCode, request.BinEntry,
            request.SourceOperationType ?? ObjectType.Package, request.SourceOperationId, userId);

        await context.SaveChangesAsync();

        logger.LogInformation("Package created: {Barcode} by user {UserId}", barcode, userId);

        return package;
    }

    public async Task<Package?> GetPackageAsync(Guid packageId) {
        return await context.Packages
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == packageId && !p.Deleted);
    }

    public async Task<Package?> GetPackageByBarcodeAsync(string barcode, bool content, bool history) {
        var query = context.Packages.AsQueryable();
        if (content)
            query = query.Include(c => c.Contents);
        if (history)
            query = query.Include(c => c.LocationHistory);
        return await query.FirstOrDefaultAsync(p => p.Barcode == barcode && !p.Deleted);
    }

    public async Task<IEnumerable<Package>> GetActivePackagesAsync(string? whsCode = null) {
        var query = context.Packages
            .Where(p => p.Status == PackageStatus.Active && !p.Deleted);

        if (!string.IsNullOrEmpty(whsCode)) {
            query = query.Where(p => p.WhsCode == whsCode);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Package>> GetActivePackagesBySourceAsync(ObjectType sourceOperationType, Guid sourceOperationId) {
        return await context.Packages
            .Include(p => p.Contents)
            .Where(p => (p.Status == PackageStatus.Init || p.Status == PackageStatus.Active) &&
                        !p.Deleted &&
                        p.CustomAttributes != null &&
                        p.CustomAttributes.Contains($"\"SourceOperationType\":{(int)sourceOperationType}") &&
                        p.CustomAttributes.Contains($"\"SourceOperationId\":\"{sourceOperationId}\""))
            .ToListAsync();
    }

    public async Task<int> ActivatePackagesBySourceAsync(ObjectType sourceOperationType, Guid sourceOperationId, SessionInfo sessionInfo) {
        var packages     = await GetActivePackagesBySourceAsync(sourceOperationType, sourceOperationId);
        var initPackages = packages.Where(p => p.Status == PackageStatus.Init).ToList();

        int activatedCount = 0;
        foreach (var package in initPackages) {
            if (package.Contents?.Any() == true) {
                package.Status          = PackageStatus.Active;
                package.UpdatedAt       = DateTime.UtcNow;
                package.UpdatedByUserId = sessionInfo.Guid;
                activatedCount++;

                logger.LogInformation("Package {Barcode} activated for {SourceOperationType} operation {SourceOperationId}",
                    package.Barcode, sourceOperationType, sourceOperationId);
            }
        }

        if (activatedCount > 0) {
            await context.SaveChangesAsync();
        }

        return activatedCount;
    }

    public async Task<Package> ClosePackageAsync(Guid packageId, SessionInfo sessionInfo) {
        var package = await GetPackageAsync(packageId);
        if (package == null || package.WhsCode != sessionInfo.Warehouse) {
            throw new InvalidOperationException($"Package {packageId} not found");
        }

        if (package.Status != PackageStatus.Active) {
            throw new InvalidOperationException($"Package {package.Barcode} is not active");
        }

        if (package.Contents.Count > 0) {
            throw new InvalidOperationException($"Package {package.Barcode} has items");
        }

        package.Status          = PackageStatus.Closed;
        package.ClosedAt        = DateTime.UtcNow;
        package.ClosedBy        = sessionInfo.Guid;
        package.UpdatedAt       = DateTime.UtcNow;
        package.UpdatedByUserId = sessionInfo.Guid;

        await context.SaveChangesAsync();

        logger.LogInformation("Package closed: {Barcode} by user {UserId}", package.Barcode, sessionInfo);

        return package;
    }

    public async Task<Package> CancelPackageAsync(Guid packageId, SessionInfo sessionInfo, string reason) {
        var package = await GetPackageAsync(packageId);
        if (package == null || sessionInfo.Warehouse != package.WhsCode) {
            throw new InvalidOperationException($"Package {packageId} not found");
        }

        if (package.Contents.Count > 0) {
            throw new InvalidOperationException($"Package {package.Barcode} has items");
        }

        package.Status          = PackageStatus.Cancelled;
        package.ClosedAt        = DateTime.UtcNow;
        package.ClosedBy        = sessionInfo.Guid;
        package.Notes           = reason;
        package.UpdatedAt       = DateTime.UtcNow;
        package.UpdatedByUserId = sessionInfo.Guid;

        await context.SaveChangesAsync();

        logger.LogInformation("Package cancelled: {Barcode} by user {UserId} - Reason: {Reason}",
            package.Barcode, sessionInfo, reason);

        return package;
    }

    public async Task<Package> LockPackageAsync(Guid packageId, SessionInfo sessionInfo, string? reason) {
        var package = await GetPackageAsync(packageId);
        if (package == null || sessionInfo.Warehouse != package.WhsCode) {
            throw new InvalidOperationException($"Package {packageId} not found");
        }

        package.Status          = PackageStatus.Locked;
        package.Notes           = reason;
        package.UpdatedAt       = DateTime.UtcNow;
        package.UpdatedByUserId = sessionInfo.Guid;

        await context.SaveChangesAsync();

        logger.LogWarning("Package locked: {Barcode} - Reason: {Reason}", package.Barcode, reason);

        return package;
    }

    public async Task<Package> UnlockPackageAsync(Guid packageId, SessionInfo sessionInfo) {
        var package = await GetPackageAsync(packageId);
        if (package == null) {
            throw new InvalidOperationException($"Package {packageId} not found");
        }

        if (package.Status != PackageStatus.Locked) {
            throw new InvalidOperationException($"Package {package.Barcode} is not locked");
        }

        package.Status          = PackageStatus.Active;
        package.Notes           = null;
        package.UpdatedAt       = DateTime.UtcNow;
        package.UpdatedByUserId = sessionInfo.Guid;

        await context.SaveChangesAsync();

        logger.LogInformation("Package unlocked: {Barcode} by user {UserId}", package.Barcode, sessionInfo);

        return package;
    }

    // Content Management - used internally and by GoodsReceiptService
    public async Task<PackageContent> AddItemToPackageAsync(AddItemToPackageRequest request, SessionInfo sessionInfo) {
        return await contentService.AddItemToPackageAsync(request, sessionInfo);
    }

    private string? SerializeCustomAttributes(Dictionary<string, object>? attributes) {
        if (attributes == null || !attributes.Any())
            return null;

        return JsonSerializer.Serialize(attributes);
    }
}