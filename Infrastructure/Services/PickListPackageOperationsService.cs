using Core.DTOs.Package;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PickListPackageOperationsService(SystemDbContext db, IPackageContentService packageContentService) {
    public async Task<(Package?, PackageContent?, PickListAddItemResponse?)> ValidatePackageForItem(PickListAddItemRequest request) {
        if (request.PackageId == null) {
            return (null, null, null);
        }

        // Load package with contents
        var package = await db.Packages
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == request.PackageId.Value);

        if (package == null) {
            return (null, null, PickListAddItemResponse.Error($"Package {request.PackageId} not found"));
        }

        // Validate package status
        var statusValidation = ValidatePackageStatus(package);
        if (statusValidation != null) {
            return (null, null, statusValidation);
        }

        // Validate bin location if specified
        if (request.BinEntry.HasValue && package.BinEntry != request.BinEntry.Value) {
            return (null, null, PickListAddItemResponse.Error("Package is not in the specified bin location"));
        }

        // Find the specific package content for the requested item
        var packageContent = package.Contents.FirstOrDefault(c => c.ItemCode == request.ItemCode);
        if (packageContent == null) {
            return (null, null, PickListAddItemResponse.Error($"Item {request.ItemCode} not found in package {request.PackageId}"));
        }

        // Check available quantity
        var availableQuantity = packageContent.Quantity - packageContent.CommittedQuantity;
        if (request.Quantity > availableQuantity) {
            return (null, null, PickListAddItemResponse.Error($"Insufficient quantity in package. Available: {availableQuantity}, Requested: {request.Quantity}"));
        }

        return (package, packageContent, null);
    }

    public async Task<(Package?, PickListPackageResponse?)> ValidatePackageForFullPicking(PickListAddPackageRequest request) {
        // Load package with contents
        var package = await db.Packages
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == request.PackageId);

        if (package == null) {
            return (null, PickListPackageResponse.ErrorResponse($"Package {request.PackageId} not found"));
        }

        // Validate package status
        var statusValidation = ValidatePackageStatusForPackageResponse(package);
        if (statusValidation != null) {
            return (null, statusValidation);
        }

        // Validate bin location if specified
        if (request.BinEntry.HasValue && package.BinEntry != request.BinEntry.Value) {
            return (null, PickListPackageResponse.ErrorResponse("Package is not in the specified bin location"));
        }

        // Check if package already added to this pick list
        var existingPackage = await db.PickListPackages
            .AnyAsync(plp => plp.AbsEntry == request.ID &&
                             plp.PackageId == request.PackageId);

        if (existingPackage) {
            return (null, PickListPackageResponse.ErrorResponse("Package already added to this pick list"));
        }

        return (package, null);
    }

    public async Task CreatePackageCommitment(SessionInfo sessionInfo, PickList pickList, PickListAddItemRequest request) {
        var commitment = new PackageCommitment {
            Id = Guid.NewGuid(),
            PackageId = request.PackageId!.Value,
            ItemCode = request.ItemCode,
            Quantity = request.Quantity,
            SourceOperationType = ObjectType.Picking,
            SourceOperationId = pickList.Id,
            TargetPackageId = request.PickingPackageId,
            CommittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = sessionInfo.Guid
        };

        db.PackageCommitments.Add(commitment);
    }

    public async Task<bool> CreatePickListPackageIfNotExists(SessionInfo sessionInfo, PickList pickList, PickListAddItemRequest request, Package package) {
        var existingPickListPackage = await db.PickListPackages
            .FirstOrDefaultAsync(plp => plp.AbsEntry == request.ID &&
                                        plp.PackageId == request.PackageId!.Value);

        if (existingPickListPackage != null)
            return false;

        var pickListPackage = new PickListPackage {
            Id = Guid.NewGuid(),
            AbsEntry = request.ID,
            PickEntry = pickList.PickEntry,
            PackageId = request.PackageId.Value,
            Type = SourceTarget.Source,
            BinEntry = request.BinEntry ?? package.BinEntry,
            AddedAt = DateTime.UtcNow,
            AddedByUserId = sessionInfo.Guid,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = sessionInfo.Guid
        };

        db.PickListPackages.Add(pickListPackage);
        return true;

    }

    public async Task AddOrUpdatePackageContent(SessionInfo sessionInfo, Guid packageId, string itemCode, int quantity, int? binEntry, int id, int type, int entry, Guid pickListId) {
        var package = await db.Packages.Include(v => v.Contents).FirstOrDefaultAsync(v => v.Id == packageId);
        if (package == null) {
            throw new KeyNotFoundException($"Package {packageId} not found");
        }

        var packageContent = package.Contents.FirstOrDefault(c => c.ItemCode == itemCode);
        if (packageContent == null) {
            packageContent = new PackageContent {
                CreatedByUserId = sessionInfo.Guid,
                PackageId = packageId,
                ItemCode = itemCode,
                Quantity = quantity,
                CommittedQuantity = quantity,
                WhsCode = sessionInfo.Warehouse,
                BinEntry = binEntry,
            };
            await db.PackageContents.AddAsync(packageContent);
        }
        else {
            packageContent.Quantity += quantity;
            packageContent.CommittedQuantity += quantity;
            db.PackageContents.Update(packageContent);
        }
        
        // Create the commitment entries
        var commitment = new PackageCommitment {
            Id = Guid.NewGuid(),
            PackageId = packageId,
            ItemCode = itemCode,
            Quantity = quantity,
            SourceOperationType = ObjectType.Picking,
            SourceOperationId = pickListId,
            CommittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = sessionInfo.Guid
        };

        db.PackageCommitments.Add(commitment);
        
        //Add log of added items from source
        await packageContentService.LogPackageTransactionAsync(new LogPackageTransactionRequest {
            PackageId = packageId,
            TransactionType = PackageTransactionType.Add,
            ItemCode = itemCode,
            Quantity = quantity,
            UnitQuantity = quantity,
            UnitType = UnitType.Unit,
            SourceOperationType = ObjectType.Picking,
            SourceOperationId = packageContent.Id,
            UserId = sessionInfo.Guid,
        });

        await db.SaveChangesAsync();
    }

    private static PickListAddItemResponse? ValidatePackageStatus(Package package) {
        if (package.Status == PackageStatus.Locked) {
            return PickListAddItemResponse.Error("Package is locked");
        }

        if (package.Status != PackageStatus.Active) {
            return PickListAddItemResponse.Error("Package is not active");
        }

        return null;
    }

    private static PickListPackageResponse? ValidatePackageStatusForPackageResponse(Package package) {
        if (package.Status == PackageStatus.Locked) {
            return PickListPackageResponse.ErrorResponse("Package is locked");
        }

        if (package.Status != PackageStatus.Active) {
            return PickListPackageResponse.ErrorResponse("Package is not active");
        }

        return null;
    }
}