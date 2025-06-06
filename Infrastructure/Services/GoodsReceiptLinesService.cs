using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GoodsReceiptLinesService(SystemDbContext db, IExternalSystemAdapter adapter) : IGoodsReceiptLinesService {
    public async Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo session, GoodsReceiptAddItemRequest request) {
        var goodsReceipt = await db.GoodsReceipts
            .Include(gr => gr.Documents)
            .FirstOrDefaultAsync(gr => gr.Id == request.Id && (gr.Status == ObjectStatus.Open || gr.Status == ObjectStatus.InProgress));

        if (goodsReceipt == null) {
            return new GoodsReceiptAddItemResponse {
                Status         = ResponseStatus.Error,
                ErrorMessage   = "Goods receipt not found or already closed",
                ClosedDocument = true
            };
        }

        // Validate with external system
        var specificDocuments = goodsReceipt.Documents.Select(d => new ObjectKey(d.ObjType, d.DocEntry, d.DocNumber)).ToList();
        var validationResult  = await adapter.ValidateGoodsReceiptAddItem(request, specificDocuments, session.Guid, session.Warehouse);
        if (!validationResult.IsValid) {
            return new GoodsReceiptAddItemResponse {
                Status       = ResponseStatus.Error,
                ErrorMessage = validationResult.ErrorMessage,
            };
        }

        // Get item details
        var items = await adapter.ItemCheckAsync(request.ItemCode, request.BarCode);
        var item  = items.FirstOrDefault();
        if (item == null) {
            return new GoodsReceiptAddItemResponse {
                Status       = ResponseStatus.Error,
                ErrorMessage = "Item not found"
            };
        }
        
        var line = new GoodsReceiptLine {
            GoodsReceiptId  = goodsReceipt.Id,
            ItemCode        = request.ItemCode,
            BarCode         = request.BarCode,
            Quantity        = 1, // Default quantity
            Unit            = request.Unit,
            Date            = DateTime.UtcNow,
            LineStatus      = LineStatus.Open,
            CreatedAt       = DateTime.UtcNow,
            CreatedByUserId = session.Guid
        };

        if (goodsReceipt.Status != ObjectStatus.InProgress) {
            goodsReceipt.Status                                                       = ObjectStatus.InProgress;
            db.GoodsReceipts.Entry(goodsReceipt).Property(gr => gr.Status).IsModified = true;
        }
        
        await db.GoodsReceiptLines.AddAsync(line);
        await db.SaveChangesAsync();

        return new() {
            Status = ResponseStatus.Ok,
            LineId = line.Id,
        };
    }


    public async Task<UpdateLineResponse> UpdateLine(SessionInfo session, UpdateGoodsReceiptLineRequest request) {
        var line = await db.GoodsReceiptLines
            .Include(l => l.GoodsReceipt)
            .FirstOrDefaultAsync(l => l.Id == request.LineID);

        if (line == null) {
            throw new KeyNotFoundException($"Line with ID {request.LineID} not found");
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
            .FirstOrDefaultAsync(l => l.GoodsReceipt.Id == request.Id && l.Id == request.LineID);

        if (line == null) {
            throw new KeyNotFoundException($"Line with ID {request.LineID} not found");
        }

        if (line.LineStatus == LineStatus.Closed) {
            return new UpdateLineResponse {
                ReturnValue  = UpdateLineReturnValue.LineStatus,
                ErrorMessage = "Cannot update closed line"
            };
        }

        line.Quantity        = request.Quantity;
        line.UpdatedAt       = DateTime.UtcNow;
        line.UpdatedByUserId = session.Guid;

        await db.SaveChangesAsync();

        return new UpdateLineResponse { ReturnValue = UpdateLineReturnValue.Ok };
    }
}