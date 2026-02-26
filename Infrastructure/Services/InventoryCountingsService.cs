using System.Text.Json;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class InventoryCountingsService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ISettings settings,
    IPackageContentService packageContentService,
    IPackageService packageService,
    IExternalCommandService externalCommandService,
    IPackageLocationService packageLocationService,
    IExternalSystemAlertService alertService,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<InventoryCountingsService> logger) : IInventoryCountingsService {

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

    // Phase A: Create batches and return immediately, fire-and-forget background processing
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

            // Create batch records
            int batchSize = settings.Options.InventoryCountingBatchSize;
            int? initialCountingBinEntry = sessionInfo.EnableBinLocations ? settings.GetInitialCountingBinEntry(counting.WhsCode) : null;
            var batches = CreateBatches(counting.Id, countingData, initialCountingBinEntry, batchSize);

            await db.InventoryCountingBatches.AddRangeAsync(batches);
            await db.SaveChangesAsync();

            await transaction.CommitAsync();

            // Fire-and-forget: start background processing
            _ = Task.Run(async () => {
                try {
                    using var scope = serviceScopeFactory.CreateScope();
                    var bgService = scope.ServiceProvider.GetRequiredService<IInventoryCountingsService>();
                    await bgService.ProcessBatchesInBackground(id, sessionInfo);
                }
                catch (Exception ex) {
                    // Log but don't throw - this is fire-and-forget
                    using var scope = serviceScopeFactory.CreateScope();
                    var bgLogger = scope.ServiceProvider.GetRequiredService<ILogger<InventoryCountingsService>>();
                    bgLogger.LogError(ex, "Unhandled error in background batch processing for counting {CountingId}", id);
                }
            });

            return new ProcessInventoryCountingResponse {
                Success = true,
                Status = ResponseStatus.Ok,
                TotalBatches = batches.Count,
                CompletedBatches = 0,
                FailedBatches = 0,
                Batches = batches.Select(MapBatchToResponse).ToList()
            };
        }
        catch (Exception) {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Phase B: Process batches sequentially in background
    public async Task ProcessBatchesInBackground(Guid countingId, SessionInfo sessionInfo) {
        var counting = await db.InventoryCountings.FirstOrDefaultAsync(ic => ic.Id == countingId);
        if (counting == null) return;

        var batches = await db.InventoryCountingBatches
            .Where(b => b.InventoryCountingId == countingId)
            .OrderBy(b => b.SequenceOrder)
            .ToListAsync();

        var alertRecipients = await alertService.GetAlertRecipientsAsync(AlertableObjectType.InventoryCounting);

        // Process regular batches first
        var regularBatches = batches.Where(b => !b.IsInitialBinBatch).OrderBy(b => b.SequenceOrder).ToList();
        foreach (var batch in regularBatches) {
            if (batch.Status == BatchStatus.Completed) continue;
            await ProcessSingleBatch(counting, batch, sessionInfo.Warehouse, alertRecipients);
        }

        // Check if all regular batches succeeded
        bool allRegularSucceeded = regularBatches.All(b => b.Status == BatchStatus.Completed);

        // Process initial bin batches only if all regular batches succeeded
        var initialBinBatches = batches.Where(b => b.IsInitialBinBatch).OrderBy(b => b.SequenceOrder).ToList();
        if (allRegularSucceeded && initialBinBatches.Count > 0) {
            foreach (var batch in initialBinBatches) {
                if (batch.Status == BatchStatus.Completed) continue;
                await ProcessSingleBatch(counting, batch, sessionInfo.Warehouse, alertRecipients);
            }
        }

        // Evaluate final state
        await EvaluateAndFinalize(counting, batches, sessionInfo, alertRecipients);
    }

    private async Task ProcessSingleBatch(InventoryCounting counting, InventoryCountingBatch batch, string warehouse, string[] alertRecipients) {
        batch.Status = BatchStatus.Processing;
        batch.LastAttemptAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        try {
            var data = JsonSerializer.Deserialize<Dictionary<string, InventoryCountingCreationDataResponse>>(batch.PayloadJson)!;

            // Build reference2: if multiple batches, use "{number}-{sequence}" format
            var totalBatches = await db.InventoryCountingBatches
                .CountAsync(b => b.InventoryCountingId == counting.Id);
            string reference2 = totalBatches > 1
                ? $"{counting.Number}-{batch.SequenceOrder}"
                : counting.Number.ToString();

            var result = await adapter.ProcessInventoryCounting(counting.Number, warehouse, data, alertRecipients, reference2);

            if (result.Success) {
                batch.Status = BatchStatus.Completed;
                batch.SapDocEntry = result.ExternalEntry;
                batch.SapDocNumber = result.ExternalNumber;
                batch.ErrorMessage = null;
            }
            else {
                batch.Status = BatchStatus.Failed;
                batch.ErrorMessage = result.ErrorMessage ?? "Unknown error";
                batch.RetryCount++;
            }
        }
        catch (Exception ex) {
            batch.Status = BatchStatus.Failed;
            batch.ErrorMessage = ex.Message;
            batch.RetryCount++;
            logger.LogError(ex, "Failed to process batch {BatchId} (sequence {Sequence}) for counting {CountingId}",
                batch.Id, batch.SequenceOrder, counting.Id);
        }

        await db.SaveChangesAsync();
    }

    private async Task EvaluateAndFinalize(InventoryCounting counting, List<InventoryCountingBatch> batches, SessionInfo sessionInfo, string[] alertRecipients) {
        bool allCompleted = batches.All(b => b.Status == BatchStatus.Completed);

        if (allCompleted) {
            // Finalize - all batches succeeded
            var finalizeTx = await db.Database.BeginTransactionAsync();
            try {
                counting.Status = ObjectStatus.Closed;
                counting.UpdatedAt = DateTime.UtcNow;
                counting.UpdatedByUserId = sessionInfo.Guid;

                // Close all open lines
                var openLines = await db.InventoryCountingLines
                    .Where(icl => icl.InventoryCountingId == counting.Id && icl.LineStatus != LineStatus.Closed)
                    .ToListAsync();

                foreach (var line in openLines) {
                    line.LineStatus = LineStatus.Closed;
                    line.UpdatedAt = DateTime.UtcNow;
                    line.UpdatedByUserId = sessionInfo.Guid;
                }

                // Process packages
                await ProcessCountingPackages(counting.Id, sessionInfo);

                await db.SaveChangesAsync();
                await finalizeTx.CommitAsync();
            }
            catch (Exception ex) {
                await finalizeTx.RollbackAsync();
                logger.LogError(ex, "Failed to finalize counting {CountingId}", counting.Id);
                counting.Status = ObjectStatus.PartiallyProcessed;
                await db.SaveChangesAsync();
            }
        }
        else {
            counting.Status = ObjectStatus.PartiallyProcessed;
            counting.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task<ProcessInventoryCountingResponse> RetryFailedBatches(RetryBatchRequest request, SessionInfo sessionInfo) {
        var counting = await db.InventoryCountings.FirstOrDefaultAsync(ic => ic.Id == request.CountingId);
        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {request.CountingId} not found.");
        }

        if (counting.Status != ObjectStatus.PartiallyProcessed) {
            throw new InvalidOperationException("Can only retry batches for partially processed countings");
        }

        var batches = await db.InventoryCountingBatches
            .Where(b => b.InventoryCountingId == request.CountingId)
            .OrderBy(b => b.SequenceOrder)
            .ToListAsync();

        // If specific batch requested, validate it's failed
        if (request.BatchId.HasValue) {
            var targetBatch = batches.FirstOrDefault(b => b.Id == request.BatchId.Value);
            if (targetBatch == null) throw new KeyNotFoundException("Batch not found");
            if (targetBatch.Status != BatchStatus.Failed) throw new InvalidOperationException("Batch is not in Failed status");
        }

        // Set counting back to Processing
        counting.Status = ObjectStatus.Processing;
        counting.UpdatedAt = DateTime.UtcNow;
        counting.UpdatedByUserId = sessionInfo.Guid;
        await db.SaveChangesAsync();

        // Fire-and-forget: start background retry
        _ = Task.Run(async () => {
            try {
                using var scope = serviceScopeFactory.CreateScope();
                var bgDb = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
                var bgAdapter = scope.ServiceProvider.GetRequiredService<IExternalSystemAdapter>();
                var bgAlertService = scope.ServiceProvider.GetRequiredService<IExternalSystemAlertService>();
                var bgLogger = scope.ServiceProvider.GetRequiredService<ILogger<InventoryCountingsService>>();

                var bgCounting = await bgDb.InventoryCountings.FirstAsync(ic => ic.Id == request.CountingId);
                var bgBatches = await bgDb.InventoryCountingBatches
                    .Where(b => b.InventoryCountingId == request.CountingId)
                    .OrderBy(b => b.SequenceOrder)
                    .ToListAsync();

                var alertRecipients = await bgAlertService.GetAlertRecipientsAsync(AlertableObjectType.InventoryCounting);

                // Get failed batches to retry
                var failedBatches = request.BatchId.HasValue
                    ? bgBatches.Where(b => b.Id == request.BatchId.Value).ToList()
                    : bgBatches.Where(b => b.Status == BatchStatus.Failed && !b.IsInitialBinBatch).ToList();

                // Use a temporary service-like object for processing
                var bgService = scope.ServiceProvider.GetRequiredService<IInventoryCountingsService>() as InventoryCountingsService;
                if (bgService == null) return;

                foreach (var batch in failedBatches) {
                    await bgService.ProcessSingleBatch(bgCounting, batch, sessionInfo.Warehouse, alertRecipients);
                }

                // Check if all regular batches succeeded, process initial bin batches
                var regularBatches = bgBatches.Where(b => !b.IsInitialBinBatch).ToList();
                bool allRegularSucceeded = regularBatches.All(b => b.Status == BatchStatus.Completed);

                var initialBinBatches = bgBatches.Where(b => b.IsInitialBinBatch).ToList();
                if (allRegularSucceeded && initialBinBatches.Any(b => b.Status != BatchStatus.Completed)) {
                    foreach (var batch in initialBinBatches.Where(b => b.Status != BatchStatus.Completed)) {
                        await bgService.ProcessSingleBatch(bgCounting, batch, sessionInfo.Warehouse, alertRecipients);
                    }
                }

                await bgService.EvaluateAndFinalize(bgCounting, bgBatches, sessionInfo, alertRecipients);
            }
            catch (Exception ex) {
                using var scope = serviceScopeFactory.CreateScope();
                var bgLogger = scope.ServiceProvider.GetRequiredService<ILogger<InventoryCountingsService>>();
                bgLogger.LogError(ex, "Unhandled error in background retry processing for counting {CountingId}", request.CountingId);
            }
        });

        return new ProcessInventoryCountingResponse {
            Success = true,
            Status = ResponseStatus.Ok,
            TotalBatches = batches.Count,
            CompletedBatches = batches.Count(b => b.Status == BatchStatus.Completed),
            FailedBatches = batches.Count(b => b.Status == BatchStatus.Failed),
            Batches = batches.Select(MapBatchToResponse).ToList()
        };
    }

    public async Task<IEnumerable<InventoryCountingBatchResponse>> GetBatches(Guid countingId) {
        var batches = await db.InventoryCountingBatches
            .Where(b => b.InventoryCountingId == countingId)
            .OrderBy(b => b.SequenceOrder)
            .ToListAsync();

        return batches.Select(MapBatchToResponse);
    }

    private List<InventoryCountingBatch> CreateBatches(
        Guid countingId,
        Dictionary<string, InventoryCountingCreationDataResponse> countingData,
        int? initialCountingBinEntry,
        int batchSize) {

        // Flatten all lines into (ItemCode, BinEntry, ...) tuples, separated by regular vs initial bin
        var regularLines = new List<(string ItemCode, InventoryCountingCreationBinResponse Bin)>();
        var initialBinLines = new List<(string ItemCode, InventoryCountingCreationBinResponse Bin)>();
        var noBinItems = new List<(string ItemCode, InventoryCountingCreationDataResponse Data)>();

        foreach (var kvp in countingData) {
            var item = kvp.Value;
            if (item.CountedBins.Count == 0) {
                // Items with no bins go to regular
                noBinItems.Add((item.ItemCode, item));
                continue;
            }

            foreach (var bin in item.CountedBins) {
                if (initialCountingBinEntry.HasValue && bin.BinEntry == initialCountingBinEntry.Value) {
                    initialBinLines.Add((item.ItemCode, bin));
                }
                else {
                    regularLines.Add((item.ItemCode, bin));
                }
            }
        }

        var batches = new List<InventoryCountingBatch>();
        int sequence = 1;

        // Chunk regular lines into batches
        var regularChunks = ChunkLines(regularLines, noBinItems, batchSize);
        foreach (var chunk in regularChunks) {
            batches.Add(new InventoryCountingBatch {
                InventoryCountingId = countingId,
                SequenceOrder = sequence++,
                IsInitialBinBatch = false,
                LineCount = chunk.lineCount,
                Status = BatchStatus.Pending,
                PayloadJson = JsonSerializer.Serialize(chunk.data)
            });
        }

        // Chunk initial bin lines into batches (usually just 1)
        if (initialBinLines.Count > 0) {
            var initialChunks = ChunkLines(initialBinLines, [], batchSize);
            foreach (var chunk in initialChunks) {
                batches.Add(new InventoryCountingBatch {
                    InventoryCountingId = countingId,
                    SequenceOrder = sequence++,
                    IsInitialBinBatch = true,
                    LineCount = chunk.lineCount,
                    Status = BatchStatus.Pending,
                    PayloadJson = JsonSerializer.Serialize(chunk.data)
                });
            }
        }

        // Edge case: if no batches were created (empty counting data), create at least one
        if (batches.Count == 0) {
            batches.Add(new InventoryCountingBatch {
                InventoryCountingId = countingId,
                SequenceOrder = 1,
                IsInitialBinBatch = false,
                LineCount = 0,
                Status = BatchStatus.Pending,
                PayloadJson = JsonSerializer.Serialize(new Dictionary<string, InventoryCountingCreationDataResponse>())
            });
        }

        return batches;
    }

    private static List<(Dictionary<string, InventoryCountingCreationDataResponse> data, int lineCount)> ChunkLines(
        List<(string ItemCode, InventoryCountingCreationBinResponse Bin)> binLines,
        List<(string ItemCode, InventoryCountingCreationDataResponse Data)> noBinItems,
        int batchSize) {

        var result = new List<(Dictionary<string, InventoryCountingCreationDataResponse> data, int lineCount)>();

        // Combine all SAP lines: each bin line = 1 SAP line, each no-bin item = 1 SAP line
        var allLines = new List<(string ItemCode, InventoryCountingCreationBinResponse? Bin, InventoryCountingCreationDataResponse? NoBinData)>();

        foreach (var (itemCode, bin) in binLines) {
            allLines.Add((itemCode, bin, null));
        }
        foreach (var (itemCode, data) in noBinItems) {
            allLines.Add((itemCode, null, data));
        }

        // Chunk into groups of batchSize
        for (int i = 0; i < allLines.Count; i += batchSize) {
            var chunk = allLines.Skip(i).Take(batchSize).ToList();
            var batchData = new Dictionary<string, InventoryCountingCreationDataResponse>();
            int lineCount = 0;

            foreach (var (itemCode, bin, noBinData) in chunk) {
                if (noBinData != null) {
                    // No-bin item: include as-is
                    batchData[itemCode] = new InventoryCountingCreationDataResponse {
                        ItemCode = noBinData.ItemCode,
                        CountedQuantity = noBinData.CountedQuantity,
                        SystemQuantity = noBinData.SystemQuantity,
                        Variance = noBinData.Variance,
                        CountedBins = new List<InventoryCountingCreationBinResponse>()
                    };
                    lineCount++;
                }
                else if (bin != null) {
                    // Bin line: group into existing or new item entry
                    if (!batchData.TryGetValue(itemCode, out var existing)) {
                        existing = new InventoryCountingCreationDataResponse {
                            ItemCode = itemCode,
                            CountedQuantity = 0,
                            SystemQuantity = 0,
                            Variance = 0,
                            CountedBins = new List<InventoryCountingCreationBinResponse>()
                        };
                        batchData[itemCode] = existing;
                    }
                    existing.CountedBins.Add(bin);
                    existing.CountedQuantity += bin.CountedQuantity;
                    existing.SystemQuantity += bin.SystemQuantity;
                    existing.Variance = existing.CountedQuantity - existing.SystemQuantity;
                    lineCount++;
                }
            }

            if (lineCount > 0) {
                result.Add((batchData, lineCount));
            }
        }

        return result;
    }

    private static InventoryCountingBatchResponse MapBatchToResponse(InventoryCountingBatch batch) => new() {
        Id = batch.Id,
        SequenceOrder = batch.SequenceOrder,
        Status = batch.Status,
        IsInitialBinBatch = batch.IsInitialBinBatch,
        LineCount = batch.LineCount,
        SapDocEntry = batch.SapDocEntry,
        SapDocNumber = batch.SapDocNumber,
        ErrorMessage = batch.ErrorMessage,
        LastAttemptAt = batch.LastAttemptAt,
        RetryCount = batch.RetryCount
    };

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


    private async Task ProcessCountingPackages(Guid countingId, SessionInfo sessionInfo) {
        var countingPackages = await db.InventoryCountingPackages
        .Include(cp => cp.Contents)
        .Include(cp => cp.Package)
        .ThenInclude(p => p.Contents)
        .AsSplitQuery()
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

        var lineIds = values.Select(v => v.LineId).ToArray();

        var packageTransactions = await db.PackageTransactions
            .Include(v => v.Package)
            .Where(v => v.SourceOperationId == id && v.SourceOperationType == ObjectType.InventoryCounting && v.SourceOperationLineId != null && lineIds.Contains(v.SourceOperationLineId.Value))
            .Select(v => new { v.SourceOperationLineId, v.PackageId, v.Package.Barcode })
            .ToArrayAsync();

        values.ForEach(v => {
            var package = packageTransactions.FirstOrDefault(p => p.SourceOperationLineId == v.LineId);
            if (package != null)
                v.Package = new PackageValueResponse(package.PackageId, package.Barcode);
        });

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

                var packageTransactions = await db.PackageTransactions
                    .Where(v => v.SourceOperationLineId != null && request.RemoveRows.Contains(v.SourceOperationLineId.Value))
                    .ToArrayAsync();

                foreach (var line in linesToRemove) {
                    line.UpdatedAt = DateTime.UtcNow;
                    line.UpdatedByUserId = sessionInfo.Guid;
                    line.LineStatus = LineStatus.Closed;
                    line.Deleted = true;
                    line.DeletedAt = DateTime.UtcNow;
                    db.InventoryCountingLines.Update(line);

                    var pkgTransaction = packageTransactions.FirstOrDefault(v => v.SourceOperationLineId == line.Id);
                    if (pkgTransaction != null && line.PackageId.HasValue) {
                        decimal removeQuantity = line.Quantity;
                        if (line.Unit != UnitType.Unit) {
                            var data = await adapter.GetItemInfo(line.ItemCode);
                            removeQuantity /= data.QuantityInUnit;
                            if (line.Unit == UnitType.Pack)
                                removeQuantity /= data.QuantityInPack;
                        }

                        await packageContentService.RemoveItemFromPackageAsync(new RemoveItemFromPackageRequest {
                            PackageId = line.PackageId.Value,
                            ItemCode = line.ItemCode,
                            Quantity = removeQuantity,
                            UnitQuantity = line.Quantity,
                            UnitType = line.Unit,
                            SourceOperationType = ObjectType.InventoryCounting,
                            SourceOperationId = request.Id
                        }, sessionInfo.Guid);
                    }
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

                decimal diff = newQuantity - line.Quantity;
                line.Quantity = newQuantity;
                line.UpdatedAt = DateTime.UtcNow;
                line.UpdatedByUserId = sessionInfo.Guid;

                // Update package content if applicable
                if (diff != 0 && line.PackageId.HasValue && item != null) {
                    decimal unitDiff = diff;
                    if (line.Unit != UnitType.Unit) {
                        unitDiff /= item.NumInBuy;
                        if (line.Unit == UnitType.Pack) {
                            unitDiff /= item.PurPackUn;
                        }
                    }

                    if (unitDiff > 0) {
                        await packageContentService.AddItemToPackageAsync(new AddItemToPackageRequest {
                            PackageId = line.PackageId.Value,
                            ItemCode = line.ItemCode,
                            Quantity = unitDiff,
                            UnitQuantity = diff,
                            UnitType = line.Unit,
                            SourceOperationType = ObjectType.InventoryCounting,
                            SourceOperationId = request.Id,
                            SourceOperationLineId = line.Id
                        }, counting.WhsCode, sessionInfo.Guid);
                    }
                    else {
                        await packageContentService.RemoveItemFromPackageAsync(new RemoveItemFromPackageRequest {
                            PackageId = line.PackageId.Value,
                            ItemCode = line.ItemCode,
                            Quantity = unitDiff * -1,
                            UnitQuantity = diff * -1,
                            UnitType = line.Unit,
                            SourceOperationType = ObjectType.InventoryCounting,
                            SourceOperationId = request.Id
                        }, sessionInfo.Guid);
                    }
                }

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
