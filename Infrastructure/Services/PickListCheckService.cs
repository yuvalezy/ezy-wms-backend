using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListCheckService(SystemDbContext dbContext, IPickListService pickListService, ISettings settings, IExternalSystemAdapter adapter, ILogger<PickListCheckService> logger)
: IPickListCheckService {
    public async Task<Core.Entities.PickListCheckSession?> StartCheck(int pickListId, SessionInfo sessionInfo) {
        if (!settings.Options.EnablePickingCheck) {
            throw new Exception("Picking check is not enabled");
        }

        // Check if an active session already exists
        var existingSession = await dbContext.PickListCheckSessions
        .Include(s => s.CheckedItems)
        .FirstOrDefaultAsync(s => s.PickListId == pickListId && !s.IsCompleted && !s.IsCancelled && !s.Deleted);

        if (existingSession != null) {
            return existingSession;
        }

        // Verify pick list exists
        var pickList = await pickListService.GetPickList(pickListId, new PickListDetailRequest(), sessionInfo.Warehouse);
        if (pickList == null) {
            return null;
        }

        var session = new Core.Entities.PickListCheckSession {
            PickListId = pickListId,
            StartedByUserId = sessionInfo.Guid,
            StartedByUserName = sessionInfo.Name,
            StartedAt = DateTime.UtcNow,
            IsCompleted = false,
            IsCancelled = false,
            CreatedByUserId = sessionInfo.Guid,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PickListCheckSessions.Add(session);
        await dbContext.SaveChangesAsync();
        return session;
    }

    public async Task<PickListCheckItemResponse> CheckItem(PickListCheckItemRequest request, SessionInfo sessionInfo) {
        if (!settings.Options.EnablePickingCheck) {
            throw new Exception("Picking check is not enabled");
        }

        var pickListCheck = await dbContext.PickListCheckSessions
        .Include(s => s.CheckedItems)
        .FirstOrDefaultAsync(s => s.PickListId == request.PickListId && !s.IsCompleted && !s.IsCancelled && !s.Deleted);

        if (pickListCheck == null) {
            return new PickListCheckItemResponse {
                Success = false,
                ErrorMessage = "No active check session found",
                Status = ResponseStatus.Error
            };
        }

        if (pickListCheck.IsCompleted) {
            return new PickListCheckItemResponse {
                Success = false,
                ErrorMessage = "Check session is already completed",
                Status = ResponseStatus.Error
            };
        }

        if (request.Unit != UnitType.Unit) {
            var itemInfo = await adapter.GetItemInfo(request.ItemCode);
            request.CheckedQuantity *= itemInfo.QuantityInUnit;
            if (request.Unit == UnitType.Pack) {
                request.CheckedQuantity *= itemInfo.QuantityInPack;
            }
        }

        // Add new item
        var newItem = new Core.Entities.PickListCheckItem {
            CheckSessionId = pickListCheck.Id,
            ItemCode = request.ItemCode,
            CheckedQuantity = request.CheckedQuantity,
            Unit = request.Unit,
            BinEntry = request.BinEntry,
            CheckedAt = DateTime.UtcNow,
            CheckedByUserId = sessionInfo.Guid,
            CreatedByUserId = sessionInfo.Guid,
            CreatedAt = DateTime.UtcNow
        };

        pickListCheck.CheckedItems.Add(newItem);

        await dbContext.SaveChangesAsync();

        // Get pick list details to calculate progress
        var pickList = await pickListService.GetPickList(
            request.PickListId,
            new PickListDetailRequest { AvailableBins = false },
            sessionInfo.Warehouse
        );

        var totalItems = pickList?.Detail?.SelectMany(d => d.Items ?? []).Count() ?? 0;

        return new PickListCheckItemResponse {
            Success = true,
            ItemsChecked = pickListCheck.CheckedItems.Count,
            TotalItems = totalItems,
            Status = ResponseStatus.Ok
        };
    }

    public async Task<PickListCheckPackageResponse> CheckPackage(PickListCheckPackageRequest request, SessionInfo sessionInfo) {
        if (!settings.Options.EnablePickingCheck) {
            throw new Exception("Picking check is not enabled");
        }

        var session = await dbContext.PickListCheckSessions
        .Include(s => s.CheckedItems)
        .Include(s => s.CheckedPackages)
        .FirstOrDefaultAsync(s => s.PickListId == request.PickListId && !s.IsCompleted && !s.IsCancelled && !s.Deleted);

        if (session == null) {
            return new PickListCheckPackageResponse {
                Success = false,
                ErrorMessage = "No active check session found",
                Status = ResponseStatus.Error
            };
        }

        // Check if package was already scanned in this session
        var packageAlreadyScanned = await dbContext.PickListCheckPackages
        .AnyAsync(cp => cp.CheckSessionId == session.Id && cp.PackageId == request.PackageId);

        if (packageAlreadyScanned) {
            return new PickListCheckPackageResponse {
                Success = false,
                ErrorMessage = "Package has already been scanned in this check session",
                Status = ResponseStatus.Error
            };
        }

        // Load package with contents
        var package = await dbContext.Packages
        .Include(p => p.Contents)
        .FirstOrDefaultAsync(p => p.Id == request.PackageId);

        if (package == null) {
            return new PickListCheckPackageResponse {
                Success = false,
                ErrorMessage = "Package not found",
                Status = ResponseStatus.Error
            };
        }

        // Get package commitments for this pick list
        var pickListIds = await dbContext.PickLists
        .Where(p => p.AbsEntry == request.PickListId)
        .Select(p => p.Id)
        .ToListAsync();

        var packageCommitments = await dbContext.PackageCommitments
        .Where(pc => pc.PackageId == request.PackageId &&
                     pc.SourceOperationType == ObjectType.Picking &&
                     pickListIds.Contains(pc.SourceOperationId))
        .ToListAsync();

        if (!packageCommitments.Any()) {
            return new PickListCheckPackageResponse {
                Success = false,
                ErrorMessage = "Package is not committed to this pick list",
                Status = ResponseStatus.Error
            };
        }

        // Create PickListCheckPackage record
        var checkPackage = new PickListCheckPackage {
            CheckSessionId = session.Id,
            PackageId = package.Id,
            PackageBarcode = package.Barcode,
            CheckedAt = DateTime.UtcNow,
            CheckedByUserId = sessionInfo.Guid,
            CreatedByUserId = sessionInfo.Guid,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PickListCheckPackages.Add(checkPackage);

        // Process each committed item in the package
        var checkedItems = new List<CheckedPackageItem>();

        foreach (var commitment in packageCommitments) {
            // Check if item already exists in this session
            var existingItem = session.CheckedItems.FirstOrDefault(i => i.ItemCode == commitment.ItemCode);

            if (existingItem != null) {
                // Update existing item by adding the committed quantity
                existingItem.CheckedQuantity += (int)commitment.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                existingItem.UpdatedByUserId = sessionInfo.Guid;
            }
            else {
                // Add new item
                var newItem = new Core.Entities.PickListCheckItem {
                    CheckSessionId = session.Id,
                    ItemCode = commitment.ItemCode,
                    CheckedQuantity = (int)commitment.Quantity,
                    Unit = UnitType.Unit,
                    BinEntry = package.BinEntry,
                    CheckedAt = DateTime.UtcNow,
                    CheckedByUserId = sessionInfo.Guid,
                    CreatedByUserId = sessionInfo.Guid,
                    CreatedAt = DateTime.UtcNow
                };

                session.CheckedItems.Add(newItem);
            }

            // Get item name for response
            var itemInfo = await adapter.GetItemInfo(commitment.ItemCode);

            checkedItems.Add(new CheckedPackageItem {
                ItemCode = commitment.ItemCode,
                ItemName = itemInfo.ItemName,
                Quantity = (int)commitment.Quantity
            });
        }

        await dbContext.SaveChangesAsync();

        // Get pick list details to calculate progress
        var pickList = await pickListService.GetPickList(
            request.PickListId,
            new PickListDetailRequest { AvailableBins = false },
            sessionInfo.Warehouse
        );

        var totalItems = pickList?.Detail?.SelectMany(d => d.Items ?? []).Count() ?? 0;

        return new PickListCheckPackageResponse {
            Success = true,
            ItemsChecked = session.CheckedItems.Count,
            TotalItems = totalItems,
            PackageBarcode = package.Barcode,
            CheckedItems = checkedItems,
            Status = ResponseStatus.Ok
        };
    }

    public async Task<PickListCheckSummaryResponse> GetCheckSummary(int pickListId, string warehouse) {
        if (!settings.Options.EnablePickingCheck) {
            throw new Exception("Picking check is not enabled");
        }

        var session = await dbContext.PickListCheckSessions
        .Include(s => s.CheckedItems)
        .Include(s => s.CheckedPackages)
        .FirstOrDefaultAsync(s => s.PickListId == pickListId && !s.Deleted);

        if (session == null) {
            return new PickListCheckSummaryResponse {
                PickListId = pickListId,
                Items = []
            };
        }

        // Get pick list details
        var pickList = await pickListService.GetPickList(
            pickListId,
            new PickListDetailRequest { PickCheck = true },
            warehouse
        );

        var summary = new PickListCheckSummaryResponse {
            PickListId = pickListId,
            CheckStartedAt = session.StartedAt,
            CheckStartedBy = session.StartedByUserName,
            Items = []
        };

        if (pickList?.Detail != null) {
            foreach (var detail in pickList.Detail) {
                foreach (var item in detail.Items ?? []) {
                    var checkedItems = session.CheckedItems.Where(ci => ci.ItemCode == item.ItemCode);
                    var checkedQty = checkedItems.Sum(checkedItem => checkedItem.CheckedQuantity);
                    var difference = checkedQty - item.Picked;

                    summary.Items.Add(new PickListCheckItemDetail {
                        ItemCode = item.ItemCode,
                        ItemName = item.ItemName,
                        PickedQuantity = item.Picked,
                        CheckedQuantity = checkedQty,
                        Difference = difference,
                        UnitMeasure = item.BuyUnitMsr,
                        QuantityInUnit = item.NumInBuy,
                        PackMeasure = item.PurPackMsr,
                        QuantityInPack = item.PurPackUn,
                    });

                    if (difference != 0) {
                        summary.DiscrepancyCount++;
                    }
                }
            }
        }

        summary.TotalItems = summary.Items.Count;
        summary.ItemsChecked = session.CheckedItems.Count;

        return summary;
    }

    public async Task<bool> CompleteCheck(int pickListId, Guid userId) {
        if (!settings.Options.EnablePickingCheck) {
            throw new Exception("Picking check is not enabled");
        }

        var session = await dbContext.PickListCheckSessions
        .Include(s => s.CheckedItems)
        .FirstOrDefaultAsync(s => s.PickListId == pickListId && !s.IsCompleted && !s.IsCancelled && !s.Deleted);

        if (session == null) {
            return false;
        }

        session.IsCompleted = true;
        session.CompletedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Pick list check completed. PickListId: {PickListId}, CompletedBy: {UserId}, ItemsChecked: {ItemCount}",
            pickListId, userId, session.CheckedItems.Count
        );

        return true;
    }

    public async Task<bool> CancelCheck(int pickListId, Guid userId) {
        if (!settings.Options.EnablePickingCheck) {
            throw new Exception("Picking check is not enabled");
        }

        var session = await dbContext.PickListCheckSessions
        .FirstOrDefaultAsync(s => s.PickListId == pickListId && !s.IsCompleted && !s.IsCancelled && !s.Deleted);

        if (session == null) {
            return false;
        }

        session.IsCancelled = true;
        session.CancelledAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Pick list check cancelled. PickListId: {PickListId}, CancelledBy: {UserId}",
            pickListId, userId
        );

        return true;
    }

    public async Task<Core.Entities.PickListCheckSession?> GetActiveCheckSession(int pickListId) {
        if (!settings.Options.EnablePickingCheck) {
            throw new Exception("Picking check is not enabled");
        }

        return await dbContext.PickListCheckSessions
        .Include(s => s.CheckedItems)
        .FirstOrDefaultAsync(s => s.PickListId == pickListId && !s.IsCompleted && !s.IsCancelled && !s.Deleted);
    }
}