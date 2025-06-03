using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;

namespace Infrastructure.Services;

public class TransferService(SystemDbContext db) : ITransferService {
    public async Task<TransferResponse> CreateTransfer(CreateTransferRequest request, SessionInfo sessionInfo) {
        var now = DateTime.UtcNow;
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
        return new TransferResponse {
            Id              = transfer.Id,
            CreatedAt       = transfer.CreatedAt,
            CreatedByUserId = transfer.CreatedByUserId,
            CreatedByUser   = transfer.CreatedByUser,
            UpdatedAt       = transfer.UpdatedAt,
            UpdatedByUserId = transfer.UpdatedByUserId,
            UpdatedByUser   = transfer.UpdatedByUser,
            Deleted         = transfer.Deleted,
            DeletedAt       = transfer.DeletedAt,
            Name            = transfer.Name,
            Comments        = transfer.Comments,
            Date            = transfer.Date,
            Status          = transfer.Status,
            WhsCode         = transfer.WhsCode,
            Lines           = transfer.Lines,
            Progress        = 0,
            IsComplete      = false
        };
    }
}