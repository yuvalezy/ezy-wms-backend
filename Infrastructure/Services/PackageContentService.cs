using Core.DTOs.Items;
using Core.DTOs.Package;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PackageContentService(SystemDbContext context, IExternalSystemAdapter adapter, ILogger<PackageContentService> logger) : IPackageContentService {
    public async Task<PackageContent> AddItemToPackageAsync(AddItemToPackageRequest request, SessionInfo sessionInfo) {
        var package = await context.Packages
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == request.PackageId && !p.Deleted);

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

        var      unit         = request.UnitType;
        decimal? unitQuantity = request.UnitQuantity;
        if (unitQuantity == null) {
            unitQuantity = request.Quantity;
            if (unit != UnitType.Unit) {
                var data = await adapter.GetItemPurchaseUnits(request.ItemCode);
                unitQuantity *= data.QuantityInUnit;
                if (unit == UnitType.Pack)
                    unitQuantity *= data.QuantityInPack;
            }
        }

        if (existingContent != null) {
            existingContent.Quantity        += unitQuantity.Value;
            existingContent.UpdatedAt       =  DateTime.UtcNow;
            existingContent.UpdatedByUserId =  sessionInfo.Guid;

            await LogPackageTransactionAsync(new LogPackageTransactionRequest {
                PackageId             = request.PackageId,
                TransactionType       = PackageTransactionType.Add,
                ItemCode              = request.ItemCode,
                Quantity              = request.Quantity,
                UnitQuantity          = unitQuantity.Value,
                UnitType              = unit,
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
            Quantity  = unitQuantity.Value,
            WhsCode   = sessionInfo.Warehouse,
            BinEntry  = request.BinEntry,
            CreatedBy = sessionInfo.Guid,
        };

        context.PackageContents.Add(content);

        await LogPackageTransactionAsync(new LogPackageTransactionRequest {
            PackageId             = request.PackageId,
            TransactionType       = PackageTransactionType.Add,
            ItemCode              = request.ItemCode,
            Quantity              = request.Quantity,
            UnitQuantity          = unitQuantity.Value,
            UnitType              = unit,
            SourceOperationType   = request.SourceOperationType ?? ObjectType.Package,
            SourceOperationId     = request.SourceOperationId,
            SourceOperationLineId = request.SourceOperationLineId,
            UserId                = sessionInfo.Guid,
            Notes                 = "Item added to package"
        });

        await context.SaveChangesAsync();

        logger.LogInformation("Item {ItemCode} added to package {Barcode}: {Quantity} {UnitCode}",
            request.ItemCode, package.Barcode, request.Quantity, unit);

        return content;
    }

    public async Task<PackageContent> RemoveItemFromPackageAsync(RemoveItemFromPackageRequest request, SessionInfo sessionInfo) {
        var package = await context.Packages
            .FirstOrDefaultAsync(p => p.Id == request.PackageId && !p.Deleted);

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

        decimal?           unitQuantity = request.UnitQuantity;
        var               unit         = request.UnitType;
        ItemUnitResponse? data         = null;

        if (unitQuantity == null) {
            unitQuantity = request.Quantity;
            if (unit != UnitType.Unit) {
                data         =  await adapter.GetItemPurchaseUnits(request.ItemCode);
                unitQuantity *= data.QuantityInUnit;
                if (unit == UnitType.Pack)
                    unitQuantity *= data.QuantityInPack;
            }
        }


        if (content.Quantity < unitQuantity.Value) {
            decimal availableQuantity = content.Quantity;
            if (unit != UnitType.Unit) {
                availableQuantity /= data!.QuantityInUnit;
                if (unit == UnitType.Pack)
                    availableQuantity /= data.QuantityInPack;
            }

            throw new InvalidOperationException($"Insufficient quantity. Available: {availableQuantity}, Requested: {request.Quantity}");
        }

        content.Quantity -= unitQuantity.Value;

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
            UnitQuantity        = unitQuantity.Value,
            UnitType            = request.UnitType,
            SourceOperationType = request.SourceOperationType ?? ObjectType.Package,
            SourceOperationId   = request.SourceOperationId,
            UserId              = sessionInfo.Guid,
            Notes               = "Item removed from package"
        });

        await context.SaveChangesAsync();

        logger.LogInformation("Item {ItemCode} removed from package {Barcode}: {Quantity} {UnitCode}",
            request.ItemCode, package.Barcode, request.Quantity, request.UnitType);

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

    public async Task<IEnumerable<PackageTransaction>> GetPackageTransactionHistoryAsync(Guid packageId) {
        return await context.PackageTransactions
            .Where(t => t.PackageId == packageId && !t.Deleted)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task LogPackageTransactionAsync(LogPackageTransactionRequest request) {
        var transaction = new PackageTransaction {
            Id                    = Guid.NewGuid(),
            PackageId             = request.PackageId,
            TransactionType       = request.TransactionType,
            ItemCode              = request.ItemCode,
            Quantity              = request.Quantity,
            UnitQuantity          = request.UnitQuantity,
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
}