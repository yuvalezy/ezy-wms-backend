using Core.DTOs;
using Core.DTOs.GoodsReceipt;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GoodsReceiptLinesService(SystemDbContext db, IExternalSystemAdapter adapter) : IGoodsReceiptLinesService {
    public async Task<UpdateLineResponse> UpdateLine(SessionInfo session, UpdateGoodsReceiptLineRequest request) {
        var line = await db.GoodsReceiptLines
            .Include(l => l.GoodsReceipt)
            .FirstOrDefaultAsync(l => l.Id == request.LineId);

        if (line == null) {
            throw new KeyNotFoundException($"Line with ID {request.LineId} not found");
        }

        if (line.LineStatus == LineStatus.Closed) {
            return new UpdateLineResponse {
                ReturnValue  = UpdateLineReturnValue.LineStatus,
                ErrorMessage = "Cannot update closed line"
            };
        }

        if (request.Status.HasValue)
            line.LineStatus = request.Status.Value;

        if (request.StatusReason.HasValue)
            line.StatusReason = request.StatusReason.Value;

        if (request.CancellationReasonId.HasValue)
            line.CancellationReasonId = request.CancellationReasonId.Value;

        if (!string.IsNullOrEmpty(request.Comment))
            line.Comments = request.Comment;

        line.UpdatedAt       = DateTime.UtcNow;
        line.UpdatedByUserId = session.Guid;

        await db.SaveChangesAsync();

        return new UpdateLineResponse { ReturnValue = UpdateLineReturnValue.Ok };
    }

    public async Task<UpdateLineResponse> UpdateLineQuantity(SessionInfo session, UpdateGoodsReceiptLineQuantityRequest request) {
        var line = await db.GoodsReceiptLines
            .Include(l => l.GoodsReceipt)
            .FirstOrDefaultAsync(l => l.GoodsReceipt.Id == request.Id && l.Id == request.LineId);

        if (line == null) {
            throw new KeyNotFoundException($"Line with ID {request.LineId} not found");
        }

        if (line.LineStatus == LineStatus.Closed) {
            return new UpdateLineResponse {
                ReturnValue  = UpdateLineReturnValue.LineStatus,
                ErrorMessage = "Cannot update closed line"
            };
        }

        decimal quantity = request.Quantity;
        if (line.Unit != UnitType.Unit) {
            var itemCheck = (await adapter.ItemCheckAsync(line.ItemCode, null)).First();
            quantity *= itemCheck.NumInBuy;
            if (line.Unit == UnitType.Pack) 
                quantity *= itemCheck.PurPackUn;
        }

        line.Quantity        = quantity;
        line.UpdatedAt       = DateTime.UtcNow;
        line.UpdatedByUserId = session.Guid;

        await db.SaveChangesAsync();

        return new UpdateLineResponse { ReturnValue = UpdateLineReturnValue.Ok };
    }
}