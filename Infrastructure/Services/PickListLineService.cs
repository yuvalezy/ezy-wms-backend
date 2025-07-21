using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListLineService(SystemDbContext db, IExternalSystemAdapter adapter, ILogger<PickListService> logger, ISettings settings, IPackageContentService packageContentService) : IPickListLineService {
    public async Task<PickListAddItemResponse> AddItem(SessionInfo sessionInfo, PickListAddItemRequest request) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            if (request.Unit != UnitType.Unit) {
                var items = await adapter.ItemCheckAsync(request.ItemCode, null);
                var item  = items.FirstOrDefault();
                if (item == null) {
                    throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, BarCode = (string?)null });
                }

                request.Quantity *= item.NumInBuy * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
            }

            // Variables to hold package data if PackageId is provided
            Package? package = null;
            PackageContent? packageContent = null;

            // Handle package-specific logic if PackageId is provided
            if (request.PackageId.HasValue) {
                // Load package with contents
                package = await db.Packages
                    .Include(p => p.Contents)
                    .FirstOrDefaultAsync(p => p.Id == request.PackageId.Value);

                if (package == null) {
                    return new PickListAddItemResponse {
                        ErrorMessage = $"Package {request.PackageId} not found",
                        Status = ResponseStatus.Error
                    };
                }

                // Validate package status
                if (package.Status == PackageStatus.Locked) {
                    return new PickListAddItemResponse {
                        ErrorMessage = "Package is locked",
                        Status = ResponseStatus.Error
                    };
                }

                if (package.Status != PackageStatus.Active) {
                    return new PickListAddItemResponse {
                        ErrorMessage = "Package is not active",
                        Status = ResponseStatus.Error
                    };
                }

                // Validate bin location if specified
                if (request.BinEntry.HasValue && package.BinEntry != request.BinEntry.Value) {
                    return new PickListAddItemResponse {
                        ErrorMessage = "Package is not in the specified bin location",
                        Status = ResponseStatus.Error
                    };
                }

                // Find the specific package content for the requested item
                packageContent = package.Contents.FirstOrDefault(c => c.ItemCode == request.ItemCode);
                if (packageContent == null) {
                    return new PickListAddItemResponse {
                        ErrorMessage = $"Item {request.ItemCode} not found in package {request.PackageId}",
                        Status = ResponseStatus.Error
                    };
                }

                // Check available quantity
                var availableQuantity = packageContent.Quantity - packageContent.CommittedQuantity;
                if (request.Quantity > availableQuantity) {
                    return new PickListAddItemResponse {
                        ErrorMessage = $"Insufficient quantity in package. Available: {availableQuantity}, Requested: {request.Quantity}",
                        Status = ResponseStatus.Error
                    };
                }
            }

            // Validate the add item request
            var validationResults = await adapter.ValidatePickingAddItem(request);

            if (validationResults.Length == 0) {
                return new PickListAddItemResponse {
                    ErrorMessage = "Item entry not found in pick",
                    Status       = ResponseStatus.Error,
                };
            }

            if (!validationResults[0].IsValid)
                return new PickListAddItemResponse {
                    ErrorMessage = validationResults[0].ErrorMessage,
                    Status       = ResponseStatus.Error
                };

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
                return new PickListAddItemResponse {
                    Status       = ResponseStatus.Error,
                    ErrorMessage = "Quantity exceeds open quantity",
                };
            }

            check.ValidationResult.OpenQuantity -= check.PickedQuantity;

            if (request.Quantity > binOnHand) {
                return new PickListAddItemResponse {
                    Status       = ResponseStatus.Error,
                    ErrorMessage = "Quantity exceeds bin available stock",
                };
            }

            var pickList = new PickList {
                Id              = Guid.NewGuid(),
                AbsEntry        = request.ID,
                PickEntry       = check.ValidationResult.PickEntry ?? request.PickEntry ?? 0,
                ItemCode        = request.ItemCode,
                Quantity        = request.Quantity,
                BinEntry        = request.BinEntry,
                Unit            = request.Unit,
                Status          = ObjectStatus.Open,
                SyncStatus      = SyncStatus.Pending,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = sessionInfo.Guid
            };

            await db.PickLists.AddAsync(pickList);

            // Handle package-specific updates if PackageId is provided
            if (request.PackageId.HasValue && packageContent != null && package != null) {
                // Update committed quantity
                packageContent.CommittedQuantity += request.Quantity;
                db.PackageContents.Update(packageContent);

                // Create package commitment
                var commitment = new PackageCommitment {
                    Id                  = Guid.NewGuid(),
                    PackageId           = request.PackageId.Value,
                    ItemCode            = request.ItemCode,
                    Quantity            = request.Quantity,
                    SourceOperationType = ObjectType.Picking,
                    SourceOperationId   = pickList.Id,
                    CommittedAt         = DateTime.UtcNow,
                    CreatedAt           = DateTime.UtcNow,
                    CreatedByUserId     = sessionInfo.Guid
                };
                db.PackageCommitments.Add(commitment);

                // Check if PickListPackage already exists for this pick list and package
                var existingPickListPackage = await db.PickListPackages
                    .FirstOrDefaultAsync(plp => plp.AbsEntry == request.ID &&
                                                plp.PackageId == request.PackageId.Value);

                if (existingPickListPackage == null) {
                    // Create new PickListPackage record
                    var pickListPackage = new PickListPackage {
                        Id              = Guid.NewGuid(),
                        AbsEntry        = request.ID,
                        PickEntry       = pickList.PickEntry,
                        PackageId       = request.PackageId.Value,
                        Type            = SourceTarget.Source,
                        BinEntry        = request.BinEntry ?? package.BinEntry,
                        AddedAt         = DateTime.UtcNow,
                        AddedByUserId   = sessionInfo.Guid,
                        CreatedAt       = DateTime.UtcNow,
                        CreatedByUserId = sessionInfo.Guid
                    };
                    db.PickListPackages.Add(pickListPackage);
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return PickListAddItemResponse.OkResponse;
        } catch (Exception ex) {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error adding item to pick list");
            throw;
        }
    }
}