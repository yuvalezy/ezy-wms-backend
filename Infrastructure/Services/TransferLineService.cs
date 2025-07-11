using Core.DTOs.General;
using Core.DTOs.Transfer;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class TransferLineService(
    SystemDbContext            db,
    IExternalSystemAdapter     adapter,
    ITransferValidationService transferValidationService) : ITransferLineService {
    public async Task<TransferAddItemResponse> AddItem(SessionInfo info, TransferAddItemRequest request) {
        // Standard item transfer logic only - package operations are now handled by dedicated endpoints
        if (!await transferValidationService.ValidateAddItemAsync(info, request))
            return new TransferAddItemResponse { ClosedTransfer = true };

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            var transfer = await db.Transfers.FindAsync(request.ID);
            if (transfer == null) {
                throw new KeyNotFoundException($"Transfer with ID {request.ID} not found.");
            }

            int quantity = request.Quantity;
            if (request.Unit != UnitType.Unit) {
                var items = await adapter.ItemCheckAsync(request.ItemCode, request.BarCode);
                var item  = items.FirstOrDefault();
                if (item == null) {
                    throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, request.BarCode });
                }

                quantity *= item.NumInBuy * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
            }

            var line = new TransferLine {
                ItemCode        = request.ItemCode,
                BarCode         = request.BarCode,
                BinEntry        = request.BinEntry,
                Date            = DateTime.UtcNow,
                Quantity        = quantity,
                Type            = request.Type,
                UnitType        = request.Unit!.Value,
                TransferId      = request.ID,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = info.Guid,
                LineStatus      = LineStatus.Open
            };

            db.TransferLines.Add(line);

            if (transfer.Status == ObjectStatus.Open)
                transfer.Status = ObjectStatus.InProgress;

            db.Update(transfer);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new TransferAddItemResponse { LineId = line.Id };
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }


    public async Task<UpdateLineResponse> UpdateLine(SessionInfo info, TransferUpdateLineRequest request) {
        var response = new UpdateLineResponse();

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Validate transfer exists and is in valid state
            var transfer = await db.Transfers
                .Where(t => t.Id == request.Id)
                .Select(t => new { t.Status })
                .FirstOrDefaultAsync();

            if (transfer == null) {
                response.ReturnValue  = UpdateLineReturnValue.Status;
                response.ErrorMessage = "Transfer not found";
                return response;
            }

            if (transfer.Status != ObjectStatus.Open && transfer.Status != ObjectStatus.InProgress) {
                response.ReturnValue  = UpdateLineReturnValue.Status;
                response.ErrorMessage = "Transfer status is not Open or In Progress";
                return response;
            }

            // Find the line to update
            var line = await db.TransferLines
                .Where(tl => tl.Id == request.LineId && tl.TransferId == request.Id)
                .FirstOrDefaultAsync();

            if (line == null) {
                response.ReturnValue  = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Transfer line not found";
                return response;
            }

            if (line.LineStatus == LineStatus.Closed) {
                response.ReturnValue  = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Line is already closed";
                return response;
            }

            // Update comments if provided
            if (request.Comment != null) {
                line.Comments = request.Comment;
            }

            // Handle quantity update if provided
            if (request.Quantity.HasValue) {
                throw new InvalidOperationException("use /api/transfer/updateLineQuantity endpoint to update quantity");
            }

            // Handle line closure
            if (request.CancellationReasonId.HasValue) {
                // Validate the cancellation reason exists and is enabled
                var cancellationReason = await db.CancellationReasons
                    .Where(cr => cr.Id == request.CancellationReasonId.Value && cr.IsEnabled && cr.Transfer)
                    .FirstOrDefaultAsync();

                if (cancellationReason == null) {
                    response.ReturnValue  = UpdateLineReturnValue.CloseReason;
                    response.ErrorMessage = "Invalid or disabled cancellation reason for transfers";
                    return response;
                }

                line.LineStatus           = LineStatus.Closed;
                line.CancellationReasonId = request.CancellationReasonId.Value;
            }

            // Update modification tracking
            line.UpdatedAt       = DateTime.UtcNow;
            line.UpdatedByUserId = info.Guid;

            db.Update(line);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return response;
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<UpdateLineResponse> UpdateLineQuantity(SessionInfo info, TransferUpdateLineQuantityRequest request) {
        var response = new UpdateLineResponse();

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Find the line to update
            var line = await db.TransferLines
                .Include(tl => tl.Transfer)
                .Where(tl => tl.Id == request.LineId && tl.TransferId == request.Id)
                .FirstOrDefaultAsync();

            if (line == null) {
                response.ReturnValue  = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Transfer line not found";
                return response;
            }

            if (line.Transfer.Status != ObjectStatus.Open && line.Transfer.Status != ObjectStatus.InProgress) {
                response.ReturnValue  = UpdateLineReturnValue.Status;
                response.ErrorMessage = "Transfer status is not Open or In Progress";
                return response;
            }

            if (line.LineStatus == LineStatus.Closed) {
                response.ReturnValue  = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Line is already closed";
                return response;
            }

            // Calculate the new quantity based on unit type
            int newQuantity = request.Quantity;
            if (line.UnitType != UnitType.Unit) {
                var items = await adapter.ItemCheckAsync(line.ItemCode, null);
                var item  = items.FirstOrDefault();
                if (item != null) {
                    newQuantity *= item.NumInBuy;
                    if (line.UnitType == UnitType.Pack) {
                        newQuantity *= item.PurPackUn;
                    }
                }
            }

            // Validate quantity availability for source lines
            if (line is { Type: SourceTarget.Source, BinEntry: not null }) {
                var validationResult = await adapter.GetItemValidationInfo(line.ItemCode, line.BarCode, info.Warehouse, line.BinEntry, info.EnableBinLocations);

                // Calculate existing quantities for this item/bin excluding current line
                int existingSourceQuantity = await db.TransferLines
                    .Where(tl => tl.TransferId == request.Id &&
                                 tl.ItemCode == line.ItemCode &&
                                 tl.BinEntry == line.BinEntry.Value &&
                                 tl.Type == SourceTarget.Source &&
                                 tl.LineStatus != LineStatus.Closed &&
                                 tl.Id != line.Id)
                    .SumAsync(tl => tl.Quantity);

                decimal availableInBin = validationResult.AvailableQuantity - existingSourceQuantity;
                if (availableInBin < newQuantity) {
                    response.ReturnValue  = UpdateLineReturnValue.QuantityMoreThenAvailable;
                    response.ErrorMessage = "Quantity more than available in bin";
                    return response;
                }
            }

            if (line.Type == SourceTarget.Target) {
                // Validate that the source has enough quantity to support this target quantity
                var allItemLines = await db.TransferLines
                    .Where(tl => tl.TransferId == request.Id &&
                                 tl.ItemCode == line.ItemCode &&
                                 tl.LineStatus != LineStatus.Closed)
                    .ToListAsync();

                // Calculate total source quantity for this item
                int totalSourceQuantity = allItemLines
                    .Where(l => l.Type == SourceTarget.Source)
                    .Sum(l => l.Quantity);

                // Calculate total target quantity excluding current line being updated
                int otherTargetQuantity = allItemLines
                    .Where(l => l.Type == SourceTarget.Target && l.Id != line.Id)
                    .Sum(l => l.Quantity);

                // Check if new target quantity would exceed available source quantity
                int totalTargetWithNewQuantity = otherTargetQuantity + newQuantity;
                if (totalTargetWithNewQuantity > totalSourceQuantity) {
                    response.ReturnValue = UpdateLineReturnValue.QuantityMoreThenAvailable;
                    // response.ErrorMessage = $"Target quantity ({totalTargetWithNewQuantity}) exceeds available source quantity ({totalSourceQuantity}) for item {line.ItemCode}";
                    return response;
                }
            }

            // Update the quantity
            line.Quantity = newQuantity;

            // Update modification tracking
            line.UpdatedAt       = DateTime.UtcNow;
            line.UpdatedByUserId = info.Guid;

            db.Update(line);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return response;
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

}