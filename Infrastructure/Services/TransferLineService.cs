using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class TransferLineService(SystemDbContext db, IExternalSystemAdapter adapter) : ITransferLineService {
    public async Task<TransferAddItemResponse> AddItem(SessionInfo info, TransferAddItemRequest request) {
        if (!await ValidateAddItem(info, request))
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

            transfer.Lines.Add(line);
            if (transfer.Status == ObjectStatus.Open)
                transfer.Status = ObjectStatus.InProgress;

            db.Update(transfer);
            await transaction.CommitAsync();

            return new TransferAddItemResponse { LineID = line.Id };
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<bool> ValidateAddItem(SessionInfo info, TransferAddItemRequest request) {
        // Validate bin is required but not provided
        if (!request.BinEntry.HasValue && info.EnableBinLocations) {
            throw new ApiErrorException((int)AddItemReturnValueType.BinMissing, new { });
        }

        // Check if transfer exists and is in valid state
        var transfer = await db.Transfers
            .Where(t => t.Id == request.ID)
            .Select(t => new { t.Status })
            .FirstOrDefaultAsync();

        if (transfer == null) {
            throw new ApiErrorException((int)AddItemReturnValueType.TransactionIDNotExists, new { request.ID });
        }

        if (transfer.Status != ObjectStatus.Open && transfer.Status != ObjectStatus.InProgress) {
            return false; // Transfer is closed
        }

        // Get validation data from external system
        var validationResult = await adapter.GetItemValidationInfo(request.ItemCode, request.BarCode, info.Warehouse, request.BinEntry, info.EnableBinLocations);

        // Validate item exists
        if (!validationResult.IsValidItem) {
            throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, request.BarCode });
        }

        // Validate barcode matches
        if (!validationResult.IsValidBarCode) {
            throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeBarCodeMismatch, new { request.ItemCode, request.BarCode });
        }

        // Validate it's an inventory item
        if (!validationResult.IsInventoryItem) {
            throw new ApiErrorException((int)AddItemReturnValueType.NotStockItem, new { request.ItemCode, request.BarCode });
        }

        // Validate item exists in warehouse
        if (!validationResult.ItemExistsInWarehouse) {
            throw new ApiErrorException((int)AddItemReturnValueType.ItemNotInWarehouse, new { BinEntry = request.BinEntry ?? 0 });
        }

        // Validate bin existence if provided
        if (request.BinEntry.HasValue) {
            if (!validationResult.BinExists) {
                throw new ApiErrorException((int)AddItemReturnValueType.BinNotExists, new { BinEntry = request.BinEntry.Value });
            }

            if (!validationResult.BinBelongsToWarehouse) {
                throw new ApiErrorException((int)AddItemReturnValueType.BinNotInWarehouse, new { request.ItemCode, request.BarCode });
            }
        }

        // Calculate total quantity including unit conversion
        int totalQuantity = request.Quantity;
        if (request.Unit != UnitType.Unit) {
            totalQuantity *= validationResult.NumInBuy * (request.Unit == UnitType.Pack ? validationResult.PurPackUn : 1);
        }

        // Calculate existing quantities from database
        var existingQuantities = await db.TransferLines
            .Where(tl => tl.TransferId == request.ID &&
                         tl.ItemCode == request.ItemCode &&
                         tl.LineStatus != LineStatus.Closed)
            .GroupBy(tl => tl.Type)
            .Select(g => new {
                Type          = g.Key,
                TotalQuantity = g.Sum(tl => tl.Quantity)
            })
            .ToListAsync();

        int sourceQuantity = 0;
        int targetQuantity = 0;

        foreach (var eq in existingQuantities) {
            switch (eq.Type) {
                case SourceTarget.Source:
                    sourceQuantity = eq.TotalQuantity;
                    break;
                case SourceTarget.Target:
                    targetQuantity = eq.TotalQuantity;
                    break;
            }
        }

        if (!request.BinEntry.HasValue)
            return true;

        // If specific bin is requested, filter by bin
        var binSpecificQuantities = await db.TransferLines
            .Where(tl => tl.TransferId == request.ID &&
                         tl.ItemCode == request.ItemCode &&
                         tl.BinEntry == request.BinEntry.Value &&
                         tl.LineStatus != LineStatus.Closed)
            .GroupBy(tl => tl.Type)
            .Select(g => new {
                Type          = g.Key,
                TotalQuantity = g.Sum(tl => tl.Quantity)
            })
            .ToListAsync();

        int binSourceQuantity = binSpecificQuantities
            .Where(x => x.Type == SourceTarget.Source)
            .Select(x => x.TotalQuantity)
            .FirstOrDefault();

        switch (request.Type) {
            // Validate quantity availability
            case SourceTarget.Source: {
                decimal availableInBin = validationResult.AvailableQuantity - binSourceQuantity;
                if (availableInBin < totalQuantity) {
                    throw new ApiErrorException((int)AddItemReturnValueType.QuantityMoreAvailable, new { request.ItemCode });
                }

                break;
            }
            case SourceTarget.Target: {
                decimal availableToTransfer = sourceQuantity - targetQuantity;
                if (availableToTransfer < totalQuantity) {
                    throw new ApiErrorException((int)AddItemReturnValueType.QuantityMoreAvailable, new { request.ItemCode });
                }

                break;
            }
        }

        return true;
    }

    public async Task<UpdateLineResponse> UpdateLine(SessionInfo info, UpdateLineRequest request) {
        var response = new UpdateLineResponse();

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Validate transfer exists and is in valid state
            var transfer = await db.Transfers
                .Where(t => t.Id == request.ID)
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
                .Where(tl => tl.Id == request.LineID && tl.TransferId == request.ID)
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
            if (request.CloseReason.HasValue) {
                line.LineStatus   = LineStatus.Closed;
                line.StatusReason = request.CloseReason.Value;
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

    public async Task<UpdateLineResponse> UpdateLineQuantity(SessionInfo info, UpdateLineQuantityRequest request) {
        var response = new UpdateLineResponse();
        
        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Find the line to update
            var line = await db.TransferLines
                .Include(tl => tl.Transfer)
                .Where(tl => tl.Id == request.LineID && tl.TransferId == request.ID)
                .FirstOrDefaultAsync();
                
            if (line == null) {
                response.ReturnValue = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Transfer line not found";
                return response;
            }
            
            if (line.Transfer.Status != ObjectStatus.Open && line.Transfer.Status != ObjectStatus.InProgress) {
                response.ReturnValue = UpdateLineReturnValue.Status;
                response.ErrorMessage = "Transfer status is not Open or In Progress";
                return response;
            }
            
            if (line.LineStatus == LineStatus.Closed) {
                response.ReturnValue = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Line is already closed";
                return response;
            }
            
            // Calculate the new quantity based on unit type
            int newQuantity = request.Quantity;
            if (line.UnitType != UnitType.Unit) {
                var items = await adapter.ItemCheckAsync(line.ItemCode, null);
                var item = items.FirstOrDefault();
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
                    .Where(tl => tl.TransferId == request.ID &&
                                 tl.ItemCode == line.ItemCode &&
                                 tl.BinEntry == line.BinEntry.Value &&
                                 tl.Type == SourceTarget.Source &&
                                 tl.LineStatus != LineStatus.Closed &&
                                 tl.Id != line.Id)
                    .SumAsync(tl => tl.Quantity);
                
                decimal availableInBin = validationResult.AvailableQuantity - existingSourceQuantity;
                if (availableInBin < newQuantity) {
                    response.ReturnValue = UpdateLineReturnValue.QuantityMoreThenAvailable;
                    response.ErrorMessage = "Quantity more than available in bin";
                    return response;
                }
            }
            
            // Update the quantity
            line.Quantity = newQuantity;
            
            // Update modification tracking
            line.UpdatedAt = DateTime.UtcNow;
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