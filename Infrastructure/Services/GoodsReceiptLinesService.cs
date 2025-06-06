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
            .Include(gr => gr.Lines)
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

        var linesIds = goodsReceipt.Lines.Select(l => l.Id).ToList();
        // Get the source documents data
        var sourceDocuments = (await adapter.AddItemSourceDocuments(request, session.Warehouse, goodsReceipt.Type, goodsReceipt.CardCode, specificDocuments)).ToList();

        // Get the source quantity already allocated and subtract it from the source documents
        var goodsReceiptSources = await db
            .GoodsReceiptSources
            .Where(g => linesIds.Contains(g.GoodsReceiptLineId))
            .ToListAsync();

        foreach (var sourceDocument in sourceDocuments) {
            int selectedQuantity = (int)goodsReceiptSources
                .Where(g => g.SourceType == sourceDocument.Type &&
                            g.SourceEntry == sourceDocument.Entry &&
                            g.SourceLine == sourceDocument.LineNum)
                .Sum(g => g.Quantity);
            sourceDocument.Quantity -= selectedQuantity;
        }

        sourceDocuments.RemoveAll(s => s.Quantity <= 0);

        // Iterate through available source documents and allocate quantities using FIFO
        int quantity = 1 * (request.Unit != UnitType.Unit ? item.NumInBuy : 1) * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
        for (int i = 0; i < sourceDocuments.Count; i++) {
            var sourceDocument = sourceDocuments[i];
            int iQty           = sourceDocument.Quantity;

            // If this source can fully satisfy remaining quantity
            if (iQty <= quantity) {
                quantity -= iQty; // Reduce remaining quantity
                if (quantity == 0) {
                    sourceDocuments.RemoveRange(i + 1, sourceDocuments.Count - (i + 1)); // Remove unused sources
                    break;
                }
            }
            else {
                // Partial allocation - this source has more than we need
                sourceDocument.Quantity = quantity;
                sourceDocuments.RemoveRange(i + 1, sourceDocuments.Count - (i + 1)); // Remove unused sources
                quantity = 0;
                break;
            }
        }


        // Handle case where we still have unallocated quantity (over-receipt scenario)
        if (quantity > 0) {
            // Add remaining quantity to the last allocated source document
            if (sourceDocuments.Count > 0) {
                sourceDocuments.Last().Quantity += quantity;
            }
            // No sources found, try to find a fallback source from existing receipt lines
            else {
                var fallback = db
                    .GoodsReceiptSources
                    .OrderBy(v => v.SourceType == 20 ? 'A' : v.SourceType == 22 ? 'B' : 'C')
                    .ThenByDescending(v => v.CreatedAt)
                    .FirstOrDefault();
                if (fallback != null) {
                    sourceDocuments.Add(new GoodsReceiptAddItemSourceDocument {
                        Type     = fallback.SourceType,
                        Entry    = fallback.SourceEntry,
                        LineNum  = fallback.SourceLine,
                        Quantity = quantity
                    });
                }
            }

            quantity = 0;
        }

        // Validate that we found at least one source document
        if (sourceDocuments.Count == 0) {
            return new GoodsReceiptAddItemResponse {
                Status       = ResponseStatus.Error,
                ErrorMessage = $"No source documents found for item {request.ItemCode}"
            };
        }


        // STEP 5: Create the goods receipt line entry
        // Reset quantity for actual insertion
        quantity = 1 * (request.Unit != UnitType.Unit ? item.NumInBuy : 1) * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);

        // Insert the main goods receipt line
        var line = new GoodsReceiptLine {
            GoodsReceiptId  = goodsReceipt.Id,
            ItemCode        = request.ItemCode,
            BarCode         = request.BarCode,
            Quantity        = quantity,
            Unit            = request.Unit,
            Date            = DateTime.UtcNow,
            LineStatus      = LineStatus.Open,
            CreatedByUserId = session.Guid,
            Sources = sourceDocuments.Select(s => new GoodsReceiptSource {
                CreatedByUserId    = session.Guid,
                Quantity           = s.Quantity,
                SourceEntry        = s.Entry,
                SourceLine         = s.LineNum,
                SourceType         = s.Type,
                GoodsReceiptLineId = goodsReceipt.Id,
            }).ToArray()
        };
        await db.GoodsReceiptLines.AddAsync(line);

        // Insert source document allocations for this line
        foreach (var s in sourceDocuments) {
            var source = new GoodsReceiptSource {
                CreatedByUserId    = session.Guid,
                Quantity           = s.Quantity,
                SourceEntry        = s.Entry,
                SourceLine         = s.LineNum,
                SourceType         = s.Type,
                GoodsReceiptLineId = line.Id,
            };
            await db.GoodsReceiptSources.AddAsync(source);
        }

        // Update goods receipt header status to InProgress
        if (goodsReceipt.Status != ObjectStatus.InProgress) {
            goodsReceipt.Status = ObjectStatus.InProgress;
            db.GoodsReceipts
                .Entry(goodsReceipt)
                .Property(gr => gr.Status)
                .IsModified = true;
        }

        // STEP 7: Load target documents that need this item (ordered by priority)
        // Find documents waiting for this item and calculate remaining quantities needed
        var documentsWaiting = adapter.AddItemTargetDocuments(session.Warehouse, request.ItemCode);
        LineStatus[] targetStatuses   = [LineStatus.Open, LineStatus.Finished, LineStatus.Processing];
        var targetData = db.GoodsReceiptTargets
            .Where(v => v.ItemCode == request.ItemCode && v.WhsCode == session.Warehouse && targetStatuses.Contains(v.TargetStatus))
            .GroupBy(v => new {v.TargetType, v.TargetEntry, v.TargetLine})
            .Select(v => new {v.Key.TargetType, v.Key.TargetEntry, v.Key.TargetLine, Quantity = v.Sum(q => q.TargetQuantity)});
        
        // todo: documentsWaintng, join with targetData, reduce waiting.quantity - target quantity, select documentWaiting where waitnig.quantity - target.quantity > 0
        
        
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