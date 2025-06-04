using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class InventoryCountingsService(SystemDbContext db, IExternalSystemAdapter adapter) : IInventoryCountingsService {
    public async Task<InventoryCountingResponse> CreateCounting(CreateInventoryCountingRequest request, SessionInfo sessionInfo) {
        var counting = new InventoryCounting {
            Name = request.Name,
            Date = DateTime.UtcNow.Date,
            Status = ObjectStatus.Open,
            WhsCode = sessionInfo.Warehouse,
            CreatedByUserId = sessionInfo.Guid,
            Lines = new List<InventoryCountingLine>()
        };
        
        await db.InventoryCountings.AddAsync(counting);
        await db.SaveChangesAsync();
        
        return InventoryCountingResponse.FromEntity(counting);
    }
    
    public async Task<IEnumerable<InventoryCountingResponse>> GetCountings(InventoryCountingsRequest request, string warehouse) {
        var query = db.InventoryCountings
            .Include(ic => ic.CreatedByUser)
            .Include(ic => ic.UpdatedByUser)
            .Where(ic => ic.WhsCode == warehouse)
            .AsQueryable();
        
        if (request.ID.HasValue) {
            query = query.Where(ic => ic.Number == request.ID.Value);
        }
        
        if (request.Date.HasValue) {
            var targetDate = request.Date.Value.Date;
            query = query.Where(ic => ic.Date.Date == targetDate);
        }
        
        if (request.Statuses?.Length > 0) {
            query = query.Where(ic => request.Statuses.Contains(ic.Status));
        }
        
        var countings = await query.OrderByDescending(ic => ic.Number).ToListAsync();
        
        return countings.Select(InventoryCountingResponse.FromEntity);
    }
    
    public async Task<InventoryCountingResponse> GetCounting(Guid id) {
        var counting = await db.InventoryCountings
            .Include(ic => ic.Lines)
            .Include(ic => ic.CreatedByUser)
            .Include(ic => ic.UpdatedByUser)
            .FirstOrDefaultAsync(ic => ic.Id == id);
        
        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {id} not found.");
        }
        
        return InventoryCountingResponse.FromEntity(counting);
    }
    
    public async Task<InventoryCountingAddItemResponse> AddItem(SessionInfo sessionInfo, InventoryCountingAddItemRequest request) {
        // Validate the counting exists and is in a valid state
        var counting = await db.InventoryCountings.FindAsync(request.ID);
        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {request.ID} not found.");
        }
        
        if (counting.Status != ObjectStatus.Open && counting.Status != ObjectStatus.InProgress) {
            return new InventoryCountingAddItemResponse {
                Status = ResponseStatus.Error,
                ErrorMessage = "Counting must be Open or In Progress to add items",
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
        int totalQuantity = request.Quantity;
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
                ItemCode = request.ItemCode,
                BarCode = request.BarCode,
                Quantity = totalQuantity,
                BinEntry = request.BinEntry,
                Unit = request.Unit,
                Date = DateTime.UtcNow,
                LineStatus = LineStatus.Open,
                CreatedByUserId = sessionInfo.Guid
            };
            
            await db.InventoryCountingLines.AddAsync(line);
            
            // Update counting status if it was Open
            if (counting.Status == ObjectStatus.Open) {
                counting.Status = ObjectStatus.InProgress;
                counting.UpdatedAt = DateTime.UtcNow;
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
                .Where(ic => ic.Id == request.ID)
                .Select(ic => new { ic.Status })
                .FirstOrDefaultAsync();
                
            if (counting == null) {
                response.ReturnValue = UpdateLineReturnValue.Status;
                response.ErrorMessage = "Inventory counting not found";
                return response;
            }
            
            if (counting.Status != ObjectStatus.Open && counting.Status != ObjectStatus.InProgress) {
                response.ReturnValue = UpdateLineReturnValue.Status;
                response.ErrorMessage = "Counting status is not Open or In Progress";
                return response;
            }
            
            // Find the line to update
            var line = await db.InventoryCountingLines
                .Where(icl => icl.Id == request.LineID && icl.InventoryCountingId == request.ID)
                .FirstOrDefaultAsync();
                
            if (line == null) {
                response.ReturnValue = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Counting line not found";
                return response;
            }
            
            if (line.LineStatus == LineStatus.Closed) {
                response.ReturnValue = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Line is already closed";
                return response;
            }
            
            // Update comments if provided
            if (request.Comment != null) {
                line.Comments = request.Comment;
            }
            
            // Update quantity if provided
            if (request.Quantity.HasValue) {
                line.Quantity = request.Quantity.Value;
            }
            
            // Handle line closure
            if (request.CancellationReasonId.HasValue) {
                // Validate the cancellation reason exists and is enabled
                var cancellationReason = await db.CancellationReasons
                    .Where(cr => cr.Id == request.CancellationReasonId.Value && cr.IsEnabled && cr.Counting)
                    .FirstOrDefaultAsync();
                    
                if (cancellationReason == null) {
                    response.ReturnValue = UpdateLineReturnValue.CloseReason;
                    response.ErrorMessage = "Invalid or disabled cancellation reason for counting";
                    return response;
                }
                
                line.LineStatus = LineStatus.Closed;
                line.CancellationReasonId = request.CancellationReasonId.Value;
            }
            
            // Update modification tracking
            line.UpdatedAt = DateTime.UtcNow;
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
    
    public async Task<bool> CancelCounting(Guid id, SessionInfo sessionInfo) {
        var counting = await db.InventoryCountings.FindAsync(id);
        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {id} not found.");
        }
        
        if (counting.Status != ObjectStatus.Open && counting.Status != ObjectStatus.InProgress) {
            throw new InvalidOperationException("Cannot cancel counting if the Status is not Open or In Progress");
        }
        
        counting.Status = ObjectStatus.Cancelled;
        counting.UpdatedAt = DateTime.UtcNow;
        counting.UpdatedByUserId = sessionInfo.Guid;
        
        db.Update(counting);
        await db.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<bool> ProcessCounting(Guid id, SessionInfo sessionInfo) {
        var counting = await db.InventoryCountings.FindAsync(id);
        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {id} not found.");
        }
        
        if (counting.Status != ObjectStatus.InProgress) {
            throw new InvalidOperationException("Cannot process counting if the Status is not In Progress");
        }
        
        // Update status to Processing
        counting.Status = ObjectStatus.Processing;
        counting.UpdatedAt = DateTime.UtcNow;
        counting.UpdatedByUserId = sessionInfo.Guid;
        db.Update(counting);
        await db.SaveChangesAsync();
        
        try {
            // Call external system to process the counting
            await adapter.ProcessInventoryCounting(counting.Number, sessionInfo.Warehouse);
            
            // Update status to Finished
            counting.Status = ObjectStatus.Closed;
            counting.UpdatedAt = DateTime.UtcNow;
            counting.UpdatedByUserId = sessionInfo.Guid;
            db.Update(counting);
            await db.SaveChangesAsync();
            
            return true;
        }
        catch (Exception) {
            // Revert status back to InProgress on error
            counting.Status = ObjectStatus.InProgress;
            counting.UpdatedAt = DateTime.UtcNow;
            counting.UpdatedByUserId = sessionInfo.Guid;
            db.Update(counting);
            await db.SaveChangesAsync();
            throw;
        }
    }
    
    public async Task<IEnumerable<InventoryCountingContentResponse>> GetCountingContent(InventoryCountingContentRequest request) {
        return await adapter.GetInventoryCountingContent(request.ID, request.BinEntry);
    }
    
    public async Task<InventoryCountingSummaryResponse> GetCountingSummaryReport(Guid id) {
        return await adapter.GetInventoryCountingSummary(id);
    }
}