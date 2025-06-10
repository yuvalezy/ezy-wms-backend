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

public class GoodsReceiptAddItemService(SystemDbContext db, IExternalSystemAdapter adapter, ISettings settings, ILogger<GoodsReceiptAddItemService> logger) : IGoodsReceiptAddItemService {
    private readonly Options options = settings.Options;
    public async Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo session, GoodsReceiptAddItemRequest request) {
        var userId    = session.Guid;
        string warehouse = session.Warehouse;

        logger.LogInformation("Adding item {ItemCode} (barcode: {BarCode}) to goods receipt {Id} for user {UserId} in warehouse {Warehouse}", 
            request.ItemCode, request.BarCode, request.Id, userId, warehouse);

        try {
            // Step 1: Validate goods receipt and item
            logger.LogDebug("Step 1: Validating goods receipt and item");
            var validationResult = await ValidateGoodsReceiptAndItem(request, userId, warehouse);
            if (validationResult.ErrorResponse != null) {
                logger.LogWarning("Validation failed for goods receipt {Id}: {ErrorMessage}", request.Id, validationResult.ErrorResponse.ErrorMessage);
                return validationResult.ErrorResponse;
            }

            var goodsReceipt      = validationResult.GoodsReceipt!;
            var item              = validationResult.Item!;
            var specificDocuments = validationResult.SpecificDocuments!;

            logger.LogDebug("Validation successful for goods receipt {Id}, item {ItemCode}, with {DocumentCount} specific documents", 
                goodsReceipt.Id, item.ItemCode, specificDocuments.Count);

            // Step 2: Process source documents allocation
            logger.LogDebug("Step 2: Processing source documents allocation");
            var sourceAllocationResult = await ProcessSourceDocumentsAllocation(request, warehouse, goodsReceipt, item, specificDocuments);
            if (sourceAllocationResult.ErrorResponse != null) {
                logger.LogWarning("Source allocation failed for item {ItemCode}: {ErrorMessage}", request.ItemCode, sourceAllocationResult.ErrorResponse.ErrorMessage);
                return sourceAllocationResult.ErrorResponse;
            }

            var sourceDocuments    = sourceAllocationResult.SourceDocuments!;
            int calculatedQuantity = sourceAllocationResult.CalculatedQuantity;

            logger.LogDebug("Source allocation successful: {DocumentCount} source documents, calculated quantity: {Quantity}", 
                sourceDocuments.Count, calculatedQuantity);

            // Step 3: Create goods receipt line
            logger.LogDebug("Step 3: Creating goods receipt line");
            var line = await CreateGoodsReceiptLine(request, goodsReceipt, sourceDocuments, calculatedQuantity, userId);
            logger.LogDebug("Created goods receipt line {LineId} with quantity {Quantity}", line.Id, line.Quantity);

            // Step 4: Update goods receipt status
            logger.LogDebug("Step 4: Updating goods receipt status");
            await UpdateGoodsReceiptStatus(goodsReceipt);

            // Step 5: Process target document allocation
            logger.LogDebug("Step 5: Processing target document allocation");
            var targetAllocationResult = await ProcessTargetDocumentAllocation(request, warehouse, line, calculatedQuantity, userId);
            logger.LogDebug("Target allocation result: Fulfillment={Fulfillment}, Showroom={Showroom}", 
                targetAllocationResult.Fulfillment, targetAllocationResult.Showroom);

            await db.SaveChangesAsync();
            logger.LogDebug("Database changes saved successfully");

            // Step 6: Build response
            logger.LogDebug("Step 6: Building response");
            var response = BuildAddItemResponse(line, item, targetAllocationResult.Fulfillment, targetAllocationResult.Showroom, calculatedQuantity);
            
            logger.LogInformation("Successfully added item {ItemCode} to goods receipt {Id}. LineId: {LineId}, Quantity: {Quantity}", 
                request.ItemCode, request.Id, line.Id, calculatedQuantity);

            return response;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to add item {ItemCode} to goods receipt {Id} for user {UserId}", 
                request.ItemCode, request.Id, userId);
            throw;
        }
    }

    private async Task<(GoodsReceiptAddItemResponse? ErrorResponse, GoodsReceipt? GoodsReceipt, ItemCheckResponse? Item, List<ObjectKey>? SpecificDocuments)>
        ValidateGoodsReceiptAndItem(GoodsReceiptAddItemRequest request, Guid userId, string warehouse) {
        
        logger.LogDebug("Validating goods receipt {Id} for user {UserId}", request.Id, userId);
        
        var goodsReceipt = await db.GoodsReceipts
            .Include(gr => gr.Documents)
            .Include(gr => gr.Lines)
            .FirstOrDefaultAsync(gr => gr.Id == request.Id && (gr.Status == ObjectStatus.Open || gr.Status == ObjectStatus.InProgress));

        if (goodsReceipt == null) {
            logger.LogWarning("Goods receipt {Id} not found or already closed for user {UserId}", request.Id, userId);
            return (new GoodsReceiptAddItemResponse {
                ErrorMessage   = "Goods receipt not found or already closed",
                ClosedDocument = true
            }, null, null, null);
        }

        logger.LogDebug("Found goods receipt {Id} with status {Status}, {DocumentCount} documents, {LineCount} lines", 
            goodsReceipt.Id, goodsReceipt.Status, goodsReceipt.Documents.Count, goodsReceipt.Lines.Count);

        var specificDocuments = goodsReceipt.Documents.Select(d => new ObjectKey(d.ObjType, d.DocEntry, d.DocNumber)).ToList();
        
        logger.LogDebug("Validating item addition with external adapter for item {ItemCode}", request.ItemCode);
        var validationResult  = await adapter.ValidateGoodsReceiptAddItem(request, specificDocuments, userId, warehouse);
        if (!validationResult.IsValid) {
            logger.LogWarning("External adapter validation failed for item {ItemCode}: {ErrorMessage}", request.ItemCode, validationResult.ErrorMessage);
            return (new GoodsReceiptAddItemResponse {
                ErrorMessage = validationResult.ErrorMessage,
            }, null, null, null);
        }

        logger.LogDebug("Checking item {ItemCode} with barcode {BarCode}", request.ItemCode, request.BarCode);
        var items = await adapter.ItemCheckAsync(request.ItemCode, request.BarCode);
        var item  = items.FirstOrDefault();
        if (item == null) {
            logger.LogWarning("Item {ItemCode} with barcode {BarCode} not found", request.ItemCode, request.BarCode);
            return (new GoodsReceiptAddItemResponse {
                ErrorMessage = "Item not found"
            }, null, null, null);
        }

        logger.LogDebug("Item validation successful: {ItemCode}, NumInBuy: {NumInBuy}, PurPackUn: {PurPackUn}", 
            item.ItemCode, item.NumInBuy, item.PurPackUn);

        return (null, goodsReceipt, item, specificDocuments);
    }

    private async Task<(GoodsReceiptAddItemResponse? ErrorResponse, List<GoodsReceiptAddItemSourceDocumentResponse>? SourceDocuments, int CalculatedQuantity)>
        ProcessSourceDocumentsAllocation(GoodsReceiptAddItemRequest request, string          warehouse, GoodsReceipt goodsReceipt,
            ItemCheckResponse                                       item,    List<ObjectKey> specificDocuments) {
        
        logger.LogDebug("Processing source documents allocation for item {ItemCode} in goods receipt {GoodsReceiptId}", 
            request.ItemCode, goodsReceipt.Id);
        
        var linesIds        = goodsReceipt.Lines.Select(l => l.Id).ToList();
        logger.LogDebug("Found {LineCount} existing lines in goods receipt", linesIds.Count);
        
        var sourceDocuments = (await adapter.AddItemSourceDocuments(request, warehouse, goodsReceipt.Type, goodsReceipt.CardCode, specificDocuments)).ToList();
        logger.LogDebug("Retrieved {SourceDocumentCount} source documents from adapter", sourceDocuments.Count);

        // Subtract already allocated quantities
        var goodsReceiptSources = await db.GoodsReceiptSources
            .Where(g => linesIds.Contains(g.GoodsReceiptLineId))
            .ToListAsync();
        
        logger.LogDebug("Found {SourceAllocationCount} existing source allocations", goodsReceiptSources.Count);

        foreach (var sourceDocument in sourceDocuments) {
            int selectedQuantity = (int)goodsReceiptSources
                .Where(g => g.SourceType == sourceDocument.Type &&
                            g.SourceEntry == sourceDocument.Entry &&
                            g.SourceLine == sourceDocument.LineNum)
                .Sum(g => g.Quantity);
            
            int originalQuantity = sourceDocument.Quantity;
            sourceDocument.Quantity -= selectedQuantity;
            
            if (selectedQuantity > 0) {
                logger.LogDebug("Adjusted source document Type={Type}, Entry={Entry}, Line={Line}: {OriginalQuantity} - {AllocatedQuantity} = {RemainingQuantity}",
                    sourceDocument.Type, sourceDocument.Entry, sourceDocument.LineNum, originalQuantity, selectedQuantity, sourceDocument.Quantity);
            }
        }

        int originalSourceCount = sourceDocuments.Count;
        sourceDocuments.RemoveAll(s => s.Quantity <= 0);
        
        if (sourceDocuments.Count < originalSourceCount) {
            logger.LogDebug("Removed {RemovedCount} fully allocated source documents", originalSourceCount - sourceDocuments.Count);
        }

        // Calculate required quantity
        int quantity = 1 * (request.Unit != UnitType.Unit ? item.NumInBuy : 1) * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
        logger.LogDebug("Calculated required quantity: {Quantity} (Unit: {Unit}, NumInBuy: {NumInBuy}, PurPackUn: {PurPackUn})", 
            quantity, request.Unit, item.NumInBuy, item.PurPackUn);

        // Allocate quantities using FIFO
        logger.LogDebug("Allocating source documents using FIFO");
        int unallocatedSourceQuantity = await AllocateSourceDocuments(sourceDocuments, quantity);

        // Handle over-receipt scenario
        logger.LogDebug("Handling over-receipt scenario");
        await HandleOverReceiptScenario(sourceDocuments, unallocatedSourceQuantity);

        if (sourceDocuments.Count == 0) {
            logger.LogWarning("No source documents available for item {ItemCode} after allocation", request.ItemCode);
            return (new GoodsReceiptAddItemResponse {
                ErrorMessage = $"No source documents found for item {request.ItemCode}"
            }, null, 0);
        }

        logger.LogDebug("Source allocation completed: {FinalSourceCount} source documents, total quantity: {TotalQuantity}", 
            sourceDocuments.Count, sourceDocuments.Sum(s => s.Quantity));

        return (null, sourceDocuments, quantity);
    }

    private async Task<int> AllocateSourceDocuments(List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments, int quantity) {
        logger.LogDebug("Starting FIFO allocation with {SourceCount} documents and required quantity {Quantity}", 
            sourceDocuments.Count, quantity);
        
        for (int i = 0; i < sourceDocuments.Count; i++) {
            var sourceDocument = sourceDocuments[i];
            int iQty           = sourceDocument.Quantity;

            logger.LogDebug("Processing source document {Index}: Type={Type}, Entry={Entry}, Line={Line}, Quantity={Quantity}", 
                i, sourceDocument.Type, sourceDocument.Entry, sourceDocument.LineNum, iQty);

            if (iQty <= quantity) {
                quantity -= iQty;
                logger.LogDebug("Fully allocated source document {Index}, remaining quantity needed: {RemainingQuantity}", i, quantity);
                
                if (quantity == 0) {
                    int removedCount = sourceDocuments.Count - (i + 1);
                    sourceDocuments.RemoveRange(i + 1, removedCount);
                    logger.LogDebug("Allocation complete, removed {RemovedCount} excess source documents", removedCount);
                    break;
                }
            }
            else {
                int originalQuantity = sourceDocument.Quantity;
                sourceDocument.Quantity = quantity;
                int removedCount = sourceDocuments.Count - (i + 1);
                sourceDocuments.RemoveRange(i + 1, removedCount);
                
                logger.LogDebug("Partially allocated source document {Index}: {OriginalQuantity} -> {AllocatedQuantity}, removed {RemovedCount} excess documents", 
                    i, originalQuantity, quantity, removedCount);
                
                quantity = 0;
                break;
            }
        }
        
        logger.LogDebug("FIFO allocation completed with {FinalSourceCount} allocated documents", sourceDocuments.Count);
        return quantity;
    }

    private async Task HandleOverReceiptScenario(List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments, int quantity) {
        if (quantity > 0) {
            logger.LogInformation("Handling over-receipt scenario: {OverQuantity} units need allocation", quantity);
            
            if (sourceDocuments.Count > 0) {
                var lastDocument = sourceDocuments.Last();
                int originalQuantity = lastDocument.Quantity;
                lastDocument.Quantity += quantity;
                
                logger.LogDebug("Added over-receipt quantity to last source document: Type={Type}, Entry={Entry}, Line={Line}, {OriginalQuantity} + {OverQuantity} = {NewQuantity}",
                    lastDocument.Type, lastDocument.Entry, lastDocument.LineNum, originalQuantity, quantity, lastDocument.Quantity);
            }
            else {
                logger.LogDebug("No source documents available, searching for fallback source document");
                
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
                    
                    logger.LogDebug("Created fallback source document for over-receipt: Type={Type}, Entry={Entry}, Line={Line}, Quantity={Quantity}",
                        fallback.SourceType, fallback.SourceEntry, fallback.SourceLine, quantity);
                }
                else {
                    logger.LogWarning("No fallback source document found for over-receipt scenario");
                }
            }
        }
        else {
            logger.LogDebug("No over-receipt scenario to handle");
        }
    }

    private async Task<GoodsReceiptLine> CreateGoodsReceiptLine(
        GoodsReceiptAddItemRequest                      request,
        GoodsReceipt                                    goodsReceipt,
        List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments,
        int                                             quantity, Guid userId) {
        
        logger.LogDebug("Creating goods receipt line for item {ItemCode} with quantity {Quantity} in goods receipt {GoodsReceiptId}", 
            request.ItemCode, quantity, goodsReceipt.Id);
        
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
        logger.LogDebug("Added goods receipt line to database with temporary ID");

        // Insert source document allocations
        logger.LogDebug("Creating {SourceCount} source document allocations", sourceDocuments.Count);
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
            
            logger.LogDebug("Added source allocation: Type={Type}, Entry={Entry}, Line={Line}, Quantity={Quantity}", 
                s.Type, s.Entry, s.LineNum, s.Quantity);
        }

        logger.LogDebug("Goods receipt line creation completed");
        return line;
    }

    private async Task UpdateGoodsReceiptStatus(GoodsReceipt goodsReceipt) {
        if (goodsReceipt.Status != ObjectStatus.InProgress) {
            var oldStatus = goodsReceipt.Status;
            goodsReceipt.Status = ObjectStatus.InProgress;
            db.GoodsReceipts
                .Entry(goodsReceipt)
                .Property(gr => gr.Status)
                .IsModified = true;
            
            logger.LogDebug("Updated goods receipt {Id} status from {OldStatus} to {NewStatus}", 
                goodsReceipt.Id, oldStatus, ObjectStatus.InProgress);
        }
        else {
            logger.LogDebug("Goods receipt {Id} status already InProgress, no update needed", goodsReceipt.Id);
        }
    }

    private async Task<(int Fulfillment, int Showroom)> ProcessTargetDocumentAllocation(
        GoodsReceiptAddItemRequest request,
        string                     warehouse,
        GoodsReceiptLine           line,
        int                        quantity,
        Guid                       userId) {
        
        logger.LogDebug("Processing target document allocation for item {ItemCode}, quantity {Quantity}", request.ItemCode, quantity);
        
        if (!options.GoodsReceiptTargetDocuments) {
            logger.LogDebug("Target document allocation disabled in options");
            return (0, 0);
        }
        
        var documentsWaiting = await adapter.AddItemTargetDocuments(warehouse, request.ItemCode);
        logger.LogDebug("Retrieved {DocumentCount} waiting target documents from adapter", documentsWaiting.Count());
        
        LineStatus[] targetStatuses = [LineStatus.Open, LineStatus.Finished, LineStatus.Processing];

        var targetData = await db.GoodsReceiptTargets
            .Where(v => v.ItemCode == request.ItemCode && v.WhsCode == warehouse && targetStatuses.Contains(v.TargetStatus))
            .GroupBy(v => new { v.TargetType, v.TargetEntry, v.TargetLine })
            .Select(v => new { v.Key.TargetType, v.Key.TargetEntry, v.Key.TargetLine, Quantity = v.Sum(q => q.TargetQuantity) })
            .ToListAsync();
        
        logger.LogDebug("Found {ExistingTargetCount} existing target allocations", targetData.Count);

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

        logger.LogDebug("Found {NeedingDocumentCount} documents needing items after filtering", documentsNeedingItems.Count);

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

            logger.LogDebug("Allocated {Quantity} units to target document: Type={Type}, Entry={Entry}, Line={Line}", 
                insertQuantity, needingItem.Type, needingItem.Entry, needingItem.LineNum);

            switch (needingItem.Type) {
                case 1250000001:
                    showroom += insertQuantity;
                    logger.LogDebug("Added {Quantity} units to showroom allocation", insertQuantity);
                    break;
                case 13 or 17:
                    fulfillment += insertQuantity;
                    logger.LogDebug("Added {Quantity} units to fulfillment allocation (type {Type})", insertQuantity, needingItem.Type);
                    break;
                default:
                    logger.LogDebug("Target document type {Type} not categorized as fulfillment or showroom", needingItem.Type);
                    break;
            }

            if (quantity == 0) {
                logger.LogDebug("All quantity allocated, stopping target allocation");
                break;
            }
        }

        logger.LogDebug("Target allocation completed: Fulfillment={Fulfillment}, Showroom={Showroom}, Remaining={Remaining}", 
            fulfillment, showroom, quantity);

        return (fulfillment, showroom);
    }

    private GoodsReceiptAddItemResponse BuildAddItemResponse(GoodsReceiptLine line,        ItemCheckResponse item,
        int                                                                   fulfillment, int               showroom, int quantity) {
        
        int warehouseQuantity = quantity - fulfillment - showroom;
        
        logger.LogDebug("Building response: LineId={LineId}, Fulfillment={Fulfillment}, Showroom={Showroom}, Warehouse={Warehouse}", 
            line.Id, fulfillment > 0, showroom > 0, warehouseQuantity > 0);
        
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
        
        logger.LogDebug("Response built successfully with allocation flags: Fulfillment={Fulfillment}, Showroom={Showroom}, Warehouse={Warehouse}", 
            response.Fulfillment, response.Showroom, response.Warehouse);
        
        return response;
    }
}