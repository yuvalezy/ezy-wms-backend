using Core.DTOs.Package;
using Core.Entities;
using Core.Enums;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PackageLocationService(SystemDbContext context, IPackageContentService contentService, ILogger<PackageLocationService> logger) : IPackageLocationService {
    public async Task<Package> MovePackageAsync(MovePackageRequest request) {
        var package = await context.Packages
            .FirstOrDefaultAsync(p => p.Id == request.PackageId && !p.Deleted);

        if (package == null) {
            throw new InvalidOperationException($"Package {request.PackageId} not found");
        }

        if (package.Status == PackageStatus.Locked) {
            throw new InvalidOperationException($"Package {package.Barcode} is locked");
        }

        string fromWhsCode  = package.WhsCode;
        int?   fromBinEntry = package.BinEntry;

        // Update package location
        package.WhsCode   = request.ToWhsCode;
        package.BinEntry  = request.ToBinEntry;
        package.UpdatedAt = DateTime.UtcNow;
        package.UpdatedByUserId = request.UserId;

        // Update all content locations to match package
        var contents = await contentService.GetPackageContentsAsync(request.PackageId);
        foreach (var content in contents) {
            content.WhsCode   = request.ToWhsCode;
            content.BinEntry  = request.ToBinEntry;
            content.UpdatedAt = DateTime.UtcNow;
            content.UpdatedByUserId = request.UserId;
        }

        // Log the movement
        await LogLocationMovementAsync(request.PackageId, PackageMovementType.Moved,
            fromWhsCode, fromBinEntry,
            request.ToWhsCode, request.ToBinEntry,
            request.SourceOperationType ?? ObjectType.Package, request.SourceOperationId, request.UserId);

        await context.SaveChangesAsync();

        logger.LogInformation("Package {Barcode} moved from {FromWhsCode} to {ToWhsCode} by user {UserId}",
            package.Barcode, fromWhsCode, request.ToWhsCode, request.UserId);

        return package;
    }

    public async Task<IEnumerable<PackageLocationHistory>> GetPackageLocationHistoryAsync(Guid packageId) {
        var history = await context.PackageLocationHistory
            .Where(h => h.PackageId == packageId && !h.Deleted)
            .OrderByDescending(h => h.MovementDate)
            .ToListAsync();

        logger.LogDebug("Retrieved {Count} location history records for package {PackageId}",
            history.Count, packageId);

        return history;
    }

    public async Task LogLocationMovementAsync(
        Guid                packageId,
        PackageMovementType movementType,
        string?             fromWhsCode,
        int?                fromBinEntry,
        string              toWhsCode,
        int?                toBinEntry,
        ObjectType          sourceOperationType,
        Guid?               sourceOperationId,
        Guid                userId) {
        var movement = new PackageLocationHistory {
            Id                  = Guid.NewGuid(),
            PackageId           = packageId,
            MovementType        = movementType,
            FromWhsCode         = fromWhsCode,
            FromBinEntry        = fromBinEntry,
            ToWhsCode           = toWhsCode,
            ToBinEntry          = toBinEntry,
            SourceOperationType = sourceOperationType,
            SourceOperationId   = sourceOperationId,
            CreatedByUserId     = userId,
            MovementDate        = DateTime.UtcNow
        };

        await context.PackageLocationHistory.AddAsync(movement);

        logger.LogDebug("Logged package movement: {PackageId} from {FromWhsCode} to {ToWhsCode} by {UserId}",
            packageId, fromWhsCode, toWhsCode, userId);
    }
}