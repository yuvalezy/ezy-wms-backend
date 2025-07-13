using System.ComponentModel.DataAnnotations;
using Core.DTOs.Package;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PickListPackageService(
    SystemDbContext        db,
    IExternalSystemAdapter adapter,
    IPackageService        packageService,
    IPackageContentService packageContentService) : IPickListPackageService {
    public async Task<PickListPackageResponse> HandleSourcePackageScanAsync(PickListAddSourcePackageRequest request, SessionInfo sessionInfo) {
        throw new NotImplementedException();
        // await using var transaction = await db.Database.BeginTransactionAsync();
        // try {
        //     // 1. Validate package exists and is available
        //     var package = await packageService.GetPackageAsync(request.PackageId);
        //     if (package == null) {
        //         throw new ValidationException("Package not found");
        //     }
        //
        //     // 2. Check if already assigned
        //     var existing = await db.PickListPackages
        //         .AnyAsync(plp => plp.AbsEntry == request.AbsEntry &&
        //                          plp.PickEntry == request.PickEntry &&
        //                          plp.PackageId == request.PackageId &&
        //                          plp.Type == SourceTarget.Source);
        //
        //     if (existing) {
        //         throw new ValidationException("Package already assigned as source to this pick operation");
        //     }
        //
        //     // 3. Create PickListPackage record
        //     var pickListPackage = new PickListPackage {
        //         AbsEntry        = request.AbsEntry,
        //         PickEntry       = request.PickEntry,
        //         PackageId       = request.PackageId,
        //         Type            = SourceTarget.Source,
        //         BinEntry        = request.BinEntry,
        //         AddedAt         = DateTime.UtcNow,
        //         AddedByUserId   = sessionInfo.Guid,
        //         CreatedAt       = DateTime.UtcNow,
        //         CreatedByUserId = sessionInfo.Guid
        //     };
        //
        //     db.PickListPackages.Add(pickListPackage);
        //
        //     // 4. Get package contents and create commitments
        //     var packageContents = await db.PackageContents
        //         .Where(pc => pc.PackageId == request.PackageId)
        //         .ToListAsync();
        //
        //     foreach (var content in packageContents) {
        //         // Update committed quantity in package content
        //         content.CommittedQuantity += content.Quantity;
        //         db.PackageContents.Update(content);
        //
        //         // Create package commitment record
        //         var commitment = new PackageCommitment {
        //             PackageId             = request.PackageId,
        //             ItemCode              = content.ItemCode,
        //             Quantity              = content.Quantity,
        //             SourceOperationType   = ObjectType.Picking,
        //             SourceOperationId     = new Guid(request.AbsEntry.ToString().PadLeft(32, '0')),  // Convert AbsEntry to Guid
        //             SourceOperationLineId = new Guid(request.PickEntry.ToString().PadLeft(32, '0')), // Store PickEntry in LineId
        //             CommittedAt           = DateTime.UtcNow,
        //             CreatedAt             = DateTime.UtcNow,
        //             CreatedByUserId       = sessionInfo.Guid
        //         };
        //
        //         db.PackageCommitments.Add(commitment);
        //     }
        //
        //     await db.SaveChangesAsync();
        //     await transaction.CommitAsync();
        //
        //     return new PickListPackageResponse {
        //         Success         = true,
        //         PackageId       = package.Id,
        //         PackageContents = (await Task.WhenAll(packageContents.Select(async c => await c.ToDto(adapter)))).ToList()
        //     };
        // }
        // catch {
        //     await transaction.RollbackAsync();
        //     throw;
        // }
    }

    public async Task<bool> CanAutoPickPackageAsync(int absEntry, Guid packageId) {
        // 1. Get package contents
        var packageContents = await db.PackageContents
            .Where(pc => pc.PackageId == packageId)
            .Select(pc => new {pc.ItemCode, pc.WhsCode, pc.Quantity, pc.CommittedQuantity})
            .ToListAsync();

        if (!packageContents.Any()) {
            return false; // Empty package
        }

        // 3. Check if package has any existing commitments
        if (packageContents.Any(pc => pc.CommittedQuantity > 0)) {
            return false; // Package already has commitments
        }

        // 4. Validate that in External System there is open quantity for all package content
        var values = packageContents.Select(v => new PickListValidateAddPackageRequest(v.ItemCode, v.WhsCode, v.Quantity));
        return await adapter.ValidatePickingAddPackage(absEntry, values);
    }

    public async Task<PickListPackageResponse> HandleAutoPickPackageAsync(PickListAutoPickRequest request, SessionInfo sessionInfo) {
        throw new NotImplementedException();
        // await using var transaction = await db.Database.BeginTransactionAsync();
        // try {
        //     // 1. Validate that auto-pick is possible
        //     var canAutoPick = await CanAutoPickPackageAsync(request.AbsEntry, request.PickEntry, request.SourcePackageId);
        //     if (!canAutoPick) {
        //         throw new ValidationException("Package cannot fulfill all pick requirements or has existing commitments");
        //     }
        //
        //     // 2. Get source package and validate
        //     var sourcePackage = await packageService.GetPackageAsync(request.SourcePackageId);
        //     if (sourcePackage == null) {
        //         throw new ValidationException("Source package not found");
        //     }
        //
        //     // 3. Get all pending pick list items
        //     var pendingPickListItems = await db.PickLists
        //         .Where(pl => pl.AbsEntry == request.AbsEntry &&
        //                      pl.PickEntry == request.PickEntry &&
        //                      pl.Status == ObjectStatus.Open)
        //         .ToListAsync();
        //
        //     // 4. Create or get target package
        //     Package targetPackage;
        //     if (request.TargetPackageId.HasValue) {
        //         targetPackage = await packageService.GetPackageAsync(request.TargetPackageId.Value);
        //         if (targetPackage == null) {
        //             throw new ValidationException("Target package not found");
        //         }
        //     }
        //     else {
        //         // Create new target package
        //         var createPackageRequest = new CreatePackageRequest {
        //             SourceOperationType = ObjectType.Picking,
        //             SourceOperationId   = new Guid(request.AbsEntry.ToString().PadLeft(32, '0')),
        //             WhsCode             = sessionInfo.Warehouse,
        //             BinEntry            = request.TargetBinEntry,
        //             Notes               = $"Auto-picked from package {sourcePackage.Barcode}"
        //         };
        //         targetPackage = await packageService.CreatePackageAsync(sessionInfo, createPackageRequest);
        //     }
        //
        //     // 5. Create source package assignment with commitments
        //     await HandleSourcePackageScanAsync(new PickListAddSourcePackageRequest {
        //         AbsEntry  = request.AbsEntry,
        //         PickEntry = request.PickEntry,
        //         PackageId = request.SourcePackageId,
        //         BinEntry  = sourcePackage.BinEntry
        //     }, sessionInfo);
        //
        //     // 6. Create target package assignment
        //     var targetPickListPackage = new PickListPackage {
        //         AbsEntry        = request.AbsEntry,
        //         PickEntry       = request.PickEntry,
        //         PackageId       = targetPackage.Id,
        //         Type            = SourceTarget.Target,
        //         BinEntry        = request.TargetBinEntry,
        //         AddedAt         = DateTime.UtcNow,
        //         AddedByUserId   = sessionInfo.Guid,
        //         CreatedAt       = DateTime.UtcNow,
        //         CreatedByUserId = sessionInfo.Guid
        //     };
        //
        //     db.PickListPackages.Add(targetPickListPackage);
        //
        //     // 7. Process each pending pick list item and add to target package
        //     var createdPickListLineIds = new List<Guid>();
        //     var packageContents        = sourcePackage.Contents.ToList();
        //
        //     foreach (var pickItem in pendingPickListItems) {
        //         var packageContent = packageContents.FirstOrDefault(pc => pc.ItemCode == pickItem.ItemCode);
        //         if (packageContent == null) continue;
        //
        //         // Update pick list item status
        //         pickItem.Status          = ObjectStatus.Finished;
        //         pickItem.UpdatedAt       = DateTime.UtcNow;
        //         pickItem.UpdatedByUserId = sessionInfo.Guid;
        //         db.PickLists.Update(pickItem);
        //
        //         // Add item to target package
        //         await packageContentService.AddItemToPackageAsync(new AddItemToPackageRequest {
        //             PackageId             = targetPackage.Id,
        //             ItemCode              = pickItem.ItemCode,
        //             Quantity              = pickItem.Quantity,
        //             UnitQuantity          = pickItem.Quantity,
        //             UnitType              = pickItem.Unit,
        //             SourceOperationType   = ObjectType.Picking,
        //             SourceOperationId     = new Guid(request.AbsEntry.ToString().PadLeft(32, '0')),
        //             SourceOperationLineId = pickItem.Id,
        //             BinEntry              = request.TargetBinEntry,
        //             WhsCode               = sessionInfo.Warehouse
        //         }, sessionInfo);
        //
        //         createdPickListLineIds.Add(pickItem.Id);
        //     }
        //
        //     await db.SaveChangesAsync();
        //     await transaction.CommitAsync();
        //
        //     return new PickListPackageResponse {
        //         Success                = true,
        //         IsAutoPickResult       = true,
        //         PackageId              = request.SourcePackageId,
        //         TargetPackageId        = targetPackage.Id,
        //         CreatedPickListLineIds = createdPickListLineIds,
        //         PackageContents        = (await Task.WhenAll(packageContents.Select(async c => await c.ToDto(adapter)))).ToList()
        //     };
        // }
        // catch {
        //     await transaction.RollbackAsync();
        //     throw;
        // }
    }

    public async Task<PickListAddItemResponse> HandlePartialPickAsync(PickListAddItemRequest request, SessionInfo sessionInfo) {
        throw new NotImplementedException();
        // if (!request.SourcePackageId.HasValue) {
        //     throw new ValidationException("SourcePackageId is required for partial picking");
        // }
        //
        // await using var transaction = await db.Database.BeginTransactionAsync();
        // try {
        //     // 1. Validate source package and item availability
        //     var sourcePackageContent = await db.PackageContents
        //         .FirstOrDefaultAsync(pc => pc.PackageId == request.SourcePackageId.Value &&
        //                                    pc.ItemCode == request.ItemCode);
        //
        //     if (sourcePackageContent == null) {
        //         throw new ValidationException($"Item {request.ItemCode} not found in source package");
        //     }
        //
        //     var availableQuantity = sourcePackageContent.Quantity - sourcePackageContent.CommittedQuantity;
        //     if (availableQuantity < request.Quantity) {
        //         throw new ValidationException($"Insufficient quantity available. Available: {availableQuantity}, Requested: {request.Quantity}");
        //     }
        //
        //     // 2. Create commitment for picked quantity
        //     sourcePackageContent.CommittedQuantity += request.Quantity;
        //     db.PackageContents.Update(sourcePackageContent);
        //
        //     var commitment = new PackageCommitment {
        //         PackageId             = request.SourcePackageId.Value,
        //         ItemCode              = request.ItemCode,
        //         Quantity              = request.Quantity,
        //         SourceOperationType   = ObjectType.Picking,
        //         SourceOperationId     = new Guid(request.ID.ToString().PadLeft(32, '0')),
        //         SourceOperationLineId = request.PickEntry.HasValue ? new Guid(request.PickEntry.Value.ToString().PadLeft(32, '0')) : null,
        //         CommittedAt           = DateTime.UtcNow,
        //         CreatedAt             = DateTime.UtcNow,
        //         CreatedByUserId       = sessionInfo.Guid
        //     };
        //
        //     db.PackageCommitments.Add(commitment);
        //
        //     // 3. Add item to target package if specified
        //     if (request.PackageId.HasValue) {
        //         await packageContentService.AddItemToPackageAsync(new AddItemToPackageRequest {
        //             PackageId           = request.PackageId.Value,
        //             ItemCode            = request.ItemCode,
        //             Quantity            = request.Quantity,
        //             UnitQuantity        = request.Quantity,
        //             UnitType            = request.Unit,
        //             SourceOperationType = ObjectType.Picking,
        //             SourceOperationId   = new Guid(request.ID.ToString().PadLeft(32, '0')),
        //             BinEntry            = request.BinEntry,
        //             WhsCode             = sessionInfo.Warehouse
        //         }, sessionInfo);
        //     }
        //
        //     await db.SaveChangesAsync();
        //     await transaction.CommitAsync();
        //
        //     return new PickListAddItemResponse {
        //         Success         = true,
        //         ItemCode        = request.ItemCode,
        //         Quantity        = request.Quantity,
        //         SourcePackageId = request.SourcePackageId,
        //         TargetPackageId = request.PackageId
        //     };
        // }
        // catch {
        //     await transaction.RollbackAsync();
        //     throw;
        // }
    }

    public async Task ClearPickListCommitmentsAsync(int absEntry, int pickEntry, SessionInfo sessionInfo) {
        throw new NotImplementedException();
        // // Get all package commitments for this specific pick operation
        // var commitments = await db.PackageCommitments
        //     .Where(pc => pc.SourceOperationType == ObjectType.Picking &&
        //                  pc.SourceOperationId == new Guid(absEntry.ToString().PadLeft(32, '0')) &&
        //                  pc.SourceOperationLineId == new Guid(pickEntry.ToString().PadLeft(32, '0')))
        //     .ToListAsync();
        //
        // foreach (var commitment in commitments) {
        //     // Find the corresponding package content and reduce committed quantity
        //     var packageContent = await db.PackageContents
        //         .FirstOrDefaultAsync(pc => pc.PackageId == commitment.PackageId &&
        //                                    pc.ItemCode == commitment.ItemCode);
        //
        //     if (packageContent != null) {
        //         packageContent.CommittedQuantity -= commitment.Quantity;
        //         db.PackageContents.Update(packageContent);
        //     }
        //
        //     // Remove the commitment record
        //     db.PackageCommitments.Remove(commitment);
        // }
        //
        // // Remove PickListPackage records for this operation
        // var pickListPackages = await db.PickListPackages
        //     .Where(plp => plp.AbsEntry == absEntry && plp.PickEntry == pickEntry)
        //     .ToListAsync();
        //
        // db.PickListPackages.RemoveRange(pickListPackages);
        //
        // await db.SaveChangesAsync();
    }

    public async Task ClearAllPickListCommitmentsAsync(int absEntry, SessionInfo sessionInfo) {
        throw new NotImplementedException();
        // // Get all package commitments for this entire pick operation (all pick entries)
        // var commitments = await db.PackageCommitments
        //     .Where(pc => pc.SourceOperationType == ObjectType.Picking &&
        //                  pc.SourceOperationId == new Guid(absEntry.ToString().PadLeft(32, '0')))
        //     .ToListAsync();
        //
        // foreach (var commitment in commitments) {
        //     // Find the corresponding package content and reduce committed quantity
        //     var packageContent = await db.PackageContents
        //         .FirstOrDefaultAsync(pc => pc.PackageId == commitment.PackageId &&
        //                                    pc.ItemCode == commitment.ItemCode);
        //
        //     if (packageContent != null) {
        //         packageContent.CommittedQuantity -= commitment.Quantity;
        //         db.PackageContents.Update(packageContent);
        //     }
        //
        //     // Remove the commitment record
        //     db.PackageCommitments.Remove(commitment);
        // }
        //
        // // Remove all PickListPackage records for this operation
        // var pickListPackages = await db.PickListPackages
        //     .Where(plp => plp.AbsEntry == absEntry)
        //     .ToListAsync();
        //
        // db.PickListPackages.RemoveRange(pickListPackages);
        //
        // await db.SaveChangesAsync();
    }
}