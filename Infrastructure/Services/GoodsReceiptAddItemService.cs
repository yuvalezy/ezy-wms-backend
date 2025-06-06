using Core.DTOs.GoodsReceipt;
using Core.DTOs.Items;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GoodsReceiptAddItemService(SystemDbContext db, IExternalSystemAdapter adapter, ISettings settings) : IGoodsReceiptAddItemService {
    private Options options = settings.Options;
    public async Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo session, GoodsReceiptAddItemRequest request) {
        var userId    = session.Guid;
        var warehouse = session.Warehouse;

        // Step 1: Validate goods receipt and item
        var validationResult = await ValidateGoodsReceiptAndItem(request, userId, warehouse);
        if (validationResult.ErrorResponse != null) {
            return validationResult.ErrorResponse;
        }

        var goodsReceipt      = validationResult.GoodsReceipt!;
        var item              = validationResult.Item!;
        var specificDocuments = validationResult.SpecificDocuments!;

        // Step 2: Process source documents allocation
        var sourceAllocationResult = await ProcessSourceDocumentsAllocation(request, warehouse, goodsReceipt, item, specificDocuments);
        if (sourceAllocationResult.ErrorResponse != null) {
            return sourceAllocationResult.ErrorResponse;
        }

        var sourceDocuments    = sourceAllocationResult.SourceDocuments!;
        var calculatedQuantity = sourceAllocationResult.CalculatedQuantity;

        // Step 3: Create goods receipt line
        var line = await CreateGoodsReceiptLine(request, goodsReceipt, sourceDocuments, calculatedQuantity, userId);

        // Step 4: Update goods receipt status
        await UpdateGoodsReceiptStatus(goodsReceipt);

        // Step 5: Process target document allocation
        var targetAllocationResult = await ProcessTargetDocumentAllocation(request, warehouse, line, calculatedQuantity, userId);

        await db.SaveChangesAsync();

        // Step 6: Build response
        return BuildAddItemResponse(line, item, targetAllocationResult.Fulfillment, targetAllocationResult.Showroom, calculatedQuantity);
    }

    private async Task<(GoodsReceiptAddItemResponse? ErrorResponse, GoodsReceipt? GoodsReceipt, ItemCheckResponse? Item, List<ObjectKey>? SpecificDocuments)>
        ValidateGoodsReceiptAndItem(GoodsReceiptAddItemRequest request, Guid userId, string warehouse) {
        var goodsReceipt = await db.GoodsReceipts
            .Include(gr => gr.Documents)
            .Include(gr => gr.Lines)
            .FirstOrDefaultAsync(gr => gr.Id == request.Id && (gr.Status == ObjectStatus.Open || gr.Status == ObjectStatus.InProgress));

        if (goodsReceipt == null) {
            return (new GoodsReceiptAddItemResponse {
                ErrorMessage   = "Goods receipt not found or already closed",
                ClosedDocument = true
            }, null, null, null);
        }

        var specificDocuments = goodsReceipt.Documents.Select(d => new ObjectKey(d.ObjType, d.DocEntry, d.DocNumber)).ToList();
        var validationResult  = await adapter.ValidateGoodsReceiptAddItem(request, specificDocuments, userId, warehouse);
        if (!validationResult.IsValid) {
            return (new GoodsReceiptAddItemResponse {
                ErrorMessage = validationResult.ErrorMessage,
            }, null, null, null);
        }

        var items = await adapter.ItemCheckAsync(request.ItemCode, request.BarCode);
        var item  = items.FirstOrDefault();
        if (item == null) {
            return (new GoodsReceiptAddItemResponse {
                ErrorMessage = "Item not found"
            }, null, null, null);
        }

        return (null, goodsReceipt, item, specificDocuments);
    }

    private async Task<(GoodsReceiptAddItemResponse? ErrorResponse, List<GoodsReceiptAddItemSourceDocumentResponse>? SourceDocuments, int CalculatedQuantity)>
        ProcessSourceDocumentsAllocation(GoodsReceiptAddItemRequest request, string          warehouse, GoodsReceipt goodsReceipt,
            ItemCheckResponse                                       item,    List<ObjectKey> specificDocuments) {
        var linesIds        = goodsReceipt.Lines.Select(l => l.Id).ToList();
        var sourceDocuments = (await adapter.AddItemSourceDocuments(request, warehouse, goodsReceipt.Type, goodsReceipt.CardCode, specificDocuments)).ToList();

        // Subtract already allocated quantities
        var goodsReceiptSources = await db.GoodsReceiptSources
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

        // Calculate required quantity
        int quantity = 1 * (request.Unit != UnitType.Unit ? item.NumInBuy : 1) * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);

        // Allocate quantities using FIFO
        await AllocateSourceDocuments(sourceDocuments, quantity);

        // Handle over-receipt scenario
        await HandleOverReceiptScenario(sourceDocuments, quantity);

        if (sourceDocuments.Count == 0) {
            return (new GoodsReceiptAddItemResponse {
                ErrorMessage = $"No source documents found for item {request.ItemCode}"
            }, null, 0);
        }

        return (null, sourceDocuments, quantity);
    }

    private async Task AllocateSourceDocuments(List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments, int quantity) {
        for (int i = 0; i < sourceDocuments.Count; i++) {
            var sourceDocument = sourceDocuments[i];
            int iQty           = sourceDocument.Quantity;

            if (iQty <= quantity) {
                quantity -= iQty;
                if (quantity == 0) {
                    sourceDocuments.RemoveRange(i + 1, sourceDocuments.Count - (i + 1));
                    break;
                }
            }
            else {
                sourceDocument.Quantity = quantity;
                sourceDocuments.RemoveRange(i + 1, sourceDocuments.Count - (i + 1));
                quantity = 0;
                break;
            }
        }
    }

    private async Task HandleOverReceiptScenario(List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments, int quantity) {
        if (quantity > 0) {
            if (sourceDocuments.Count > 0) {
                sourceDocuments.Last().Quantity += quantity;
            }
            else {
                var fallback = await db.GoodsReceiptSources
                    .OrderBy(v => v.SourceType == 20 ? 'A' : v.SourceType == 22 ? 'B' : 'C')
                    .ThenByDescending(v => v.CreatedAt)
                    .FirstOrDefaultAsync();

                if (fallback != null) {
                    sourceDocuments.Add(new GoodsReceiptAddItemSourceDocumentResponse {
                        Type     = fallback.SourceType,
                        Entry    = fallback.SourceEntry,
                        LineNum  = fallback.SourceLine,
                        Quantity = quantity
                    });
                }
            }
        }
    }

    private async Task<GoodsReceiptLine> CreateGoodsReceiptLine(
        GoodsReceiptAddItemRequest                      request,
        GoodsReceipt                                    goodsReceipt,
        List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments,
        int                                             quantity, Guid userId) {
        var line = new GoodsReceiptLine {
            GoodsReceiptId  = goodsReceipt.Id,
            ItemCode        = request.ItemCode,
            BarCode         = request.BarCode,
            Quantity        = quantity,
            Unit            = request.Unit,
            Date            = DateTime.UtcNow,
            LineStatus      = LineStatus.Open,
            CreatedByUserId = userId,
        };

        await db.GoodsReceiptLines.AddAsync(line);

        // Insert source document allocations
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

        return line;
    }

    private async Task UpdateGoodsReceiptStatus(GoodsReceipt goodsReceipt) {
        if (goodsReceipt.Status != ObjectStatus.InProgress) {
            goodsReceipt.Status = ObjectStatus.InProgress;
            db.GoodsReceipts
                .Entry(goodsReceipt)
                .Property(gr => gr.Status)
                .IsModified = true;
        }
    }

    private async Task<(int Fulfillment, int Showroom)> ProcessTargetDocumentAllocation(
        GoodsReceiptAddItemRequest request,
        string                     warehouse,
        GoodsReceiptLine           line,
        int                        quantity,
        Guid                       userId) {
        if (!options.GoodsReceiptTargetDocuments)
            return (0, 0);
        
        var          documentsWaiting = await adapter.AddItemTargetDocuments(warehouse, request.ItemCode);
        LineStatus[] targetStatuses   = [LineStatus.Open, LineStatus.Finished, LineStatus.Processing];

        var targetData = await db.GoodsReceiptTargets
            .Where(v => v.ItemCode == request.ItemCode && v.WhsCode == warehouse && targetStatuses.Contains(v.TargetStatus))
            .GroupBy(v => new { v.TargetType, v.TargetEntry, v.TargetLine })
            .Select(v => new { v.Key.TargetType, v.Key.TargetEntry, v.Key.TargetLine, Quantity = v.Sum(q => q.TargetQuantity) })
            .ToListAsync();

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

        int fulfillment = 0, showroom = 0;

        foreach (var needingItem in documentsNeedingItems) {
            int scanQuantity   = needingItem.RemainingQuantity;
            int insertQuantity = quantity > scanQuantity ? scanQuantity : quantity;
            quantity -= insertQuantity;

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

            if (quantity == 0) {
                break;
            }
        }

        return (fulfillment, showroom);
    }

    private GoodsReceiptAddItemResponse BuildAddItemResponse(GoodsReceiptLine line,        ItemCheckResponse item,
        int                                                                   fulfillment, int               showroom, int quantity) {
        return new GoodsReceiptAddItemResponse {
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