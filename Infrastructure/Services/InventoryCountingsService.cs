using Core.DTOs;
using Core.DTOs.InventoryCounting;
using Core.DTOs.Items;
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
            Name            = request.Name,
            Date            = DateTime.UtcNow.Date,
            Status          = ObjectStatus.Open,
            WhsCode         = sessionInfo.Warehouse,
            CreatedByUserId = sessionInfo.Guid,
            Lines           = new List<InventoryCountingLine>()
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
                int newQuantity = request.Quantity.Value;
                if (line.Unit != UnitType.Unit) {
                    var items = await adapter.ItemCheckAsync(line.ItemCode, null);
                    var item  = items.FirstOrDefault();
                    if (item != null) {
                        newQuantity *= item.NumInBuy;
                        if (line.Unit == UnitType.Pack) {
                            newQuantity *= item.PurPackUn;
                        }
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

    public async Task<bool> CancelCounting(Guid id, SessionInfo sessionInfo) {
        var counting = await db.InventoryCountings.FindAsync(id);
        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {id} not found.");
        }

        if (counting.Status != ObjectStatus.Open && counting.Status != ObjectStatus.InProgress) {
            throw new InvalidOperationException("Cannot cancel counting if the Status is not Open or In Progress");
        }

        counting.Status          = ObjectStatus.Cancelled;
        counting.UpdatedAt       = DateTime.UtcNow;
        counting.UpdatedByUserId = sessionInfo.Guid;

        db.Update(counting);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<ProcessInventoryCountingResponse> ProcessCounting(Guid id, SessionInfo sessionInfo) {
        var transaction = await db.Database.BeginTransactionAsync();
        try {
            var counting = await db.InventoryCountings
                .Include(ic => ic.Lines.Where(l => l.LineStatus != LineStatus.Closed))
                .FirstOrDefaultAsync(ic => ic.Id == id);
                
            if (counting == null) {
                throw new KeyNotFoundException($"Inventory counting with ID {id} not found.");
            }

            if (counting.Status != ObjectStatus.InProgress) {
                throw new InvalidOperationException("Cannot process counting if the Status is not In Progress");
            }

            // Update status to Processing
            counting.Status          = ObjectStatus.Processing;
            counting.UpdatedAt       = DateTime.UtcNow;
            counting.UpdatedByUserId = sessionInfo.Guid;
            await db.SaveChangesAsync();

            // Prepare data for SAP B1 inventory counting creation
            var countingData = await PrepareCountingData(id, counting.WhsCode);

            // Call external system to process the counting
            var result = await adapter.ProcessInventoryCounting(counting.Number, sessionInfo.Warehouse, countingData);

            if (result.Success) {
                // Update status to Closed
                counting.Status          = ObjectStatus.Closed;
                counting.UpdatedAt       = DateTime.UtcNow;
                counting.UpdatedByUserId = sessionInfo.Guid;

                // Update all open lines to Closed
                var openLines = await db.InventoryCountingLines
                    .Where(icl => icl.InventoryCountingId == id && icl.LineStatus != LineStatus.Closed)
                    .ToListAsync();

                foreach (var line in openLines) {
                    line.LineStatus      = LineStatus.Closed;
                    line.UpdatedAt       = DateTime.UtcNow;
                    line.UpdatedByUserId = sessionInfo.Guid;
                }

                await db.SaveChangesAsync();
            }
            else {
                throw new InvalidOperationException(result.ErrorMessage ?? "Unknown error");
            }

            await transaction.CommitAsync();
            return result;
        }
        catch (Exception) {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<Dictionary<string, InventoryCountingCreationDataResponse>> PrepareCountingData(Guid countingId, string warehouse) {
        var lines = await db.InventoryCountingLines
            .Where(icl => icl.InventoryCountingId == countingId && icl.LineStatus != LineStatus.Closed)
            .GroupBy(icl => icl.ItemCode)
            .Select(g => new {
                ItemCode = g.Key,
                Lines    = g.ToList()
            })
            .ToListAsync();

        var countingData = new Dictionary<string, InventoryCountingCreationDataResponse>();

        foreach (var itemGroup in lines) {
            int totalCountedQuantity = 0;
            int systemQuantity = 0;
            var countedBins = new List<InventoryCountingCreationBinResponse>();

            // Group by bins
            var binGroups = itemGroup.Lines
                .Where(l => l.BinEntry.HasValue)
                .GroupBy(l => l.BinEntry.Value)
                .ToList();

            foreach (var binGroup in binGroups) {
                int binCountedQuantity = binGroup.Sum(l => l.Quantity);
                int binSystemQuantity = 0;

                try {
                    // Get system quantity for this bin
                    var binContents = await adapter.BinCheckAsync(binGroup.Key);
                    var binContent = binContents.FirstOrDefault(bc => bc.ItemCode == itemGroup.ItemCode);
                    binSystemQuantity = binContent != null ? (int)binContent.OnHand : 0;
                }
                catch {
                    // If external adapter fails, continue with zero system quantity
                }

                countedBins.Add(new InventoryCountingCreationBinResponse {
                    BinEntry = binGroup.Key,
                    CountedQuantity = binCountedQuantity,
                    SystemQuantity = binSystemQuantity
                });

                totalCountedQuantity += binCountedQuantity;
                systemQuantity += binSystemQuantity;
            }

            // Handle lines without bins
            var noBinLines = itemGroup.Lines.Where(l => !l.BinEntry.HasValue).ToList();
            if (noBinLines.Any()) {
                int noBinCountedQuantity = noBinLines.Sum(l => l.Quantity);
                totalCountedQuantity += noBinCountedQuantity;

                try {
                    // Get warehouse stock for items without bins
                    var stocks = await adapter.ItemStockAsync(itemGroup.ItemCode, warehouse);
                    int warehouseSystemQuantity = stocks.Sum(s => s.Quantity);
                    // Subtract quantities already counted in bins
                    systemQuantity += Math.Max(0, warehouseSystemQuantity - systemQuantity);
                }
                catch {
                    // If external adapter fails, continue with existing system quantity
                }
            }

            int variance = totalCountedQuantity - systemQuantity;

            var data = new InventoryCountingCreationDataResponse {
                ItemCode = itemGroup.ItemCode,
                CountedQuantity = totalCountedQuantity,
                SystemQuantity = systemQuantity,
                Variance = variance,
                CountedBins = countedBins
            };

            countingData[itemGroup.ItemCode] = data;
        }

        return countingData;
    }

    public async Task<IEnumerable<InventoryCountingContentResponse>> GetCountingContent(InventoryCountingContentRequest request) {
        var query = db.InventoryCountingLines
            .Include(icl => icl.InventoryCounting)
            .Where(icl => icl.InventoryCountingId == request.ID);

        if (request.BinEntry.HasValue) {
            query = query.Where(icl => icl.BinEntry == request.BinEntry.Value);
        }

        var lines = await query.ToListAsync();

        var result = new List<InventoryCountingContentResponse>();

        // Group by ItemCode and BinEntry to aggregate quantities
        var groupedLines = lines
            .GroupBy(l => new { l.ItemCode, l.BinEntry })
            .ToList();

        foreach (var group in groupedLines) {
            var firstLine            = group.First();
            int totalCountedQuantity = group.Sum(l => l.Quantity);

            // Get system quantity from SAP B1 using external adapter
            int    systemQuantity = 0;
            string binCode        = "";

            ItemCheckResponse? item;
            try {
                // Get item information
                var itemInfo = await adapter.ItemCheckAsync(group.Key.ItemCode, null);
                item = itemInfo.FirstOrDefault();
                if (item == null) {
                    throw new Exception($"Item {group.Key.ItemCode} not found.");
                }

                // Get bin information and system quantity
                if (group.Key.BinEntry.HasValue) {
                    var binContents = await adapter.BinCheckAsync(group.Key.BinEntry.Value);
                    var binContent  = binContents.FirstOrDefault(bc => bc.ItemCode == group.Key.ItemCode);
                    systemQuantity = 0;
                    if (binContent != null) {
                        systemQuantity = (int)binContent.OnHand;
                    }

                    binCode = group.Key.BinEntry.Value.ToString(); // TODO: We could enhance this to get actual bin code
                }
                else {
                    // Get stock from warehouse if no specific bin
                    var stocks = await adapter.ItemStockAsync(group.Key.ItemCode, firstLine.InventoryCounting.WhsCode);
                    systemQuantity = stocks.Sum(s => s.Quantity);
                }
            }
            catch (Exception ex) {
                // If external adapter fails, continue with zero system quantity
                throw new Exception($"Failed to get item information for {group.Key.ItemCode} and bin {group.Key.BinEntry}.");
            }

            int variance = totalCountedQuantity - systemQuantity;

            result.Add(new InventoryCountingContentResponse {
                ItemCode        = group.Key.ItemCode,
                ItemName        = item.ItemName,
                BuyUnitMsr      = item.BuyUnitMsr,
                NumInBuy        = item.NumInBuy,
                PurPackMsr      = item.PurPackMsr,
                PurPackUn       = item.PurPackUn,
                BinEntry        = group.Key.BinEntry,
                BinCode         = binCode,
                SystemQuantity  = systemQuantity,
                CountedQuantity = totalCountedQuantity,
                Variance        = variance,
                SystemValue     = 0,
                CountedValue    = 0,
                VarianceValue   = 0 
            });
        }

        return result.OrderBy(r => r.ItemCode).ThenBy(r => r.BinEntry);
    }

    public async Task<InventoryCountingSummaryResponse> GetCountingSummaryReport(Guid id) {
        var counting = await db.InventoryCountings
            .Include(ic => ic.Lines)
            .FirstOrDefaultAsync(ic => ic.Id == id);

        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {id} not found.");
        }

        var lines          = counting.Lines ?? new List<InventoryCountingLine>();
        int totalLines     = lines.Count;
        int processedLines = lines.Count(l => l.LineStatus == LineStatus.Closed || l.LineStatus == LineStatus.Finished);

        // Calculate variance lines and values by comparing with system quantities
        int     varianceLines     = 0;
        decimal totalSystemValue  = 0m;
        decimal totalCountedValue = 0m;
        var     reportLines       = new List<InventoryCountingReportLine>();

        // Group lines by ItemCode and BinEntry to calculate variances
        var groupedLines = lines
            .GroupBy(l => new { l.ItemCode, l.BinEntry })
            .ToList();

        foreach (var group in groupedLines) {
            int                totalCountedQuantity = group.Sum(l => l.Quantity);
            int                systemQuantity       = 0;
            string             itemName             = "";
            string             binCode              = "";
            ItemCheckResponse? item                 = null;

            try {
                // Get item information
                var itemInfo = await adapter.ItemCheckAsync(group.Key.ItemCode, null);
                item     = itemInfo.FirstOrDefault();
                itemName = item?.ItemName ?? "";

                // Get system quantity from SAP B1 using external adapter
                if (group.Key.BinEntry.HasValue) {
                    var binContents = await adapter.BinCheckAsync(group.Key.BinEntry.Value);
                    var binContent  = binContents.FirstOrDefault(bc => bc.ItemCode == group.Key.ItemCode);
                    systemQuantity = binContent != null ? (int)binContent.OnHand : 0;

                    // Get the actual bin code
                    binCode = await adapter.GetBinCodeAsync(group.Key.BinEntry.Value) ?? group.Key.BinEntry.Value.ToString();
                }
                else {
                    // Get stock from warehouse if no specific bin
                    var stocks = await adapter.ItemStockAsync(group.Key.ItemCode, counting.WhsCode);
                    systemQuantity = stocks.Sum(s => s.Quantity);
                    binCode        = "No Bin";
                }
            }
            catch {
                // If external adapter fails, continue with zero system quantity
                throw new Exception($"Failed to get item information for {group.Key.ItemCode} and bin {group.Key.BinEntry}.");
            }

            int variance = totalCountedQuantity - systemQuantity;
            if (variance != 0) {
                varianceLines++;
            }

            // Add report line
            reportLines.Add(new InventoryCountingReportLine {
                ItemCode   = group.Key.ItemCode,
                ItemName   = itemName,
                BinCode    = binCode,
                Quantity   = totalCountedQuantity,
                BuyUnitMsr = item?.BuyUnitMsr,
                NumInBuy   = item?.NumInBuy ?? 1,
                PurPackMsr = item?.PurPackMsr,
                PurPackUn  = item?.PurPackUn ?? 1
            });

            // Note: For value calculations, we would need price information from SAP B1
            // For now, we'll leave values as 0 since price retrieval would require additional adapter calls
        }

        return new InventoryCountingSummaryResponse {
            CountingId         = counting.Id,
            Number             = counting.Number,
            Name               = counting.Name,
            Date               = counting.Date,
            WhsCode            = counting.WhsCode,
            TotalLines         = totalLines,
            ProcessedLines     = processedLines,
            VarianceLines      = varianceLines,
            TotalSystemValue   = totalSystemValue,
            TotalCountedValue  = totalCountedValue,
            TotalVarianceValue = totalSystemValue - totalCountedValue,
            Lines              = reportLines
        };
    }
}