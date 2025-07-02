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

public class PackageService(SystemDbContext context, ISettings settings, ILogger<PackageService> logger) : IPackageService {
    public async Task<Package> CreatePackageAsync(SessionInfo sessionInfo, CreatePackageRequest request) {
        if (!settings.Options.EnablePackages) {
            throw new InvalidOperationException("Package feature is not enabled");
        }

        string barcode = await GeneratePackageBarcodeAsync();

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

        await LogLocationMovementAsync(package.Id, PackageMovementType.Created,
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

    public async Task<Package?> GetPackageByBarcodeAsync(string barcode) {
        return await context.Packages
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Barcode == barcode && !p.Deleted);
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

    public async Task<PackageContent> AddItemToPackageAsync(AddItemToPackageRequest request, SessionInfo sessionInfo) {
        var package = await GetPackageAsync(request.PackageId);
        if (package == null || package.WhsCode != sessionInfo.Warehouse) {
            throw new InvalidOperationException($"Package {request.PackageId} not found");
        }

        if (package.Status == PackageStatus.Locked) {
            throw new InvalidOperationException($"Package {package.Barcode} is locked");
        }

        if (package.Status == PackageStatus.Closed) {
            throw new InvalidOperationException($"Package {package.Barcode} is closed");
        }

        if (request.BinEntry != package.BinEntry) {
            throw new InvalidOperationException("Item location must match package location");
        }

        var existingContent = await context.PackageContents
            .FirstOrDefaultAsync(c => c.PackageId == request.PackageId && c.ItemCode == request.ItemCode);

        if (existingContent != null) {
            existingContent.Quantity        += request.Quantity;
            existingContent.UpdatedAt       =  DateTime.UtcNow;
            existingContent.UpdatedByUserId =  sessionInfo.Guid;

            await LogPackageTransactionAsync(new LogPackageTransactionRequest {
                PackageId             = request.PackageId,
                TransactionType       = PackageTransactionType.Add,
                ItemCode              = request.ItemCode,
                Quantity              = request.Quantity,
                UnitType              = request.UnitType,
                SourceOperationType   = request.SourceOperationType ?? ObjectType.Package,
                SourceOperationId     = request.SourceOperationId,
                SourceOperationLineId = request.SourceOperationLineId,
                UserId                = sessionInfo.Guid,
                Notes                 = "Item quantity increased in package"
            });

            await context.SaveChangesAsync();
            return existingContent;
        }

        var content = new PackageContent {
            Id        = Guid.NewGuid(),
            PackageId = request.PackageId,
            ItemCode  = request.ItemCode,
            Quantity  = request.Quantity,
            UnitType  = request.UnitType,
            WhsCode   = sessionInfo.Warehouse,
            BinEntry  = request.BinEntry,
            CreatedBy = sessionInfo.Guid,
        };

        context.PackageContents.Add(content);

        if (package.Status == PackageStatus.Init) {
            package.Status    = PackageStatus.Active;
            package.UpdatedAt = DateTime.UtcNow;
        }

        await LogPackageTransactionAsync(new LogPackageTransactionRequest {
            PackageId             = request.PackageId,
            TransactionType       = PackageTransactionType.Add,
            ItemCode              = request.ItemCode,
            Quantity              = request.Quantity,
            UnitType              = request.UnitType,
            SourceOperationType   = request.SourceOperationType ?? ObjectType.Package,
            SourceOperationId     = request.SourceOperationId,
            SourceOperationLineId = request.SourceOperationLineId,
            UserId                = sessionInfo.Guid,
            Notes                 = "Item added to package"
        });

        await context.SaveChangesAsync();

        logger.LogInformation("Item {ItemCode} added to package {Barcode}: {Quantity} {UnitCode}",
            request.ItemCode, package.Barcode, request.Quantity, request.UnitType);

        return content;
    }

    public async Task<PackageContent> RemoveItemFromPackageAsync(RemoveItemFromPackageRequest request, SessionInfo sessionInfo) {
        var package = await GetPackageAsync(request.PackageId);
        if (package == null) {
            throw new InvalidOperationException($"Package {request.PackageId} not found");
        }

        if (package.Status == PackageStatus.Locked) {
            throw new InvalidOperationException($"Package {package.Barcode} is locked");
        }

        var content = await context.PackageContents
            .FirstOrDefaultAsync(c => c.PackageId == request.PackageId && c.ItemCode == request.ItemCode);

        if (content == null) {
            throw new InvalidOperationException($"Item {request.ItemCode} not found in package {package.Barcode}");
        }

        if (content.Quantity < request.Quantity) {
            throw new InvalidOperationException($"Insufficient quantity. Available: {content.Quantity}, Requested: {request.Quantity}");
        }

        content.Quantity -= request.Quantity;

        if (content.Quantity == 0) {
            context.PackageContents.Remove(content);
        }
        else {
            content.UpdatedAt       = DateTime.UtcNow;
            content.UpdatedByUserId = sessionInfo.Guid;
        }

        await LogPackageTransactionAsync(new LogPackageTransactionRequest {
            PackageId           = request.PackageId,
            TransactionType     = PackageTransactionType.Remove,
            ItemCode            = request.ItemCode,
            Quantity            = -request.Quantity,
            UnitType            = content.UnitType,
            SourceOperationType = request.SourceOperationType ?? ObjectType.Package,
            SourceOperationId   = request.SourceOperationId,
            UserId              = sessionInfo.Guid,
            Notes               = "Item removed from package"
        });

        await context.SaveChangesAsync();

        logger.LogInformation("Item {ItemCode} removed from package {Barcode}: {Quantity} {UnitCode}",
            request.ItemCode, package.Barcode, request.Quantity, content.UnitType);

        return content;
    }

    public async Task<IEnumerable<PackageContent>> GetPackageContentsAsync(Guid packageId) {
        return await context.PackageContents
            .Where(c => c.PackageId == packageId && !c.Deleted)
            .ToListAsync();
    }

    public async Task<decimal> GetItemQuantityInPackageAsync(Guid packageId, string itemCode) {
        return await context.PackageContents
            .Where(c => c.PackageId == packageId && c.ItemCode == itemCode && !c.Deleted)
            .SumAsync(c => c.Quantity);
    }

    public async Task<Package> MovePackageAsync(MovePackageRequest request) {
        // throw new Exception("TODO: Not implemented, need to take into consideration SBO stock");
        var package = await GetPackageAsync(request.PackageId);
        if (package == null) {
            throw new InvalidOperationException($"Package {request.PackageId} not found");
        }

        if (package.Status == PackageStatus.Locked) {
            throw new InvalidOperationException($"Package {package.Barcode} is locked");
        }

        string fromWhsCode  = package.WhsCode;
        int?   fromBinEntry = package.BinEntry;

        package.WhsCode   = request.ToWhsCode;
        package.BinEntry  = request.ToBinEntry;
        package.UpdatedAt = DateTime.UtcNow;

        var contents = await GetPackageContentsAsync(request.PackageId);
        foreach (var content in contents) {
            content.WhsCode   = request.ToWhsCode;
            content.BinEntry  = request.ToBinEntry;
            content.UpdatedAt = DateTime.UtcNow;
        }

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
        return await context.PackageLocationHistory
            .Where(h => h.PackageId == packageId && !h.Deleted)
            .OrderByDescending(h => h.MovementDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<PackageTransaction>> GetPackageTransactionHistoryAsync(Guid packageId) {
        return await context.PackageTransactions
            .Where(t => t.PackageId == packageId && !t.Deleted)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<string> GeneratePackageBarcodeAsync() {
        var  barcodeSettings = settings.Package.Barcode;
        long lastNumber      = await GetLastPackageNumberAsync();
        long nextNumber      = lastNumber + 1;

        string numberPart = nextNumber.ToString().PadLeft(
            barcodeSettings.Length - barcodeSettings.Prefix.Length - barcodeSettings.Suffix.Length, '0');

        return $"{barcodeSettings.Prefix}{numberPart}{barcodeSettings.Suffix}";
    }

    public async Task<bool> ValidatePackageBarcodeAsync(string barcode) {
        if (string.IsNullOrEmpty(barcode))
            return false;

        var existing = await context.Packages
            .FirstOrDefaultAsync(p => p.Barcode == barcode && !p.Deleted);

        return existing == null;
    }

    public async Task<PackageValidationResult> ValidatePackageConsistencyAsync(Guid packageId) {
        var package = await GetPackageAsync(packageId);
        if (package == null) {
            return new PackageValidationResult {
                IsValid = false,
                Errors  = ["Package not found"]
            };
        }

        var result = new PackageValidationResult { IsValid = true };

        var contents                = await GetPackageContentsAsync(packageId);
        var locationInconsistencies = contents.Where(c => c.WhsCode != package.WhsCode || c.BinEntry != package.BinEntry).ToList();

        if (locationInconsistencies.Any()) {
            result.IsValid = false;
            result.Errors.Add($"Location inconsistency: {locationInconsistencies.Count} items have different location than package");
        }

        return result;
    }

    public async Task<IEnumerable<PackageInconsistency>> DetectInconsistenciesAsync(string? whsCode = null) {
        var query = context.PackageInconsistencies
            .Where(i => !i.IsResolved && !i.Deleted);

        if (!string.IsNullOrEmpty(whsCode)) {
            query = query.Where(i => i.WhsCode == whsCode);
        }

        return await query.ToListAsync();
    }

    public async Task LogPackageTransactionAsync(LogPackageTransactionRequest request) {
        var transaction = new PackageTransaction {
            Id                    = Guid.NewGuid(),
            PackageId             = request.PackageId,
            TransactionType       = request.TransactionType,
            ItemCode              = request.ItemCode,
            Quantity              = request.Quantity,
            UnitType              = request.UnitType,
            SourceOperationType   = request.SourceOperationType ?? ObjectType.Package,
            SourceOperationId     = request.SourceOperationId,
            SourceOperationLineId = request.SourceOperationLineId,
            UserId                = request.UserId,
            TransactionDate       = DateTime.UtcNow,
            Notes                 = request.Notes
        };

        await context.PackageTransactions.AddAsync(transaction);
    }

    private async Task<long> GetLastPackageNumberAsync() {
        var     barcodeSettings = settings.Package.Barcode;
        string? prefix          = barcodeSettings.Prefix;
        string? suffix          = barcodeSettings.Suffix;

        var lastPackage = await context.Packages
            .Where(p => p.Barcode.StartsWith(prefix) && p.Barcode.EndsWith(suffix) && !p.Deleted)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastPackage == null) {
            return barcodeSettings.StartNumber - 1;
        }

        string numberPart = lastPackage.Barcode.Substring(prefix.Length, lastPackage.Barcode.Length - prefix.Length - suffix.Length);

        if (long.TryParse(numberPart, out long number)) {
            return number;
        }

        return barcodeSettings.StartNumber - 1;
    }

    private string? SerializeCustomAttributes(Dictionary<string, object>? attributes) {
        if (attributes == null || !attributes.Any())
            return null;

        return JsonSerializer.Serialize(attributes);
    }

    private async Task LogLocationMovementAsync(
        Guid                packageId,
        PackageMovementType movementType,
        string?             fromWhsCode,
        int?                fromBinEntry,
        string              toWhsCode,
        int?                toBinEntry,
        ObjectType          sourceOperationType,
        Guid?               sourceOperationId, Guid userId) {
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
            UserId              = userId,
            MovementDate        = DateTime.UtcNow
        };

        await context.PackageLocationHistory.AddAsync(movement);
    }
}