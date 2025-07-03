using Core.DTOs;
using Core.DTOs.GoodsReceipt;
using Core.DTOs.Items;
using Core.DTOs.Package;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class GoodsReceiptLineService(
    SystemDbContext                     db,
    IExternalSystemAdapter              adapter,
    IGoodsReceiptLineItemProcessService lineItemProcessService,
    ILogger<GoodsReceiptLineService>    logger,
    ISettings                           settings,
    IPackageService                     packageService,
    IPackageContentService              packageContentService,
    IPackageContentService              contentService)
    : IGoodsReceiptLineService {
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

    public async Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo sessionInfo, GoodsReceiptAddItemRequest request) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var             userId      = sessionInfo.Guid;
        string          warehouse   = sessionInfo.Warehouse;
        var             id          = request.Id;
        string          itemCode    = request.ItemCode;
        string          barcode     = request.BarCode;

        logger.LogInformation("Adding item {ItemCode} (barcode: {BarCode}) to goods receipt {Id} for user {UserId} in warehouse {Warehouse}", itemCode, barcode, id, userId, warehouse);

        try {
            // Step 0: Create new package for this operation if enabled
            string? packageBarcode = null;
            if (request.StartNewPackage && settings.Options.EnablePackages) {
                var package = await packageService.CreatePackageAsync(sessionInfo, new CreatePackageRequest {
                    BinEntry            = sessionInfo.DefaultBinLocation,
                    SourceOperationType = ObjectType.GoodsReceipt,
                    SourceOperationId   = request.Id
                });

                request.PackageId = package.Id;
                packageBarcode    = package.Barcode;
            }

            // Step 1: Validate goods receipt and item
            var validationResult = await lineItemProcessService.ValidateGoodsReceiptAndItem(request, userId, warehouse);
            if (validationResult.ErrorResponse != null) {
                logger.LogWarning("Validation failed for goods receipt {Id}: {ErrorMessage}", id, validationResult.ErrorResponse.ErrorMessage);
                return validationResult.ErrorResponse;
            }

            var goodsReceipt      = validationResult.GoodsReceipt!;
            var item              = validationResult.Item!;
            var specificDocuments = validationResult.SpecificDocuments!;

            // Step 2: Process source documents allocation
            var sourceAllocationResult = await lineItemProcessService.ProcessSourceDocumentsAllocation(request.ItemCode, request.Unit, warehouse, goodsReceipt, item, specificDocuments);
            if (sourceAllocationResult.ErrorResponse != null) {
                logger.LogWarning("Source allocation failed for item {ItemCode}: {ErrorMessage}", itemCode, sourceAllocationResult.ErrorResponse.ErrorMessage);
                return sourceAllocationResult.ErrorResponse;
            }

            var sourceDocuments    = sourceAllocationResult.SourceDocuments!;
            int calculatedQuantity = sourceAllocationResult.CalculatedQuantity;

            // Step 3: Create goods receipt line
            var line = await lineItemProcessService.CreateGoodsReceiptLine(request, goodsReceipt, sourceDocuments, calculatedQuantity, userId);

            // Step 4: Update goods receipt status
            lineItemProcessService.UpdateGoodsReceiptStatus(goodsReceipt);

            // Step 5: Process target document allocation
            var targetAllocationResult = await lineItemProcessService.ProcessTargetDocumentAllocation(request, warehouse, line, calculatedQuantity, userId);

            // Step 6: Add item to package if package operation is active
            if (request.PackageId.HasValue) {
                await contentService.AddItemToPackageAsync(new AddItemToPackageRequest {
                    PackageId             = request.PackageId.Value,
                    ItemCode              = request.ItemCode,
                    Quantity              = 1.0m,
                    UnitQuantity          = line.Quantity,
                    UnitType              = line.Unit,
                    BinEntry              = sessionInfo.DefaultBinLocation,
                    SourceOperationType   = ObjectType.GoodsReceipt,
                    SourceOperationId     = request.Id,
                    SourceOperationLineId = line.Id
                }, sessionInfo);
            }

            await db.SaveChangesAsync();

            // Step 7: Build response
            var response = lineItemProcessService.BuildAddItemResponse(line, item, targetAllocationResult.Fulfillment, targetAllocationResult.Showroom, calculatedQuantity);

            // Step 8: Return enhanced response with package info
            if (request.StartNewPackage && settings.Options.EnablePackages && request.PackageId.HasValue) {
                response.PackageId      = request.PackageId;
                response.PackageBarcode = packageBarcode;
            }

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
        IDbContextTransaction? transaction      = null;
        bool                   closeTransaction = false;
        if (db.Database.CurrentTransaction == null) {
            transaction      = await db.Database.BeginTransactionAsync();
            closeTransaction = true;
        }

        try {
            //Step 1: Load existing data
            var lineId = request.LineId;
            var line = await db.GoodsReceiptLines
                .Include(l => l.GoodsReceipt)
                .ThenInclude(v => v.Documents)
                .FirstOrDefaultAsync(l => l.Id == lineId);

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
            var sourceAllocationResult =
                await lineItemProcessService.ProcessSourceDocumentsAllocation(itemCode, unit, warehouse, goodsReceipt, item, specificDocuments, (int)request.Quantity, lineId);
            if (sourceAllocationResult.ErrorResponse != null) {
                logger.LogWarning("Source allocation failed for item {ItemCode}: {ErrorMessage}", itemCode, sourceAllocationResult.ErrorResponse.ErrorMessage);
                return new UpdateLineResponse {
                    ErrorMessage = sourceAllocationResult.ErrorResponse.ErrorMessage,
                };
            }

            var sourceDocuments    = sourceAllocationResult.SourceDocuments!;
            int calculatedQuantity = sourceAllocationResult.CalculatedQuantity;

            // Step 3: Update line
            decimal diff = line.Quantity - calculatedQuantity;
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
                    SourceNumber       = sourceDocument.Number,
                    SourceLine         = sourceDocument.LineNum,
                    SourceType         = sourceDocument.Type,
                    GoodsReceiptLineId = line.Id,
                };
                await db.GoodsReceiptSources.AddAsync(source);
            }

            // Step 5: Add/Remove difference from the package
            await UpdatePackageContentBasedOnLineQuantity(session, line, item, diff);

            await db.SaveChangesAsync();

            if (closeTransaction && transaction != null) {
                await transaction.CommitAsync();
            }

            return new UpdateLineResponse { ReturnValue = UpdateLineReturnValue.Ok };
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to update line {LineId} for goods receipt {Id}", request.LineId, request.Id);
            if (closeTransaction && transaction != null) {
                await transaction.RollbackAsync();
            }

            throw;
        }
    }

    private async Task UpdatePackageContentBasedOnLineQuantity(SessionInfo session, GoodsReceiptLine line, ItemCheckResponse item, decimal diff) {
        if (diff == 0) {
            return;
        }

        var transaction = await db
            .PackageTransactions
            .Include(v => v.Package)
            .FirstOrDefaultAsync(v => v.SourceOperationLineId == line.Id);

        if (transaction == null) {
            return;
        }

        decimal diffUnit = diff;
        if (line.Unit != UnitType.Unit) {
            diffUnit /= item.NumInBuy;
            if (line.Unit == UnitType.Pack)
                diffUnit /= item.PurPackUn;
        }

        switch (diff) {
            case > 0: {
                var addRequest = new AddItemToPackageRequest {
                    PackageId             = transaction.PackageId,
                    ItemCode              = line.ItemCode,
                    Quantity              = diff,
                    UnitQuantity          = diffUnit,
                    UnitType              = line.Unit,
                    BinEntry              = transaction.Package.BinEntry,
                    SourceOperationType   = ObjectType.GoodsReceipt,
                    SourceOperationId     = line.GoodsReceiptId,
                    SourceOperationLineId = line.Id
                };
                await packageContentService.AddItemToPackageAsync(addRequest, session);
                break;
            }
            case < 0: {
                var removeRequest = new RemoveItemFromPackageRequest {
                    PackageId           = transaction.PackageId,
                    ItemCode            = line.ItemCode,
                    Quantity            = diff * -1,
                    UnitQuantity        = diffUnit * -1,
                    UnitType            = line.Unit,
                    SourceOperationType = ObjectType.GoodsReceipt,
                    SourceOperationId   = line.GoodsReceiptId
                };
                await packageContentService.RemoveItemFromPackageAsync(removeRequest, session);
                break;
            }
        }
    }

    public async Task RemoveRows(Guid[] rows, SessionInfo session) {
        if (rows.Length == 0)
            return;
        var lines = await db.GoodsReceiptLines.Where(v => rows.Contains(v.Id)).ToArrayAsync();

        var packageTransactions = await db
            .PackageTransactions
            .Where(v => v.SourceOperationLineId != null && rows.Contains(v.SourceOperationLineId.Value))
            .ToArrayAsync();

        foreach (var line in lines) {
            line.UpdatedAt       = DateTime.UtcNow;
            line.UpdatedByUserId = session.Guid;
            line.LineStatus      = LineStatus.Closed;
            line.Deleted         = true;
            line.DeletedAt       = DateTime.UtcNow;
            db.GoodsReceiptLines.Update(line);

            var transaction = packageTransactions.FirstOrDefault(v => v.SourceOperationLineId == line.Id);
            if (transaction == null)
                continue;
            decimal removeQuantity = line.Quantity;
            if (line.Unit != UnitType.Unit) {
                var data = await adapter.GetItemPurchaseUnits(line.ItemCode);
                removeQuantity /= data.QuantityInUnit;
                if (line.Unit == UnitType.Pack)
                    removeQuantity /= data.QuantityInPack;
            }

            var removeRequest = new RemoveItemFromPackageRequest {
                PackageId           = transaction.PackageId,
                ItemCode            = line.ItemCode,
                Quantity            = removeQuantity,
                UnitQuantity        = line.Quantity,
                UnitType            = line.Unit,
                SourceOperationType = ObjectType.GoodsReceipt,
                SourceOperationId   = line.GoodsReceiptId
            };
            await packageContentService.RemoveItemFromPackageAsync(removeRequest, session);
        }

        await db.SaveChangesAsync();
    }
}