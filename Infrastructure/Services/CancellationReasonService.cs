using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CancellationReasonService : ICancellationReasonService {
    private readonly SystemDbContext _db;

    public CancellationReasonService(SystemDbContext db) {
        _db = db;
    }

    public async Task<CancellationReasonResponse> CreateAsync(CreateCancellationReasonRequest request) {
        var reason = new CancellationReason {
            Name = request.Name,
            Transfer = request.Transfer,
            GoodsReceipt = request.GoodsReceipt,
            Counting = request.Counting,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.CancellationReasons.Add(reason);
        await _db.SaveChangesAsync();

        return MapToResponse(reason);
    }

    public async Task<CancellationReasonResponse> UpdateAsync(UpdateCancellationReasonRequest request) {
        var reason = await _db.CancellationReasons.FindAsync(request.Id);
        if (reason == null) {
            throw new KeyNotFoundException($"Cancellation reason with ID {request.Id} not found.");
        }

        reason.Name = request.Name;
        reason.Transfer = request.Transfer;
        reason.GoodsReceipt = request.GoodsReceipt;
        reason.Counting = request.Counting;
        reason.IsEnabled = request.IsEnabled;
        reason.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await MapToResponseWithCanDelete(reason);
    }

    public async Task<bool> DeleteAsync(Guid id) {
        var reason = await _db.CancellationReasons.FindAsync(id);
        if (reason == null) {
            return false;
        }

        // Check if the reason is in use
        var isInUse = await IsReasonInUse(id);
        if (isInUse) {
            throw new InvalidOperationException("Cannot delete cancellation reason that is in use. Consider disabling it instead.");
        }

        _db.CancellationReasons.Remove(reason);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<CancellationReasonResponse>> GetAllAsync(ObjectType? objectType = null, bool includeDisabled = false) {
        var query = _db.CancellationReasons.AsQueryable();

        // Filter by enabled status
        if (!includeDisabled) {
            query = query.Where(r => r.IsEnabled);
        }

        // Filter by object type
        if (objectType.HasValue) {
            query = objectType.Value switch {
                ObjectType.Transfer => query.Where(r => r.Transfer),
                ObjectType.GoodsReceipt => query.Where(r => r.GoodsReceipt),
                ObjectType.InventoryCounting => query.Where(r => r.Counting),
                _ => query
            };
        }

        var reasons = await query.OrderBy(r => r.Name).ToListAsync();

        // Map to response with CanDelete calculated for each
        var responses = new List<CancellationReasonResponse>();
        foreach (var reason in reasons) {
            responses.Add(await MapToResponseWithCanDelete(reason));
        }

        return responses;
    }

    public async Task<CancellationReasonResponse?> GetByIdAsync(Guid id) {
        var reason = await _db.CancellationReasons.FindAsync(id);
        return reason == null ? null : await MapToResponseWithCanDelete(reason);
    }

    private async Task<bool> IsReasonInUse(Guid reasonId) {
        // Check if used in TransferLines
        var usedInTransfers = await _db.TransferLines
            .AnyAsync(tl => tl.CancellationReasonId == reasonId);
        if (usedInTransfers) return true;

        // Check if used in GoodsReceiptLines
        var usedInGoodsReceipts = await _db.GoodsReceiptLines
            .AnyAsync(grl => grl.CancellationReasonId == reasonId);
        if (usedInGoodsReceipts) return true;

        // Check if used in InventoryCountingLines
        var usedInCountings = await _db.InventoryCountingLines
            .AnyAsync(icl => icl.CancellationReasonId == reasonId);
        if (usedInCountings) return true;

        return false;
    }

    private CancellationReasonResponse MapToResponse(CancellationReason reason) {
        return new CancellationReasonResponse {
            Id = reason.Id,
            Name = reason.Name,
            Transfer = reason.Transfer,
            GoodsReceipt = reason.GoodsReceipt,
            Counting = reason.Counting,
            IsEnabled = reason.IsEnabled,
            CanDelete = true // Default to true for new records
        };
    }

    private async Task<CancellationReasonResponse> MapToResponseWithCanDelete(CancellationReason reason) {
        var response = MapToResponse(reason);
        response.CanDelete = !await IsReasonInUse(reason.Id);
        return response;
    }
}