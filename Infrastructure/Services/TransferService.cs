using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class TransferService(SystemDbContext db) : ITransferService {
    public async Task<TransferResponse> CreateTransfer(CreateTransferRequest request, SessionInfo sessionInfo) {
        var now = DateTime.UtcNow.Date;
        var transfer = new Transfer {
            Name            = request.Name,
            CreatedByUserId = sessionInfo.Guid,
            Comments        = request.Comments,
            Date            = now,
            Status          = ObjectStatus.Open,
            WhsCode         = sessionInfo.Warehouse,
            Lines           = []
        };
        await db.Transfers.AddAsync(transfer);
        await db.SaveChangesAsync();
        return TransferResponse.FromTransfer(transfer);
    }

    public async Task<TransferResponse> GetTransfer(Guid id, bool progress = false) {
        var query = db.Transfers.AsQueryable();

        if (progress) {
            query = query.Include(t => t.Lines.Where(l => l.LineStatus != LineStatus.Closed));
        }

        query = query.Include(t => t.CreatedByUser);

        var transfer = await query.FirstOrDefaultAsync(t => t.Id == id);
        if (transfer == null) {
            throw new KeyNotFoundException($"Transfer with ID {id} not found.");
        }

        return GetTransferResponse(progress, transfer);
    }

    public async Task<IEnumerable<TransferResponse>> GetTransfers(TransfersRequest request, string warehouse) {
        var query = db.Transfers
            .Include(t => t.CreatedByUser)
            .Where(t => t.WhsCode == warehouse)
            .AsQueryable();

        // Apply filters
        if (request.Date.HasValue) {
            query = query.Where(t => t.Date == request.Date.Value.Date);
        }

        if (request.Status?.Length > 0) {
            query = query.Where(t => request.Status.Contains(t.Status));
        }

        if (request.ID.HasValue) {
            // Assuming ID is some sort of display ID, not the GUID
            // You may need to adjust this based on your business logic
            query = query.Where(t => t.Id == new Guid(request.ID.Value.ToString()));
        }

        if (request.Number is > 0) {
            query = query.Where(t => t.Number == request.Number);
        }

        // Include lines for progress calculation if requested
        if (request.Progress) {
            query = query.Include(t => t.Lines.Where(l => l.LineStatus != LineStatus.Closed));
        }

        // Apply ordering
        switch (request.OrderBy) {
            case TransferOrderBy.Date:
                query = request.Desc
                    ? query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
                    : query.OrderBy(t => t.Date).ThenBy(t => t.Id);
                break;
            case TransferOrderBy.ID:
            default:
                query = request.Desc
                    ? query.OrderByDescending(t => t.Id)
                    : query.OrderBy(t => t.Id);
                break;
        }


        var transfers = await query.ToListAsync();

        return transfers.Select(transfer => GetTransferResponse(request.Progress, transfer)).ToList();
    }

    private static TransferResponse GetTransferResponse(bool progress, Transfer transfer) {
        var response = TransferResponse.FromTransfer(transfer);

        if (progress && transfer.Lines.Any()) {
            int sourceQuantity = transfer.Lines
                .Where(l => l.Type == SourceTarget.Source && l.LineStatus != LineStatus.Closed)
                .Sum(l => l.Quantity);

            int targetQuantity = transfer.Lines
                .Where(l => l.Type == SourceTarget.Target && l.LineStatus != LineStatus.Closed)
                .Sum(l => l.Quantity);

            response.Progress = sourceQuantity > 0 ? (targetQuantity * 100) / sourceQuantity : 0;
        }

        return response;
    }

    public async Task<TransferResponse> GetProcessInfo(Guid id) {
        var transfer = await GetTransfer(id, true);

        bool hasIncompleteItems = await db.TransferLines
            .Where(l => l.TransferId == id && l.LineStatus != LineStatus.Closed)
            .GroupBy(l => l.ItemCode)
            .AnyAsync(g => g.Where(l => l.Type == SourceTarget.Source).Sum(l => l.Quantity) !=
                           g.Where(l => l.Type == SourceTarget.Target).Sum(l => l.Quantity));

        bool hasItems = await db.TransferLines
            .AnyAsync(l => l.TransferId == id && l.LineStatus != LineStatus.Closed);

        var response = TransferResponse.FromTransfer(transfer);
        response.IsComplete = !hasIncompleteItems && hasItems;

        return TransferResponse.FromTransfer(transfer);
    }
}