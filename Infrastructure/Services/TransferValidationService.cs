using Core.DTOs.Transfer;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class TransferValidationService(SystemDbContext db, IExternalSystemAdapter adapter) : ITransferValidationService {
    
    public async Task<bool> ValidateAddItemAsync(SessionInfo sessionInfo, TransferAddItemRequest request) {
        // Validate bin is required but not provided
        if (!request.BinEntry.HasValue && sessionInfo.EnableBinLocations) {
            throw new ApiErrorException((int)AddItemReturnValueType.BinMissing, new { });
        }

        // Check if transfer exists and is in valid state
        var transfer = await db.Transfers
            .Where(t => t.Id == request.ID)
            .Select(t => new { t.Status })
            .FirstOrDefaultAsync();

        if (transfer == null) {
            throw new ApiErrorException((int)AddItemReturnValueType.TransactionIDNotExists, new { request.ID });
        }

        if (transfer.Status != ObjectStatus.Open && transfer.Status != ObjectStatus.InProgress) {
            return false; // Transfer is closed
        }

        // Get validation data from external system
        var validationResult = await adapter.GetItemValidationInfo(request.ItemCode, request.BarCode, sessionInfo.Warehouse, request.BinEntry, sessionInfo.EnableBinLocations);

        // Validate item exists
        if (!validationResult.IsValidItem) {
            throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, request.BarCode });
        }

        // Validate barcode matches
        if (!validationResult.IsValidBarCode) {
            throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeBarCodeMismatch, new { request.ItemCode, request.BarCode });
        }

        // Validate it's an inventory item
        if (!validationResult.IsInventoryItem) {
            throw new ApiErrorException((int)AddItemReturnValueType.NotStockItem, new { request.ItemCode, request.BarCode });
        }

        // Validate item exists in warehouse
        if (!validationResult.ItemExistsInWarehouse) {
            throw new ApiErrorException((int)AddItemReturnValueType.ItemNotInWarehouse, new { request.ItemCode, request.BarCode });
        }

        // Validate bin existence if provided
        if (request.BinEntry.HasValue) {
            if (!validationResult.BinExists) {
                throw new ApiErrorException((int)AddItemReturnValueType.BinNotExists, new { BinEntry = request.BinEntry.Value });
            }

            if (!validationResult.BinBelongsToWarehouse) {
                throw new ApiErrorException((int)AddItemReturnValueType.BinNotInWarehouse, new { validationResult.BinCode });
            }
        }

        // Calculate total quantity including unit conversion
        int totalQuantity = request.Quantity;
        if (request.Unit != UnitType.Unit) {
            totalQuantity *= validationResult.NumInBuy * (request.Unit == UnitType.Pack ? validationResult.PurPackUn : 1);
        }

        // Calculate existing quantities from database
        var existingQuantities = await db.TransferLines
            .Where(tl => tl.TransferId == request.ID &&
                         tl.ItemCode == request.ItemCode &&
                         tl.LineStatus != LineStatus.Closed)
            .GroupBy(tl => tl.Type)
            .Select(g => new {
                Type          = g.Key,
                TotalQuantity = g.Sum(tl => tl.Quantity)
            })
            .ToListAsync();

        int sourceQuantity = 0;
        int targetQuantity = 0;

        foreach (var eq in existingQuantities) {
            switch (eq.Type) {
                case SourceTarget.Source:
                    sourceQuantity = eq.TotalQuantity;
                    break;
                case SourceTarget.Target:
                    targetQuantity = eq.TotalQuantity;
                    break;
            }
        }

        if (!request.BinEntry.HasValue)
            return true;

        // If specific bin is requested, filter by bin
        var binSpecificQuantities = await db.TransferLines
            .Where(tl => tl.TransferId == request.ID &&
                         tl.ItemCode == request.ItemCode &&
                         tl.BinEntry == request.BinEntry.Value &&
                         tl.LineStatus != LineStatus.Closed)
            .GroupBy(tl => tl.Type)
            .Select(g => new {
                Type          = g.Key,
                TotalQuantity = g.Sum(tl => tl.Quantity)
            })
            .ToListAsync();

        int binSourceQuantity = binSpecificQuantities
            .Where(x => x.Type == SourceTarget.Source)
            .Select(x => x.TotalQuantity)
            .FirstOrDefault();

        switch (request.Type) {
            // Validate quantity availability
            case SourceTarget.Source: {
                // Get quantity locked in packages for this item and bin
                var packageStatuses = new[] { PackageStatus.Active, PackageStatus.Locked };
                decimal packagedQuantity = await db.PackageContents
                    .Where(pc => pc.ItemCode == request.ItemCode &&
                                 pc.BinEntry == request.BinEntry.Value &&
                                 packageStatuses.Contains(pc.Package.Status))
                    .SumAsync(pc => pc.Quantity);

                decimal availableInBin = validationResult.AvailableQuantity - binSourceQuantity - packagedQuantity;
                if (availableInBin < totalQuantity) {
                    throw new ApiErrorException((int)AddItemReturnValueType.QuantityMoreAvailable, new { request.ItemCode });
                }

                break;
            }
            case SourceTarget.Target: {
                decimal availableToTransfer = sourceQuantity - targetQuantity;
                if (availableToTransfer < totalQuantity) {
                    throw new ApiErrorException((int)AddItemReturnValueType.QuantityMoreAvailable, new { request.ItemCode });
                }

                break;
            }
        }

        return true;
    }
}