using Core.DTOs.PickList;
using Core.Entities;
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

        var itemInfo = await adapter.GetItemInfo(request.ItemCode);
        if (request.Unit != UnitType.Unit) {
            request.CheckedQuantity *= itemInfo.QuantityInUnit;
            if (request.Unit == UnitType.Pack) {
                request.CheckedQuantity *= itemInfo.QuantityInPack;
            }
        }
        request.CheckedQuantity *= itemInfo.Factor1 * itemInfo.Factor2 * itemInfo.Factor3 * itemInfo.Factor4;

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

    public async Task<PickListCheckSummaryResponse> GetCheckSummary(int pickListId, string warehouse) {
        if (!settings.Options.EnablePickingCheck) {
            throw new Exception("Picking check is not enabled");
        }

        var session = await dbContext.PickListCheckSessions
        .Include(s => s.CheckedItems)
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
                        Factor1 = item.Factor1,
                        Factor2 = item.Factor2,
                        Factor3 = item.Factor3,
                        Factor4 = item.Factor4,
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
