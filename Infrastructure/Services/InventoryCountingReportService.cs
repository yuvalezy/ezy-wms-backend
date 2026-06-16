using Core.DTOs.InventoryCounting;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class InventoryCountingReportService(
    SystemDbContext db,
    IExternalSystemAdapter adapter) : IInventoryCountingReportService {

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

        // Extract distinct item codes and bin entries for bulk fetching
        var distinctItemCodes = groupedLines.Select(g => g.Key.ItemCode).Distinct().ToArray();
        var distinctBinEntries = groupedLines
            .Where(g => g.Key.BinEntry.HasValue)
            .Select(g => g.Key.BinEntry!.Value)
            .Distinct()
            .ToArray();

        // Bulk fetch all data in parallel
        var itemCheckTask = adapter.BulkItemCheckAsync(distinctItemCodes);
        var binCheckTask = adapter.BulkBinCheckAsync(distinctBinEntries);
        await Task.WhenAll(itemCheckTask, binCheckTask);

        var itemsLookup = await itemCheckTask;
        var binContentsLookup = await binCheckTask;

        // Fetch ItemBinStock for groups without bins
        var whsCode = lines.FirstOrDefault()?.InventoryCounting.WhsCode ?? "";
        var noBinItemCodes = groupedLines
            .Where(g => !g.Key.BinEntry.HasValue)
            .Select(g => g.Key.ItemCode)
            .Distinct()
            .ToList();

        var itemBinStockLookup = new Dictionary<string, decimal>();
        foreach (var itemCode in noBinItemCodes) {
            var stocks = await adapter.ItemBinStockAsync(itemCode, whsCode);
            itemBinStockLookup[itemCode] = stocks.Sum(s => s.Quantity);
        }

        // Build results using pre-fetched data
        foreach (var group in groupedLines) {
            decimal totalCountedQuantity = group.Sum(l => l.Quantity);
            decimal systemQuantity = 0;
            string binCode = "";

            if (!itemsLookup.TryGetValue(group.Key.ItemCode, out var item)) {
                throw new Exception($"Item {group.Key.ItemCode} not found.");
            }

            if (group.Key.BinEntry.HasValue) {
                if (binContentsLookup.TryGetValue(group.Key.BinEntry.Value, out var binContents)) {
                    var binContent = binContents.FirstOrDefault(bc => bc.ItemCode == group.Key.ItemCode);
                    if (binContent != null) {
                        systemQuantity = (decimal)binContent.OnHand;
                    }
                }

                binCode = group.Key.BinEntry.Value.ToString();
            }
            else {
                systemQuantity = itemBinStockLookup.GetValueOrDefault(group.Key.ItemCode);
            }

            decimal variance = totalCountedQuantity - systemQuantity;

            result.Add(new InventoryCountingContentResponse {
                ItemCode = group.Key.ItemCode,
                ItemName = item.ItemName,
                BuyUnitMsr = item.BuyUnitMsr,
                NumInBuy = item.NumInBuy,
                PurPackMsr = item.PurPackMsr,
                PurPackUn = item.PurPackUn,
                Factor1 = item.Factor1,
                Factor2 = item.Factor2,
                Factor3 = item.Factor3,
                Factor4 = item.Factor4,
                CustomFields = item.CustomFields,
                BinEntry = group.Key.BinEntry,
                BinCode = binCode,
                SystemQuantity = systemQuantity,
                CountedQuantity = totalCountedQuantity,
                Variance = variance,
                SystemValue = 0,
                CountedValue = 0,
                VarianceValue = 0
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

        var lines = counting.Lines;
        int totalLines = lines.Count;
        int processedLines = lines.Count(l => l.LineStatus is LineStatus.Closed or LineStatus.Finished);

        // Group lines by ItemCode and BinEntry to calculate variances
        var groupedLines = lines
        .GroupBy(l => new { l.ItemCode, l.BinEntry })
        .ToList();

        // Extract distinct item codes and bin entries for bulk fetching
        var distinctItemCodes = groupedLines.Select(g => g.Key.ItemCode).Distinct().ToArray();
        var distinctBinEntries = groupedLines
            .Where(g => g.Key.BinEntry.HasValue)
            .Select(g => g.Key.BinEntry!.Value)
            .Distinct()
            .ToArray();

        // Bulk fetch all data in parallel (3-4 queries total instead of 3N-4N)
        var itemCheckTask = adapter.BulkItemCheckAsync(distinctItemCodes);
        var binCheckTask = adapter.BulkBinCheckAsync(distinctBinEntries);
        var binCodesTask = adapter.BulkGetBinCodesAsync(distinctBinEntries);

        await Task.WhenAll(itemCheckTask, binCheckTask, binCodesTask);

        var itemsLookup = await itemCheckTask;
        var binContentsLookup = await binCheckTask;
        var binCodesLookup = await binCodesTask;

        // Fetch ItemBinStock for groups without bins (one query per unique itemCode without bin)
        var noBinItemCodes = groupedLines
            .Where(g => !g.Key.BinEntry.HasValue)
            .Select(g => g.Key.ItemCode)
            .Distinct()
            .ToList();

        var itemBinStockLookup = new Dictionary<string, decimal>();
        foreach (var itemCode in noBinItemCodes) {
            var stocks = await adapter.ItemBinStockAsync(itemCode, counting.WhsCode);
            itemBinStockLookup[itemCode] = stocks.Sum(s => s.Quantity);
        }

        // Build report lines using pre-fetched data (pure in-memory, no awaits)
        int varianceLines = 0;
        decimal totalSystemValue = 0m;
        decimal totalCountedValue = 0m;
        var reportLines = new List<InventoryCountingReportLine>();

        foreach (var group in groupedLines) {
            decimal totalCountedQuantity = group.Sum(l => l.Quantity);
            decimal systemQuantity = 0;
            string binCode = "";

            itemsLookup.TryGetValue(group.Key.ItemCode, out var item);
            string itemName = item?.ItemName ?? "";

            if (group.Key.BinEntry.HasValue) {
                var binEntry = group.Key.BinEntry.Value;
                if (binContentsLookup.TryGetValue(binEntry, out var binContents)) {
                    var binContent = binContents.FirstOrDefault(bc => bc.ItemCode == group.Key.ItemCode);
                    systemQuantity = binContent != null ? (decimal)binContent.OnHand : 0;
                }

                binCode = binCodesLookup.TryGetValue(binEntry, out var code) ? code : binEntry.ToString();
            }
            else {
                systemQuantity = itemBinStockLookup.GetValueOrDefault(group.Key.ItemCode);
                binCode = "No Bin";
            }

            decimal variance = totalCountedQuantity - systemQuantity;
            if (variance != 0) {
                varianceLines++;
            }

            reportLines.Add(new InventoryCountingReportLine {
                ItemCode = group.Key.ItemCode,
                ItemName = itemName,
                BinCode = binCode,
                BinEntry = group.Key.BinEntry,
                Quantity = totalCountedQuantity,
                BuyUnitMsr = item?.BuyUnitMsr,
                NumInBuy = item?.NumInBuy ?? 1,
                PurPackMsr = item?.PurPackMsr,
                PurPackUn = item?.PurPackUn ?? 1,
                CustomFields = item?.CustomFields,
            });
        }

        return new InventoryCountingSummaryResponse {
            CountingId = counting.Id,
            Number = counting.Number,
            Name = counting.Name,
            Date = counting.Date,
            WhsCode = counting.WhsCode,
            TotalLines = totalLines,
            ProcessedLines = processedLines,
            VarianceLines = varianceLines,
            TotalSystemValue = totalSystemValue,
            TotalCountedValue = totalCountedValue,
            TotalVarianceValue = totalSystemValue - totalCountedValue,
            Lines = reportLines
        };
    }

    public async Task<IEnumerable<InventoryCountingReportAllDetailsResponse>> GetCountingReportAllDetails(Guid id, string itemCode, int? binEntry) {
        var values = await db.InventoryCountingLines
            .Include(l => l.CreatedByUser)
            .Where(l => l.InventoryCountingId == id && l.ItemCode == itemCode && l.LineStatus != LineStatus.Closed)
            .Where(l => binEntry == null ? !l.BinEntry.HasValue : l.BinEntry == binEntry)
            .Select(l => new InventoryCountingReportAllDetailsResponse {
                LineId = l.Id,
                CreatedByUserName = l.CreatedByUser!.FullName,
                TimeStamp = l.UpdatedAt ?? l.CreatedAt,
                Quantity = l.Quantity,
                Unit = l.Unit,
            })
            .ToListAsync();

        return values;
    }

    public async Task<string?> UpdateCountingAll(UpdateInventoryCountingAllRequest request, SessionInfo sessionInfo) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Validate counting exists and is in valid state
            var counting = await db.InventoryCountings.FindAsync(request.Id);
            if (counting == null)
                return "Inventory counting not found";

            if (counting.Status != ObjectStatus.Open && counting.Status != ObjectStatus.InProgress)
                return "Counting status is not Open or In Progress";

            // Remove rows
            if (request.RemoveRows.Length > 0) {
                var linesToRemove = await db.InventoryCountingLines
                    .Where(l => request.RemoveRows.Contains(l.Id) && l.InventoryCountingId == request.Id)
                    .ToArrayAsync();

                foreach (var line in linesToRemove) {
                    line.UpdatedAt = DateTime.UtcNow;
                    line.UpdatedByUserId = sessionInfo.Guid;
                    line.LineStatus = LineStatus.Closed;
                    line.Deleted = true;
                    line.DeletedAt = DateTime.UtcNow;
                    db.InventoryCountingLines.Update(line);
                }
            }

            // Update quantities
            foreach (var pair in request.QuantityChanges) {
                var lineId = pair.Key;
                decimal newDisplayQuantity = pair.Value;

                var line = await db.InventoryCountingLines
                    .FirstOrDefaultAsync(l => l.Id == lineId && l.InventoryCountingId == request.Id);

                if (line == null)
                    continue;

                // Convert display quantity back to base quantity
                decimal newQuantity = newDisplayQuantity;
                var items = await adapter.ItemCheckAsync(line.ItemCode, null);
                var item = items.FirstOrDefault();
                if (line.Unit != UnitType.Unit && item != null) {
                    newQuantity *= item.NumInBuy;
                    if (line.Unit == UnitType.Pack) {
                        newQuantity *= item.PurPackUn;
                    }
                }

                line.Quantity = newQuantity;
                line.UpdatedAt = DateTime.UtcNow;
                line.UpdatedByUserId = sessionInfo.Guid;

                db.InventoryCountingLines.Update(line);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return null;
        }
        catch (Exception e) {
            await transaction.RollbackAsync();
            return e.Message;
        }
    }
}
