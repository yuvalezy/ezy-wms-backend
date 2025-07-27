using System.Globalization;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListLineService(SystemDbContext db, IExternalSystemAdapter adapter, ILogger<PickListService> logger) : IPickListLineService {
    public async Task<PickListAddItemResponse> AddItem(SessionInfo sessionInfo, PickListAddItemRequest request) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            if (request.Unit != UnitType.Unit) {
                var items = await adapter.ItemCheckAsync(request.ItemCode, null);
                var item = items.FirstOrDefault();
                if (item == null) {
                    throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, BarCode = (string?)null });
                }

                request.Quantity *= item.NumInBuy * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
            }

            // Handle package-specific logic if PackageId is provided
            var (package, packageContent, packageValidation) = await ValidateAddItemPackage(request);
            if (packageValidation != null) {
                return packageValidation;
            }

            // Validate the add item request
            var validationResults = await adapter.ValidatePickingAddItem(request);

            if (validationResults.Length == 0) {
                return PickListAddItemResponse.Error("Item entry not found in pick");
            }

            if (!validationResults[0].IsValid)
                return PickListAddItemResponse.Error(validationResults[0].ErrorMessage!);

            int result = db.PickLists
            .Where(p => p.ItemCode == request.ItemCode && p.BinEntry == request.BinEntry && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .Select(p => p.Quantity)
            .Concat(
                db.TransferLines
                .Where(t => t.ItemCode == request.ItemCode && t.BinEntry == request.BinEntry && (t.LineStatus == LineStatus.Open || t.LineStatus == LineStatus.Processing))
                .Select(t => t.Quantity)
            )
            .Sum();

            int binOnHand = validationResults.First().BinOnHand - result;

            var dbPickedQuantity = await db.PickLists.Where(v => v.AbsEntry == request.ID && v.ItemCode == request.ItemCode && (v.Status == ObjectStatus.Open || v.Status == ObjectStatus.Processing))
            .GroupBy(v => v.PickEntry)
            .Select(v => new { PickEntry = v.Key, Quantity = v.Sum(vv => vv.Quantity) })
            .ToArrayAsync();

            var check = (from v in validationResults.Where(a => a.IsValid)
                join p in dbPickedQuantity on v.PickEntry equals p.PickEntry into gj
                from sub in gj.DefaultIfEmpty()
                where v.OpenQuantity - (sub?.Quantity ?? 0) >= 0
                select new { ValidationResult = v, PickedQuantity = sub?.Quantity ?? 0 })
            .FirstOrDefault();

            if (check == null) {
                return PickListAddItemResponse.Error("Quantity exceeds open quantity");
            }

            check.ValidationResult.OpenQuantity -= check.PickedQuantity;

            if (request.Quantity > binOnHand) {
                return PickListAddItemResponse.Error("Quantity exceeds bin available stock");
            }

            var pickList = new PickList {
                Id = Guid.NewGuid(),
                AbsEntry = request.ID,
                PickEntry = check.ValidationResult.PickEntry ?? request.PickEntry ?? 0,
                ItemCode = request.ItemCode,
                Quantity = request.Quantity,
                BinEntry = request.BinEntry,
                Unit = request.Unit,
                Status = ObjectStatus.Open,
                SyncStatus = SyncStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = sessionInfo.Guid
            };

            await db.PickLists.AddAsync(pickList);

            await AddItemPackage(sessionInfo, pickList, package, packageContent, request);
        
            await AddNewPackageContent(sessionInfo, request);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return PickListAddItemResponse.OkResponse;
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error adding item to pick list");
            throw;
        }
    }

    private async Task AddItemPackage(SessionInfo sessionInfo, PickList pickList, Package? package, PackageContent? packageContent, PickListAddItemRequest request) {
        if (!request.PackageId.HasValue || packageContent == null || package == null) {
            return;
        }

        packageContent.CommittedQuantity += request.Quantity;
        db.PackageContents.Update(packageContent);

        // Create package commitment
        var commitment = new PackageCommitment {
            Id = Guid.NewGuid(),
            PackageId = request.PackageId.Value,
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

        // Check if PickListPackage already exists for this pick list and package
        var existingPickListPackage = await db.PickListPackages
        .FirstOrDefaultAsync(plp => plp.AbsEntry == request.ID &&
                                    plp.PackageId == request.PackageId.Value);

        // Create new PickListPackage record
        if (existingPickListPackage == null) {
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
        }
    }
    private async Task AddNewPackageContent(SessionInfo sessionInfo, PickListAddItemRequest request) {
        // If content is added to a new picking package
        if (request.PickingPackageId != null) {
            var pickingPackage = await db.Packages.Include(v => v.Contents).FirstOrDefaultAsync(v => v.Id == request.PickingPackageId.Value);
            if (pickingPackage == null) {
                throw new KeyNotFoundException($"Picking package {request.PickingPackageId} not found");
            }
            var pickingPackageContent = pickingPackage.Contents.FirstOrDefault(c => c.ItemCode == request.ItemCode);
            if (pickingPackageContent == null) {
                pickingPackageContent = new PackageContent {
                    CreatedByUserId = sessionInfo.Guid,
                    PackageId = request.PickingPackageId.Value,
                    ItemCode = request.ItemCode,
                    Quantity = request.Quantity,
                    CommittedQuantity = 0,
                    WhsCode = sessionInfo.Warehouse,
                    BinEntry = request.BinEntry,
                };
                await db.PackageContents.AddAsync(pickingPackageContent);
            }
            else {
                pickingPackageContent.Quantity += request.Quantity;
                db.PackageContents.Update(pickingPackageContent);
            }
        }
    }

    private async Task<(Package?, PackageContent?, PickListAddItemResponse?)> ValidateAddItemPackage(PickListAddItemRequest request) {
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
        if (package.Status == PackageStatus.Locked) {
            return (null, null, PickListAddItemResponse.Error("Package is locked"));
        }

        if (package.Status != PackageStatus.Active) {
            return (null, null, PickListAddItemResponse.Error("Package is not active"));
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
}