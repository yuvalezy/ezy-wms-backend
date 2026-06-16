using Core.DTOs.InventoryCounting;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class InventoryCountingsService(
    SystemDbContext db) : IInventoryCountingsService {

    public async Task<InventoryCountingResponse> CreateCounting(CreateInventoryCountingRequest request, SessionInfo sessionInfo) {
        var counting = new InventoryCounting {
            Name = request.Name,
            Date = DateTime.UtcNow.Date,
            Status = ObjectStatus.Open,
            WhsCode = sessionInfo.Warehouse,
            CreatedByUserId = sessionInfo.Guid,
            Lines = new List<InventoryCountingLine>()
        };

        await db.InventoryCountings.AddAsync(counting);
        await db.SaveChangesAsync();

        return InventoryCountingResponse.FromEntity(counting);
    }

    public async Task<IEnumerable<InventoryCountingResponse>> GetCountings(InventoryCountingsRequest request, string warehouse) {
        var query = db.InventoryCountings
        .Include(ic => ic.CreatedByUser)
        .Include(ic => ic.UpdatedByUser)
        .Where(ic => ic.WhsCode == warehouse)
        .AsQueryable();

        if (request.ID.HasValue) {
            query = query.Where(ic => ic.Number == request.ID.Value);
        }

        if (request.Date.HasValue) {
            var targetDate = request.Date.Value.Date;
            query = query.Where(ic => ic.Date.Date == targetDate);
        }

        if (request.Statuses?.Length > 0) {
            query = query.Where(ic => request.Statuses.Contains(ic.Status));
        }

        var countings = await query.OrderByDescending(ic => ic.Number).ToListAsync();

        return countings.Select(InventoryCountingResponse.FromEntity);
    }

    public async Task<InventoryCountingResponse> GetCounting(Guid id) {
        var counting = await db.InventoryCountings
        .Include(ic => ic.Lines)
        .Include(ic => ic.CreatedByUser)
        .Include(ic => ic.UpdatedByUser)
        .FirstOrDefaultAsync(ic => ic.Id == id);

        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {id} not found.");
        }

        return InventoryCountingResponse.FromEntity(counting);
    }

    public async Task<bool> CancelCounting(Guid id, SessionInfo sessionInfo) {
        var counting = await db.InventoryCountings.FindAsync(id);
        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {id} not found.");
        }

        if (counting.Status != ObjectStatus.Open && counting.Status != ObjectStatus.InProgress) {
            throw new InvalidOperationException("Cannot cancel counting if the Status is not Open or In Progress");
        }

        counting.Status = ObjectStatus.Cancelled;
        counting.UpdatedAt = DateTime.UtcNow;
        counting.UpdatedByUserId = sessionInfo.Guid;

        db.Update(counting);
        await db.SaveChangesAsync();

        return true;
    }
}
