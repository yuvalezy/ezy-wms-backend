using Core.DTOs;
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

public class GoodsReceiptLineItemService(SystemDbContext db, IExternalSystemAdapter adapter, ISettings settings, ILogger<GoodsReceiptLineItemService> logger, ILoggerFactory loggerFactory)
    : IGoodsReceiptLineItemService {
    private readonly Options options = settings.Options;

    public async Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo session, GoodsReceiptAddItemRequest request) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var             userId      = session.Guid;
        string          warehouse   = session.Warehouse;
        var             id          = request.Id;
        string          itemCode    = request.ItemCode;
        string          barcode     = request.BarCode;

        logger.LogInformation("Adding item {ItemCode} (barcode: {BarCode}) to goods receipt {Id} for user {UserId} in warehouse {Warehouse}", itemCode, barcode, id, userId, warehouse);

        try {
            // Step 1: Validate goods receipt and item
            var validationResult = await ValidateGoodsReceiptAndItem(request, userId, warehouse);
            if (validationResult.ErrorResponse != null) {
                logger.LogWarning("Validation failed for goods receipt {Id}: {ErrorMessage}", id, validationResult.ErrorResponse.ErrorMessage);
                return validationResult.ErrorResponse;
            }

            var goodsReceipt      = validationResult.GoodsReceipt!;
            var item              = validationResult.Item!;
            var specificDocuments = validationResult.SpecificDocuments!;

            // Step 2: Process source documents allocation
            var sourceAllocationResult = await ProcessSourceDocumentsAllocation(request.ItemCode, request.Unit, warehouse, goodsReceipt, item, specificDocuments);
            if (sourceAllocationResult.ErrorResponse != null) {
                logger.LogWarning("Source allocation failed for item {ItemCode}: {ErrorMessage}", itemCode, sourceAllocationResult.ErrorResponse.ErrorMessage);
                return sourceAllocationResult.ErrorResponse;
            }

            var sourceDocuments    = sourceAllocationResult.SourceDocuments!;
            int calculatedQuantity = sourceAllocationResult.CalculatedQuantity;

            // Step 3: Create goods receipt line
            var line = await CreateGoodsReceiptLine(request, goodsReceipt, sourceDocuments, calculatedQuantity, userId);

            // Step 4: Update goods receipt status
            UpdateGoodsReceiptStatus(goodsReceipt);

            // Step 5: Process target document allocation
            var targetAllocationResult = await ProcessTargetDocumentAllocation(request, warehouse, line, calculatedQuantity, userId);

            await db.SaveChangesAsync();

            // Step 6: Build response
            var response = BuildAddItemResponse(line, item, targetAllocationResult.Fulfillment, targetAllocationResult.Showroom, calculatedQuantity);

            logger.LogInformation("Successfully added item {ItemCode} to goods receipt {Id}. LineId: {LineId}, Quantity: {Quantity}", itemCode, id, line.Id, calculatedQuantity);

            await transaction.CommitAsync();

            return response;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to add item {ItemCode} to goods receipt {Id} for user {UserId}",
                itemCode, id, userId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<UpdateLineResponse> UpdateLineQuantity(SessionInfo session, UpdateGoodsReceiptLineQuantityRequest request) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            //Step 1: Load existing data
            var id     = request.Id;
            var lineId = request.LineId;
            var line = await db.GoodsReceiptLines
                .Include(l => l.GoodsReceipt)
                .ThenInclude(v => v.Documents)
                .FirstOrDefaultAsync(l => l.GoodsReceipt.Id == id && l.Id == lineId);

            if (line == null) {
                throw new KeyNotFoundException($"Line with ID {lineId} not found");
            }

            if (line.LineStatus == LineStatus.Closed) {
                return new UpdateLineResponse {
                    ReturnValue  = UpdateLineReturnValue.LineStatus,
                    ErrorMessage = "Cannot update closed line"
                };
            }

            string itemCode          = line.ItemCode;
            var    goodsReceipt      = line.GoodsReceipt;
            string warehouse         = goodsReceipt.WhsCode;
            var    item              = (await adapter.ItemCheckAsync(line.ItemCode, null)).First();
            var    unit              = line.Unit;
            var    specificDocuments = goodsReceipt.Documents.Select(d => new ObjectKey(d.ObjType, d.DocEntry, d.DocNumber)).ToList();

            // Step 2: Process source documents allocation
            var sourceAllocationResult = await ProcessSourceDocumentsAllocation(itemCode, unit, warehouse, goodsReceipt, item, specificDocuments, (int)request.Quantity, lineId);
            if (sourceAllocationResult.ErrorResponse != null) {
                logger.LogWarning("Source allocation failed for item {ItemCode}: {ErrorMessage}", itemCode, sourceAllocationResult.ErrorResponse.ErrorMessage);
                return new UpdateLineResponse {
                    ErrorMessage = sourceAllocationResult.ErrorResponse.ErrorMessage,
                };
            }

            var sourceDocuments    = sourceAllocationResult.SourceDocuments!;
            int calculatedQuantity = sourceAllocationResult.CalculatedQuantity;

            // Step 3: Update line
            line.Quantity        = calculatedQuantity;
            line.UpdatedAt       = DateTime.UtcNow;
            line.UpdatedByUserId = session.Guid;

            // Step 4: Update source documents
            var lineSources = await db.GoodsReceiptSources.Where(v => v.GoodsReceiptLineId == line.Id).ToArrayAsync();
            foreach (var lineSource in lineSources) {
                var check = sourceDocuments.FirstOrDefault(v => v.Type == lineSource.SourceType && v.Entry == lineSource.SourceEntry && v.LineNum == lineSource.SourceLine);
                if (check != null) {
                    lineSource.Quantity        = check.Quantity;
                    lineSource.UpdatedAt       = DateTime.UtcNow;
                    lineSource.UpdatedByUserId = session.Guid;
                    check.Quantity             = 0;
                    db.GoodsReceiptSources.Update(lineSource);
                }
                else {
                    db.GoodsReceiptSources.Remove(lineSource);
                }
            }

            foreach (var sourceDocument in sourceDocuments.Where(v => v.Quantity > 0)) {
                var source = new GoodsReceiptSource {
                    CreatedByUserId    = session.Guid,
                    Quantity           = sourceDocument.Quantity,
                    SourceEntry        = sourceDocument.Entry,
                    SourceLine         = sourceDocument.LineNum,
                    SourceType         = sourceDocument.Type,
                    GoodsReceiptLineId = line.Id,
                };
                await db.GoodsReceiptSources.AddAsync(source);
            }

            await db.SaveChangesAsync();

            await transaction.CommitAsync();

            return new UpdateLineResponse { ReturnValue = UpdateLineReturnValue.Ok };
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to update line {LineId} for goods receipt {Id}", request.LineId, request.Id);
            await transaction.RollbackAsync();
            throw;
        }
    }

    private record ValidateGoodsReceiptAndItemResponse(GoodsReceiptAddItemResponse? ErrorResponse, GoodsReceipt? GoodsReceipt, ItemCheckResponse? Item, List<ObjectKey>? SpecificDocuments);

    private async Task<ValidateGoodsReceiptAndItemResponse> ValidateGoodsReceiptAndItem(GoodsReceiptAddItemRequest request, Guid userId, string warehouse) {
        var goodsReceipt = await db.GoodsReceipts
            .Include(gr => gr.Documents)
            .Include(gr => gr.Lines)
            .FirstOrDefaultAsync(gr => gr.Id == request.Id && (gr.Status == ObjectStatus.Open || gr.Status == ObjectStatus.InProgress));

        if (goodsReceipt == null) {
            logger.LogWarning("Goods receipt {Id} not found or already closed for user {UserId}", request.Id, userId);
            return new ValidateGoodsReceiptAndItemResponse(new GoodsReceiptAddItemResponse {
                ErrorMessage   = "Goods receipt not found or already closed",
                ClosedDocument = true
            }, null, null, null);
        }


        var specificDocuments = goodsReceipt.Documents.Select(d => new ObjectKey(d.ObjType, d.DocEntry, d.DocNumber)).ToList();
        var validationResult  = await adapter.ValidateGoodsReceiptAddItem(request.ItemCode, request.BarCode, specificDocuments, warehouse);
        if (!validationResult.IsValid) {
            logger.LogWarning("External adapter validation failed for item {ItemCode}: {ErrorMessage}", request.ItemCode, validationResult.ErrorMessage);
            return new ValidateGoodsReceiptAndItemResponse(new GoodsReceiptAddItemResponse {
                ErrorMessage = validationResult.ErrorMessage,
            }, null, null, null);
        }

        var items = await adapter.ItemCheckAsync(request.ItemCode, request.BarCode);
        var item  = items.FirstOrDefault();
        if (item == null) {
            logger.LogWarning("Item {ItemCode} with barcode {BarCode} not found", request.ItemCode, request.BarCode);
            return new ValidateGoodsReceiptAndItemResponse(new GoodsReceiptAddItemResponse {
                ErrorMessage = "Item not found"
            }, null, null, null);
        }


        return new ValidateGoodsReceiptAndItemResponse(null, goodsReceipt, item, specificDocuments);
    }

    private record ProcessSourceDocumentsAllocationResponse(GoodsReceiptAddItemResponse? ErrorResponse, List<GoodsReceiptAddItemSourceDocumentResponse>? SourceDocuments, int CalculatedQuantity);

    private async Task<ProcessSourceDocumentsAllocationResponse> ProcessSourceDocumentsAllocation(
        string            itemCode,
        UnitType          unit,
        string            warehouse,
        GoodsReceipt      goodsReceipt,
        ItemCheckResponse item,
        List<ObjectKey>   specificDocuments,
        int               quantity     = 1,
        Guid?             updateLineId = null) {
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
        await HandleOverReceiptScenario(sourceDocuments, unallocatedSourceQuantity);

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
            int iQty           = sourceDocument.Quantity;

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

    private async Task HandleOverReceiptScenario(List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments, int quantity) {
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
            else {
                logger.LogWarning("No fallback source document found for over-receipt scenario");
            }
        }
    }

    private async Task<GoodsReceiptLine> CreateGoodsReceiptLine(
        GoodsReceiptAddItemRequest                      request,
        GoodsReceipt                                    goodsReceipt,
        List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments,
        int                                             quantity,
        Guid                                            userId) {
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

    private void UpdateGoodsReceiptStatus(GoodsReceipt goodsReceipt) {
        if (goodsReceipt.Status == ObjectStatus.InProgress) {
            return;
        }

        goodsReceipt.Status = ObjectStatus.InProgress;
        db.GoodsReceipts
            .Entry(goodsReceipt)
            .Property(gr => gr.Status)
            .IsModified = true;
    }

    private async Task<(int Fulfillment, int Showroom)> ProcessTargetDocumentAllocation(
        GoodsReceiptAddItemRequest request,
        string                     warehouse,
        GoodsReceiptLine           line,
        int                        quantity,
        Guid                       userId) {
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
        int warehouseQuantity = quantity - fulfillment - showroom;

        var response = new GoodsReceiptAddItemResponse {
            LineId      = line.Id,
            Fulfillment = fulfillment > 0,
            Showroom    = showroom > 0,
            Warehouse   = warehouseQuantity > 0,
            Quantity    = 1,
            NumInBuy    = item.NumInBuy,
            BuyUnitMsr  = item.BuyUnitMsr,
            PurPackUn   = item.PurPackUn,
            PurPackMsr  = item.PurPackMsr
        };

        return response;
    }
}