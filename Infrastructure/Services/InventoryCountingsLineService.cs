using System.ComponentModel.DataAnnotations;
using Core.DTOs.General;
using Core.DTOs.InventoryCounting;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class InventoryCountingsLineService(SystemDbContext db, IExternalSystemAdapter adapter)
    : IInventoryCountingsLineService {
    public async Task<InventoryCountingAddItemResponse> AddItem(SessionInfo sessionInfo, InventoryCountingAddItemRequest request) {
        // Validate the counting exists and is in a valid state
        var counting = await db.InventoryCountings.FindAsync(request.ID);
        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {request.ID} not found.");
        }

        if (counting.Status != ObjectStatus.Open && counting.Status != ObjectStatus.InProgress) {
            return new InventoryCountingAddItemResponse {
                Status         = ResponseStatus.Error,
                ErrorMessage   = "Counting must be Open or In Progress to add items",
                ClosedCounting = true
            };
        }

        // Validate the item and barcode
        var validationResult = await adapter.GetItemValidationInfo(request.ItemCode, request.BarCode, sessionInfo.Warehouse, request.BinEntry, sessionInfo.EnableBinLocations);

        if (!validationResult.IsValidBarCode) {
            throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeBarCodeMismatch, new { request.ItemCode, request.BarCode });
        }

        if (!validationResult.IsInventoryItem) {
            throw new ApiErrorException((int)AddItemReturnValueType.NotStockItem, new { request.ItemCode, request.BarCode });
        }

        // Calculate total quantity including unit conversion
        decimal totalQuantity = request.Quantity;
        if (request.Unit != UnitType.Unit) {
            totalQuantity *= validationResult.NumInBuy;
            if (request.Unit == UnitType.Pack) {
                totalQuantity *= validationResult.PurPackUn;
            }
        }

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Create counting line
            var line = new InventoryCountingLine {
                InventoryCountingId = request.ID,
                ItemCode            = request.ItemCode,
                BarCode             = request.BarCode,
                Quantity            = totalQuantity,
                BinEntry            = request.BinEntry,
                Unit                = request.Unit,
                Date                = DateTime.UtcNow,
                LineStatus          = LineStatus.Open,
                CreatedByUserId     = sessionInfo.Guid
            };

            await db.InventoryCountingLines.AddAsync(line);

            // Update counting status if it was Open
            if (counting.Status == ObjectStatus.Open) {
                counting.Status          = ObjectStatus.InProgress;
                counting.UpdatedAt       = DateTime.UtcNow;
                counting.UpdatedByUserId = sessionInfo.Guid;
                db.Update(counting);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return InventoryCountingAddItemResponse.Success(line.Id);
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<UpdateLineResponse> UpdateLine(SessionInfo sessionInfo, InventoryCountingUpdateLineRequest request) {
        var response = new UpdateLineResponse();

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Validate counting exists and is in valid state
            var counting = await db.InventoryCountings
                .Where(ic => ic.Id == request.Id)
                .Select(ic => new { ic.Status })
                .FirstOrDefaultAsync();

            if (counting == null) {
                response.ReturnValue  = UpdateLineReturnValue.Status;
                response.ErrorMessage = "Inventory counting not found";
                return response;
            }

            if (counting.Status != ObjectStatus.Open && counting.Status != ObjectStatus.InProgress) {
                response.ReturnValue  = UpdateLineReturnValue.Status;
                response.ErrorMessage = "Counting status is not Open or In Progress";
                return response;
            }

            // Find the line to update
            var line = await db.InventoryCountingLines
                .Where(icl => icl.Id == request.LineId && icl.InventoryCountingId == request.Id)
                .FirstOrDefaultAsync();

            if (line == null) {
                response.ReturnValue  = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Counting line not found";
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

            // Update quantity if provided
            if (request.Quantity.HasValue) {
                decimal newQuantity = request.Quantity.Value;
                var items       = await adapter.ItemCheckAsync(line.ItemCode, null);
                var item        = items.FirstOrDefault();
                if (line.Unit != UnitType.Unit && item != null) {
                    newQuantity *= item.NumInBuy;
                    if (line.Unit == UnitType.Pack) {
                        newQuantity *= item.PurPackUn;
                    }
                }

                line.Quantity = newQuantity;
            }

            // Handle line closure
            if (request.CancellationReasonId.HasValue) {
                // Validate the cancellation reason exists and is enabled
                var cancellationReason = await db.CancellationReasons
                    .Where(cr => cr.Id == request.CancellationReasonId.Value && cr.IsEnabled && cr.Counting)
                    .FirstOrDefaultAsync();

                if (cancellationReason == null) {
                    response.ReturnValue  = UpdateLineReturnValue.CloseReason;
                    response.ErrorMessage = "Invalid or disabled cancellation reason for counting";
                    return response;
                }

                line.LineStatus           = LineStatus.Closed;
                line.CancellationReasonId = request.CancellationReasonId.Value;
            }

            // Update modification tracking
            line.UpdatedAt       = DateTime.UtcNow;
            line.UpdatedByUserId = sessionInfo.Guid;

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
