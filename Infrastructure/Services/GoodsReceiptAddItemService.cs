using Core.DTOs.GoodsReceipt;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GoodsReceiptAddItemService(SystemDbContext db, IExternalSystemAdapter adapter) : IGoodsReceiptAddItemService {
    public async Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo session, GoodsReceiptAddItemRequest request) {
        var    userId    = session.Guid;
        string warehouse = session.Warehouse;
        var goodsReceipt = await db.GoodsReceipts
            .Include(gr => gr.Documents)
            .Include(gr => gr.Lines)
            .FirstOrDefaultAsync(gr => gr.Id == request.Id && (gr.Status == ObjectStatus.Open || gr.Status == ObjectStatus.InProgress));

        if (goodsReceipt == null) {
            return new GoodsReceiptAddItemResponse {
                ErrorMessage   = "Goods receipt not found or already closed",
                ClosedDocument = true
            };
        }

        // Validate with external system
        var specificDocuments = goodsReceipt.Documents.Select(d => new ObjectKey(d.ObjType, d.DocEntry, d.DocNumber)).ToList();
        var validationResult  = await adapter.ValidateGoodsReceiptAddItem(request, specificDocuments, userId, warehouse);
        if (!validationResult.IsValid) {
            return new GoodsReceiptAddItemResponse {
                ErrorMessage = validationResult.ErrorMessage,
            };
        }

        // Get item details
        var items = await adapter.ItemCheckAsync(request.ItemCode, request.BarCode);
        var item  = items.FirstOrDefault();
        if (item == null) {
            return new GoodsReceiptAddItemResponse {
                ErrorMessage = "Item not found"
            };
        }

        var linesIds = goodsReceipt.Lines.Select(l => l.Id).ToList();
        // Get the source documents data
        var sourceDocuments = (await adapter.AddItemSourceDocuments(request, warehouse, goodsReceipt.Type, goodsReceipt.CardCode, specificDocuments)).ToList();

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
                    sourceDocuments.Add(new GoodsReceiptAddItemSourceDocumentResponse {
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
            CreatedByUserId = userId,
            Sources = sourceDocuments.Select(s => new GoodsReceiptSource {
                CreatedByUserId    = userId,
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
                CreatedByUserId    = userId,
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
        var          documentsWaiting = await adapter.AddItemTargetDocuments(warehouse, request.ItemCode);
        LineStatus[] targetStatuses   = [LineStatus.Open, LineStatus.Finished, LineStatus.Processing];
        var targetData = await db.GoodsReceiptTargets
            .Where(v => v.ItemCode == request.ItemCode && v.WhsCode == warehouse && targetStatuses.Contains(v.TargetStatus))
            .GroupBy(v => new { v.TargetType, v.TargetEntry, v.TargetLine })
            .Select(v => new { v.Key.TargetType, v.Key.TargetEntry, v.Key.TargetLine, Quantity = v.Sum(q => q.TargetQuantity) })
            .ToListAsync();

        // Join documentsWaiting with targetData to calculate remaining quantities needed
        var documentsNeedingItems = documentsWaiting
            .GroupJoin(targetData,
                waiting => new { waiting.Type, waiting.Entry, waiting.LineNum },
                target => new { Type = target.TargetType, Entry = target.TargetEntry, LineNum = target.TargetLine },
                (waiting, targets) => new {
                    waiting.Priority,
                    waiting.Type,
                    waiting.Entry,
                    waiting.LineNum,
                    waiting.Date,
                    RequiredQuantity  = waiting.Quantity,
                    AllocatedQuantity = targets.Sum(t => (int)t.Quantity),
                    RemainingQuantity = waiting.Quantity - targets.Sum(t => (int)t.Quantity)
                })
            .Where(doc => doc.RemainingQuantity > 0)
            .OrderBy(doc => doc.Priority)
            .ThenBy(doc => doc.Date)
            .ToList();

        // STEP 8: Distribute received quantity to target documents by priority
        // Allocate our received quantity to waiting documents in priority order

        (int fulfillment, int showroom) = (0, 0);
        foreach (var needingItem in documentsNeedingItems) {
            int scanQuantity = needingItem.RemainingQuantity;
            // Determine how much to allocate (either full demand or remaining quantity)
            int insertQuantity = quantity > scanQuantity ? scanQuantity : quantity;
            // Reduce remaining quantity to allocate
            quantity -= insertQuantity;

            // Create target allocation record
            await db.GoodsReceiptTargets.AddAsync(new GoodsReceiptTarget {
                CreatedByUserId    = userId,
                ItemCode           = request.ItemCode,
                WhsCode            = warehouse,
                TargetType         = needingItem.Type,
                TargetEntry        = needingItem.Entry,
                TargetLine         = needingItem.LineNum,
                TargetQuantity     = insertQuantity,
                TargetStatus       = LineStatus.Open,
                GoodsReceiptLineId = line.Id,
            });

            switch (needingItem.Type) {
                case 1250000001:
                    showroom += insertQuantity;
                    break;
                case 13 or 17:
                    fulfillment += insertQuantity;
                    break;
            }

            // Stop if we've allocated all available quantity
            if (quantity == 0) {
                break;
            }
        }


        await db.SaveChangesAsync();

        quantity = 1 * (request.Unit != UnitType.Unit ? item.NumInBuy : 1) * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
        return new() {
            LineId      = line.Id,
            Fulfillment = fulfillment > 0,
            Showroom    = showroom > 0,
            Warehouse   = quantity - fulfillment - showroom > 0,
            Quantity    = 1,
            NumInBuy    = item.NumInBuy,
            BuyUnitMsr  = item.BuyUnitMsr,
            PurPackUn   = item.PurPackUn,
            PurPackMsr  = item.PurPackMsr
        };
    }
}