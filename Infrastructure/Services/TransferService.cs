using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class TransferService(SystemDbContext db, IExternalSystemAdapter adapter) : ITransferService {
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

    public async Task<bool> CancelTransfer(Guid id, SessionInfo sessionInfo) {
        var transfer = await db.Transfers.FindAsync(id);
        if (transfer == null) {
            throw new KeyNotFoundException($"Transfer with ID {id} not found.");
        }

        if (transfer.Status != ObjectStatus.Open && transfer.Status != ObjectStatus.InProgress) {
            throw new InvalidOperationException("Cannot cancel transfer if the Status is not Open or In Progress");
        }

        // Update transfer status
        transfer.Status = ObjectStatus.Cancelled;
        transfer.UpdatedAt = DateTime.UtcNow;
        transfer.UpdatedByUserId = sessionInfo.Guid;

        // Update all open lines to cancelled
        var openLines = await db.TransferLines
            .Where(tl => tl.TransferId == id && tl.LineStatus != LineStatus.Closed)
            .ToListAsync();

        foreach (var line in openLines) {
            line.LineStatus = LineStatus.Closed;
            line.UpdatedAt = DateTime.UtcNow;
            line.UpdatedByUserId = sessionInfo.Guid;
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ProcessTransferResponse> ProcessTransfer(Guid id, SessionInfo sessionInfo) {
        var transfer = await db.Transfers
            .Include(t => t.Lines.Where(l => l.LineStatus != LineStatus.Closed))
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transfer == null) {
            throw new KeyNotFoundException($"Transfer with ID {id} not found.");
        }

        if (transfer.Status != ObjectStatus.InProgress) {
            throw new InvalidOperationException("Cannot process transfer if the Status is not In Progress");
        }

        // Update transfer status to Processing
        transfer.Status = ObjectStatus.Processing;
        transfer.UpdatedAt = DateTime.UtcNow;
        transfer.UpdatedByUserId = sessionInfo.Guid;
        await db.SaveChangesAsync();

        try {
            // Prepare data for SAP B1 transfer creation
            var transferData = await PrepareTransferData(id);
            
            // Call external system to create the transfer in SAP B1
            var result = await adapter.ProcessTransfer(id, transfer.WhsCode, transfer.Comments, transferData);

            if (result.Success) {
                // Update transfer status to Finished
                transfer.Status = ObjectStatus.Finished;
                transfer.UpdatedAt = DateTime.UtcNow;
                transfer.UpdatedByUserId = sessionInfo.Guid;

                // Update all open lines to Finished
                var openLines = await db.TransferLines
                    .Where(tl => tl.TransferId == id && tl.LineStatus != LineStatus.Closed)
                    .ToListAsync();

                foreach (var line in openLines) {
                    line.LineStatus = LineStatus.Finished;
                    line.UpdatedAt = DateTime.UtcNow;
                    line.UpdatedByUserId = sessionInfo.Guid;
                }

                await db.SaveChangesAsync();
            } else {
                // Rollback to InProgress if SAP B1 creation failed
                transfer.Status = ObjectStatus.InProgress;
                transfer.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return result;
        }
        catch (Exception ex) {
            // Rollback to InProgress on any error
            transfer.Status = ObjectStatus.InProgress;
            transfer.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            
            return new ProcessTransferResponse {
                Success = false,
                ErrorMessage = $"Error processing transfer: {ex.Message}",
                Status = ResponseStatus.Error
            };
        }
    }

    private async Task<Dictionary<string, TransferCreationData>> PrepareTransferData(Guid transferId) {
        var lines = await db.TransferLines
            .Where(tl => tl.TransferId == transferId && tl.LineStatus != LineStatus.Closed)
            .GroupBy(tl => tl.ItemCode)
            .Select(g => new {
                ItemCode = g.Key,
                Lines = g.ToList()
            })
            .ToListAsync();

        var transferData = new Dictionary<string, TransferCreationData>();

        foreach (var itemGroup in lines) {
            var data = new TransferCreationData {
                ItemCode = itemGroup.ItemCode,
                Quantity = itemGroup.Lines.Sum(l => l.Quantity)
            };

            // Group source bins
            var sourceBins = itemGroup.Lines
                .Where(l => l.Type == SourceTarget.Source && l.BinEntry.HasValue)
                .GroupBy(l => l.BinEntry.Value)
                .Select(g => new TransferCreationBin {
                    BinEntry = g.Key,
                    Quantity = g.Sum(l => l.Quantity)
                })
                .ToList();

            // Group target bins
            var targetBins = itemGroup.Lines
                .Where(l => l.Type == SourceTarget.Target && l.BinEntry.HasValue)
                .GroupBy(l => l.BinEntry.Value)
                .Select(g => new TransferCreationBin {
                    BinEntry = g.Key,
                    Quantity = g.Sum(l => l.Quantity)
                })
                .ToList();

            data.SourceBins = sourceBins;
            data.TargetBins = targetBins;

            transferData[itemGroup.ItemCode] = data;
        }

        return transferData;
    }
}