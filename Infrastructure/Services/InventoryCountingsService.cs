using Core.DTOs.InventoryCounting;
using Core.DTOs.Items;
using Core.DTOs.Package;
using Core.Entities;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class InventoryCountingsService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ISettings settings,
    IPackageContentService packageContentService,
    IPackageService packageService,
    IExternalCommandService externalCommandService,
    IPackageLocationService packageLocationService,
    IExternalSystemAlertService alertService) : IInventoryCountingsService {
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
            counting.Status = ObjectStatus.Processing;
            counting.UpdatedAt = DateTime.UtcNow;
            counting.UpdatedByUserId = sessionInfo.Guid;
            await db.SaveChangesAsync();

            // Prepare data for SAP B1 inventory counting creation
            var countingData = await PrepareCountingData(id, counting.WhsCode, sessionInfo.EnableBinLocations);

            // Get alert recipients
            var alertRecipients = await alertService.GetAlertRecipientsAsync(AlertableObjectType.InventoryCounting);

            // Call external system to process the counting
            var result = await adapter.ProcessInventoryCounting(counting.Number, sessionInfo.Warehouse, countingData, alertRecipients);

            if (result.Success) {
                // Update status to Closed
                counting.Status = ObjectStatus.Closed;
                counting.UpdatedAt = DateTime.UtcNow;
                counting.UpdatedByUserId = sessionInfo.Guid;

                // Update all open lines to Closed
                var openLines = await db.InventoryCountingLines
                .Where(icl => icl.InventoryCountingId == id && icl.LineStatus != LineStatus.Closed)
                .ToListAsync();

                foreach (var line in openLines) {
                    line.LineStatus = LineStatus.Closed;
                    line.UpdatedAt = DateTime.UtcNow;
                    line.UpdatedByUserId = sessionInfo.Guid;
                }

                // Process packages based on counting results
                await ProcessCountingPackages(id, sessionInfo);

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

    private async Task<Dictionary<string, InventoryCountingCreationDataResponse>> PrepareCountingData(Guid countingId, string warehouse, bool enableBinLocation) {
        var lines = await db.InventoryCountingLines
        .Where(icl => icl.InventoryCountingId == countingId && icl.LineStatus != LineStatus.Closed)
        .GroupBy(icl => icl.ItemCode)
        .Select(g => new {
            ItemCode = g.Key,
            Lines = g.ToList()
        })
        .ToListAsync();

        var countingData = new Dictionary<string, InventoryCountingCreationDataResponse>();
        int? initialCountingBinEntry = enableBinLocation ? settings.GetInitialCountingBinEntry(warehouse) : null;

        // Collect all distinct bin entries across all item groups for bulk fetching
        var allBinEntries = lines
            .SelectMany(ig => ig.Lines)
            .Where(l => l.BinEntry.HasValue)
            .Select(l => l.BinEntry!.Value)
            .Distinct()
            .ToArray();

        // Bulk fetch bin contents in a single query
        var binContentsLookup = await adapter.BulkBinCheckAsync(allBinEntries);

        // Collect items that need ItemBinStock: items with no-bin lines OR items needing initialCountingBinEntry
        var itemsNeedingBinStock = new HashSet<string>();
        foreach (var itemGroup in lines) {
            bool hasNoBinLines = itemGroup.Lines.Any(l => !l.BinEntry.HasValue);
            if (hasNoBinLines) {
                itemsNeedingBinStock.Add(itemGroup.ItemCode);
                continue;
            }

            if (initialCountingBinEntry.HasValue) {
                bool hasSystemBinEntry = itemGroup.Lines
                    .Where(l => l.BinEntry.HasValue)
                    .Any(l => l.BinEntry!.Value == initialCountingBinEntry.Value);
                if (!hasSystemBinEntry) {
                    itemsNeedingBinStock.Add(itemGroup.ItemCode);
                }
            }
        }

        // Fetch ItemBinStock for all items that need it (one query per unique item)
        var itemBinStockLookup = new Dictionary<string, IEnumerable<ItemBinStockResponse>>();
        foreach (var itemCode in itemsNeedingBinStock) {
            try {
                itemBinStockLookup[itemCode] = await adapter.ItemBinStockAsync(itemCode, warehouse);
            }
            catch {
                itemBinStockLookup[itemCode] = Enumerable.Empty<ItemBinStockResponse>();
            }
        }

        // Build counting data using pre-fetched lookups
        foreach (var itemGroup in lines) {
            decimal totalCountedQuantity = 0;
            decimal systemQuantity = 0;
            var countedBins = new List<InventoryCountingCreationBinResponse>();

            // Group by bins
            var binGroups = itemGroup.Lines
            .Where(l => l.BinEntry.HasValue)
            .GroupBy(l => l.BinEntry.Value)
            .ToList();

            foreach (var binGroup in binGroups) {
                decimal binCountedQuantity = binGroup.Sum(l => l.Quantity);
                decimal binSystemQuantity = 0;

                try {
                    if (binContentsLookup.TryGetValue(binGroup.Key, out var binContents)) {
                        var binContent = binContents.FirstOrDefault(bc => bc.ItemCode == itemGroup.ItemCode);
                        binSystemQuantity = binContent != null ? (decimal)binContent.OnHand : 0;
                    }
                }
                catch {
                    // If lookup fails, continue with zero system quantity
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
                decimal noBinCountedQuantity = noBinLines.Sum(l => l.Quantity);
                totalCountedQuantity += noBinCountedQuantity;

                if (itemBinStockLookup.TryGetValue(itemGroup.ItemCode, out var stocks)) {
                    decimal warehouseSystemQuantity = stocks.Sum(s => s.Quantity);
                    systemQuantity += Math.Max(0, warehouseSystemQuantity - systemQuantity);
                }
            }

            // Handle Initial Counting Bin Entry logic
            if (initialCountingBinEntry.HasValue) {
                bool hasSystemBinEntry = binGroups.Any(bg => bg.Key == initialCountingBinEntry.Value);

                if (!hasSystemBinEntry) {
                    decimal systemBinStock = 0;
                    if (itemBinStockLookup.TryGetValue(itemGroup.ItemCode, out var systemBinStocks)) {
                        var systemBinStockResponse = systemBinStocks.FirstOrDefault(s => s.BinEntry == initialCountingBinEntry.Value);
                        systemBinStock = systemBinStockResponse?.Quantity ?? 0;
                    }

                    decimal systemBinCountedQuantity = Math.Max(0, systemBinStock - totalCountedQuantity);

                    countedBins.Add(new InventoryCountingCreationBinResponse {
                        BinEntry = initialCountingBinEntry.Value,
                        CountedQuantity = systemBinCountedQuantity,
                        SystemQuantity = systemBinStock
                    });

                    totalCountedQuantity += systemBinCountedQuantity;
                    systemQuantity += systemBinStock;
                }
            }

            decimal variance = totalCountedQuantity - systemQuantity;

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


    private async Task ProcessCountingPackages(Guid countingId, SessionInfo sessionInfo) {
        var countingPackages = await db.InventoryCountingPackages
        .Include(cp => cp.Contents)
        .Include(cp => cp.Package)
        .ThenInclude(p => p.Contents)
        .Where(cp => cp.InventoryCountingId == countingId)
        .ToListAsync();

        foreach (var countingPackage in countingPackages) {
            var package = countingPackage.Package;

            // Handle package location changes
            if (countingPackage.CountedWhsCode != countingPackage.OriginalWhsCode ||
                countingPackage.CountedBinEntry != countingPackage.OriginalBinEntry) {
                await packageLocationService.LogLocationMovementAsync(
                    package.Id,
                    PackageMovementType.Moved,
                    countingPackage.OriginalWhsCode,
                    countingPackage.OriginalBinEntry,
                    countingPackage.CountedWhsCode,
                    countingPackage.CountedBinEntry,
                    ObjectType.InventoryCounting,
                    countingId,
                    sessionInfo.Guid
                );

                // Update package location
                package.WhsCode = countingPackage.CountedWhsCode;
                package.BinEntry = countingPackage.CountedBinEntry;
                package.UpdatedAt = DateTime.UtcNow;
                package.UpdatedByUserId = sessionInfo.Guid;
            }

            // Process package contents
            var itemsToProcess = new Dictionary<string, (decimal counted, decimal? original)>();

            // Collect all items from counting
            foreach (var countingContent in countingPackage.Contents) {
                itemsToProcess[countingContent.ItemCode] = (countingContent.CountedQuantity, countingContent.OriginalQuantity);
            }

            // Process each item
            foreach (var item in itemsToProcess) {
                string itemCode = item.Key;
                decimal countedQty = item.Value.counted;
                decimal originalQty = item.Value.original ?? 0;

                if (countedQty == originalQty)
                    continue;

                decimal difference = countedQty - originalQty;

                switch (difference) {
                    case > 0:
                        // Add items to package
                        await packageContentService.AddItemToPackageAsync(new AddItemToPackageRequest {
                            PackageId = package.Id,
                            ItemCode = itemCode,
                            Quantity = difference,
                            UnitQuantity = difference,
                            UnitType = UnitType.Unit,
                            BinEntry = countingPackage.CountedBinEntry,
                            SourceOperationType = ObjectType.InventoryCounting,
                            SourceOperationId = countingId
                        }, sessionInfo.Warehouse, sessionInfo.Guid);

                        break;
                    case < 0:
                        // Remove items from package
                        await packageContentService.RemoveItemFromPackageAsync(new RemoveItemFromPackageRequest {
                            PackageId = package.Id,
                            ItemCode = itemCode,
                            Quantity = Math.Abs(difference),
                            UnitQuantity = Math.Abs(difference),
                            UnitType = UnitType.Unit,
                            SourceOperationType = ObjectType.InventoryCounting,
                            SourceOperationId = countingId
                        }, sessionInfo.Guid);

                        break;
                }
            }

            // Handle items that were in the original package but not counted (removed)
            var originalItems = package.Contents.Where(c => !itemsToProcess.ContainsKey(c.ItemCode));
            foreach (var originalItem in originalItems) {
                if (originalItem.Quantity > 0) {
                    await packageContentService.RemoveItemFromPackageAsync(new RemoveItemFromPackageRequest {
                        PackageId = package.Id,
                        ItemCode = originalItem.ItemCode,
                        Quantity = originalItem.Quantity,
                        UnitQuantity = originalItem.Quantity,
                        UnitType = UnitType.Unit,
                        SourceOperationType = ObjectType.InventoryCounting,
                        SourceOperationId = countingId
                    }, sessionInfo.Guid);
                }
            }

            // Activate package if it was created during counting and has content
            if (countingPackage.IsNewPackage && package.Status == PackageStatus.Init) {
                var packages = await packageService.ActivatePackagesBySourceAsync(ObjectType.InventoryCounting, countingId, sessionInfo);
                foreach (var packageId in packages) {
                    await externalCommandService.ExecuteCommandsAsync(CommandTriggerType.ActivatePackage, ObjectType.Package, packageId);
                }
            }
        }
    }
}