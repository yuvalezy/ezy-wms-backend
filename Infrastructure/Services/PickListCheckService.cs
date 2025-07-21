using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListCheckService : IPickListCheckService {
    private readonly IMemoryCache _cache;
    private readonly IPickListService _pickListService;
    private readonly ILogger<PickListCheckService> _logger;
    private const string CACHE_KEY_PREFIX = "PickListCheck_";

    public PickListCheckService(
        IMemoryCache cache,
        IPickListService pickListService,
        ILogger<PickListCheckService> logger) {
        _cache = cache;
        _pickListService = pickListService;
        _logger = logger;
    }

    public async Task<PickListCheckSession?> StartCheck(int pickListId, SessionInfo sessionInfo) {
        var cacheKey = $"{CACHE_KEY_PREFIX}{pickListId}";
        
        // Check if session already exists
        if (_cache.TryGetValue<PickListCheckSession>(cacheKey, out var existingSession)) {
            if (!existingSession.IsCompleted) {
                return existingSession;
            }
        }

        // Verify pick list exists
        var pickList = await _pickListService.GetPickList(pickListId, new PickListDetailRequest(), sessionInfo.Warehouse);
        if (pickList == null) {
            return null;
        }

        var session = new PickListCheckSession {
            PickListId = pickListId,
            StartedByUserId = sessionInfo.Guid,
            StartedByUserName = sessionInfo.Name,
            StartedAt = DateTime.UtcNow,
            CheckedItems = new Dictionary<string, PickListCheckItem>(),
            IsCompleted = false
        };

        _cache.Set(cacheKey, session, TimeSpan.FromHours(4));
        return session;
    }

    public async Task<PickListCheckItemResponse> CheckItem(PickListCheckItemRequest request, SessionInfo sessionInfo) {
        var cacheKey = $"{CACHE_KEY_PREFIX}{request.PickListId}";
        
        if (!_cache.TryGetValue<PickListCheckSession>(cacheKey, out var session)) {
            return new PickListCheckItemResponse {
                Success = false,
                ErrorMessage = "No active check session found",
                Status = ResponseStatus.Error
            };
        }

        if (session.IsCompleted) {
            return new PickListCheckItemResponse {
                Success = false,
                ErrorMessage = "Check session is already completed",
                Status = ResponseStatus.Error
            };
        }

        // Update or add checked item
        session.CheckedItems[request.ItemCode] = new PickListCheckItem {
            ItemCode = request.ItemCode,
            CheckedQuantity = request.CheckedQuantity,
            Unit = request.Unit,
            BinEntry = request.BinEntry,
            CheckedAt = DateTime.UtcNow,
            CheckedByUserId = sessionInfo.Guid
        };

        _cache.Set(cacheKey, session, TimeSpan.FromHours(4));

        // Get pick list details to calculate progress
        var pickList = await _pickListService.GetPickList(
            request.PickListId, 
            new PickListDetailRequest { AvailableBins = false }, 
            sessionInfo.Warehouse
        );

        var totalItems = pickList?.Detail?.SelectMany(d => d.Items ?? []).Count() ?? 0;

        return new PickListCheckItemResponse {
            Success = true,
            ItemsChecked = session.CheckedItems.Count,
            TotalItems = totalItems,
            Status = ResponseStatus.Ok
        };
    }

    public async Task<PickListCheckSummaryResponse> GetCheckSummary(int pickListId) {
        var cacheKey = $"{CACHE_KEY_PREFIX}{pickListId}";
        
        if (!_cache.TryGetValue<PickListCheckSession>(cacheKey, out var session)) {
            return new PickListCheckSummaryResponse {
                PickListId = pickListId,
                Items = new List<PickListCheckItemDetail>()
            };
        }

        // Get pick list details
        var pickList = await _pickListService.GetPickList(
            pickListId, 
            new PickListDetailRequest { AvailableBins = true }, 
            string.Empty
        );

        var summary = new PickListCheckSummaryResponse {
            PickListId = pickListId,
            CheckStartedAt = session.StartedAt,
            CheckStartedBy = session.StartedByUserName,
            Items = new List<PickListCheckItemDetail>()
        };

        if (pickList?.Detail != null) {
            foreach (var detail in pickList.Detail) {
                foreach (var item in detail.Items ?? []) {
                    var checkedItem = session.CheckedItems.GetValueOrDefault(item.ItemCode);
                    var checkedQty = checkedItem?.CheckedQuantity ?? 0;
                    var difference = checkedQty - item.Picked;

                    summary.Items.Add(new PickListCheckItemDetail {
                        ItemCode = item.ItemCode,
                        ItemName = item.ItemName,
                        PickedQuantity = item.Picked,
                        CheckedQuantity = checkedQty,
                        Difference = difference,
                        Unit = checkedItem?.Unit ?? UnitType.Unit,
                        BinLocation = item.BinQuantities?.FirstOrDefault()?.Code
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
        var cacheKey = $"{CACHE_KEY_PREFIX}{pickListId}";
        
        if (!_cache.TryGetValue<PickListCheckSession>(cacheKey, out var session)) {
            return false;
        }

        session.IsCompleted = true;
        session.CompletedAt = DateTime.UtcNow;
        
        _cache.Set(cacheKey, session, TimeSpan.FromHours(24)); // Keep completed checks for 24 hours
        
        _logger.LogInformation(
            "Pick list check completed. PickListId: {PickListId}, CompletedBy: {UserId}, ItemsChecked: {ItemCount}",
            pickListId, userId, session.CheckedItems.Count
        );

        return true;
    }

    public async Task<PickListCheckSession?> GetActiveCheckSession(int pickListId) {
        var cacheKey = $"{CACHE_KEY_PREFIX}{pickListId}";
        _cache.TryGetValue<PickListCheckSession>(cacheKey, out var session);
        return await Task.FromResult(session);
    }
}