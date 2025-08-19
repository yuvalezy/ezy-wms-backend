using Core.DTOs.GoodsReceipt;
using Core.DTOs.Items;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class GoodsReceiptLineItemProcessService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ISettings settings,
    ILogger<GoodsReceiptLineItemProcessService> logger)
: IGoodsReceiptLineItemProcessService {
    private readonly Options options = settings.Options;


    public async Task<ValidateGoodsReceiptAndItemResponse> ValidateGoodsReceiptAndItem(GoodsReceiptAddItemRequest request, Guid userId, string warehouse) {
        var goodsReceipt = await db.GoodsReceipts
        .Include(gr => gr.Documents)
        .Include(gr => gr.Lines)
        .FirstOrDefaultAsync(gr => gr.Id == request.Id && (gr.Status == ObjectStatus.Open || gr.Status == ObjectStatus.InProgress));

        if (goodsReceipt == null) {
            logger.LogWarning("Goods receipt {Id} not found or already closed for user {UserId}", request.Id, userId);
            return new ValidateGoodsReceiptAndItemResponse(new GoodsReceiptAddItemResponse {
                ErrorMessage = "Goods receipt not found or already closed",
                ClosedDocument = true
            }, null, null, null);
        }


        var specificDocuments = goodsReceipt.Documents.Select(d => new ObjectKey(d.ObjType, d.DocEntry, d.DocNumber)).ToList();
        var validationResult = await adapter.ValidateGoodsReceiptAddItem(request.ItemCode, request.BarCode, specificDocuments, warehouse, request.Unit == UnitType.Unit);
        if (!validationResult.IsValid) {
            logger.LogWarning("External adapter validation failed for item {ItemCode}: {ErrorMessage}", request.ItemCode, validationResult.ErrorMessage);
            return new ValidateGoodsReceiptAndItemResponse(new GoodsReceiptAddItemResponse {
                ErrorMessage = validationResult.ErrorMessage,
            }, null, null, null);
        }

        var items = await adapter.ItemCheckAsync(request.ItemCode, request.BarCode);
        var item = items.FirstOrDefault();
        if (item == null) {
            logger.LogWarning("Item {ItemCode} with barcode {BarCode} not found", request.ItemCode, request.BarCode);
            return new ValidateGoodsReceiptAndItemResponse(new GoodsReceiptAddItemResponse {
                ErrorMessage = "Item not found"
            }, null, null, null);
        }


        return new ValidateGoodsReceiptAndItemResponse(null, goodsReceipt, item, specificDocuments);
    }


    public async Task<ProcessSourceDocumentsAllocationResponse> ProcessSourceDocumentsAllocation(
        string itemCode,
        UnitType unit,
        string warehouse,
        GoodsReceipt goodsReceipt,
        ItemCheckResponse item,
        List<ObjectKey> specificDocuments,
        int quantity = 1,
        Guid? updateLineId = null) {
        var linesIds = goodsReceipt.Lines.Select(l => l.Id).ToList();

        var sourceDocuments = (await adapter.AddItemSourceDocuments(itemCode, unit, warehouse, goodsReceipt.Type, goodsReceipt.CardCode, specificDocuments)).ToList();

        // Subtract already allocated quantities
        var goodsReceiptSources = await db.GoodsReceiptSources
        .Where(g => linesIds.Contains(g.GoodsReceiptLineId))
        .ToListAsync();

        foreach (var sourceDocument in sourceDocuments) {
            int selectedQuantity = (int)goodsReceiptSources
            .Where(g => g.SourceType == sourceDocument.Type &&
                        g.SourceEntry == sourceDocument.Entry &&
                        g.SourceLine == sourceDocument.LineNum
                        && (updateLineId == null || g.GoodsReceiptLineId != updateLineId))
            .Sum(g => g.Quantity);

            sourceDocument.Quantity -= selectedQuantity;
        }

        sourceDocuments.RemoveAll(s => s.Quantity <= 0);

        // Calculate required quantity
        quantity = quantity * (unit != UnitType.Unit ? item.NumInBuy : 1) * (unit == UnitType.Pack ? item.PurPackUn : 1);

        // Allocate quantities using FIFO
        int unallocatedSourceQuantity = await AllocateSourceDocuments(sourceDocuments, quantity);

        // Handle over-receipt scenario
        await HandleOverReceiptScenario(linesIds, sourceDocuments, unallocatedSourceQuantity, itemCode);

        if (sourceDocuments.Count == 0) {
            logger.LogWarning("No source documents available for item {ItemCode} after allocation", itemCode);
            return new ProcessSourceDocumentsAllocationResponse(new GoodsReceiptAddItemResponse {
                ErrorMessage = $"No source documents found for item {itemCode}"
            }, null, 0);
        }

        return new ProcessSourceDocumentsAllocationResponse(null, sourceDocuments, quantity);
    }

    private static Task<int> AllocateSourceDocuments(List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments, int quantity) {
        for (int i = 0; i < sourceDocuments.Count; i++) {
            var sourceDocument = sourceDocuments[i];
            int iQty = sourceDocument.Quantity;

            if (iQty <= quantity) {
                quantity -= iQty;
                if (quantity == 0) {
                    int removedCount = sourceDocuments.Count - (i + 1);
                    sourceDocuments.RemoveRange(i + 1, removedCount);
                    break;
                }
            }
            else {
                sourceDocument.Quantity = quantity;
                int removedCount = sourceDocuments.Count - (i + 1);
                sourceDocuments.RemoveRange(i + 1, removedCount);

                quantity = 0;
                break;
            }
        }

        return Task.FromResult(quantity);
    }

    private async Task HandleOverReceiptScenario(List<Guid> linesIds, List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments, int quantity, string itemCode) {
        if (quantity <= 0) {
            return;
        }

        logger.LogInformation("Handling over-receipt scenario: {OverQuantity} units need allocation", quantity);

        if (sourceDocuments.Count > 0) {
            var lastDocument = sourceDocuments.Last();
            lastDocument.Quantity += quantity;
        }
        else {
            var fallback = await db.GoodsReceiptSources
            .Include(v => v.GoodsReceiptLine)
            .Where(v => linesIds.Contains(v.GoodsReceiptLineId) && v.GoodsReceiptLine.ItemCode == itemCode)
            .OrderBy(v => v.SourceType == 20 ? 'A' : v.SourceType == 22 ? 'B' : 'C')
            .ThenByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();

            if (fallback != null) {
                sourceDocuments.Add(new GoodsReceiptAddItemSourceDocumentResponse {
                    Type = fallback.SourceType,
                    Entry = fallback.SourceEntry,
                    Number = fallback.SourceNumber,
                    LineNum = fallback.SourceLine,
                    Quantity = quantity
                });
            }
            else {
                logger.LogWarning("No fallback source document found for over-receipt scenario");
            }
        }
    }

    public async Task<GoodsReceiptLine> CreateGoodsReceiptLine(
        GoodsReceiptAddItemRequest request,
        GoodsReceipt goodsReceipt,
        List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments,
        int quantity,
        Guid userId) {
        var line = new GoodsReceiptLine {
            GoodsReceiptId = goodsReceipt.Id,
            ItemCode = request.ItemCode,
            BarCode = request.BarCode,
            Quantity = quantity,
            Unit = request.Unit,
            Date = DateTime.UtcNow,
            LineStatus = LineStatus.Open,
            CreatedByUserId = userId,
        };

        await db.GoodsReceiptLines.AddAsync(line);

        // Insert source document allocations
        foreach (var s in sourceDocuments) {
            var source = new GoodsReceiptSource {
                CreatedByUserId = userId,
                Quantity = s.Quantity,
                SourceEntry = s.Entry,
                SourceNumber = s.Number,
                SourceLine = s.LineNum,
                SourceType = s.Type,
                GoodsReceiptLineId = line.Id,
            };

            await db.GoodsReceiptSources.AddAsync(source);
        }

        return line;
    }

    public void UpdateGoodsReceiptStatus(GoodsReceipt goodsReceipt) {
        if (goodsReceipt.Status == ObjectStatus.InProgress) {
            return;
        }

        goodsReceipt.Status = ObjectStatus.InProgress;
        db.GoodsReceipts
        .Entry(goodsReceipt)
        .Property(gr => gr.Status)
        .IsModified = true;
    }

    public async Task<(int Fulfillment, int Showroom)> ProcessTargetDocumentAllocation(
        GoodsReceiptAddItemRequest request,
        string warehouse,
        GoodsReceiptLine line,
        int quantity,
        Guid userId) {
        if (!options.GoodsReceiptTargetDocuments) {
            return (0, 0);
        }

        var documentsWaiting = await adapter.AddItemTargetDocuments(warehouse, request.ItemCode);

        LineStatus[] targetStatuses = [LineStatus.Open, LineStatus.Finished, LineStatus.Processing];

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
                RequiredQuantity = waiting.Quantity,
                AllocatedQuantity = targets.Sum(t => (int)t.Quantity),
                RemainingQuantity = waiting.Quantity - targets.Sum(t => (int)t.Quantity)
            })
        .Where(doc => doc.RemainingQuantity > 0)
        .OrderBy(doc => doc.Priority)
        .ThenBy(doc => doc.Date)
        .ToList();

        int fulfillment = 0, showroom = 0;

        foreach (var needingItem in documentsNeedingItems) {
            int scanQuantity = needingItem.RemainingQuantity;
            int insertQuantity = quantity > scanQuantity ? scanQuantity : quantity;
            quantity -= insertQuantity;

            await db.GoodsReceiptTargets.AddAsync(new GoodsReceiptTarget {
                CreatedByUserId = userId,
                ItemCode = request.ItemCode,
                WhsCode = warehouse,
                TargetType = needingItem.Type,
                TargetEntry = needingItem.Entry,
                TargetLine = needingItem.LineNum,
                TargetQuantity = insertQuantity,
                TargetStatus = LineStatus.Open,
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

    public GoodsReceiptAddItemResponse BuildAddItemResponse(
        GoodsReceiptLine line,
        ItemCheckResponse item,
        int fulfillment,
        int showroom,
        int quantity) {
        int warehouseQuantity = quantity - fulfillment - showroom;

        var response = new GoodsReceiptAddItemResponse {
            LineId = line.Id,
            Fulfillment = fulfillment > 0,
            Showroom = showroom > 0,
            Warehouse = warehouseQuantity > 0,
            Quantity = 1,
            NumInBuy = item.NumInBuy,
            BuyUnitMsr = item.BuyUnitMsr,
            PurPackUn = item.PurPackUn,
            PurPackMsr = item.PurPackMsr,
            CustomFields = item.CustomFields
        };

        return response;
    }
}