using System.Collections.Concurrent;
using Core.DTOs.ReAllocate;
using Core.DTOs.Transfer;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Moves an item's bin stock (session warehouse) into 1–3 target bins as a single SAP stock transfer:
/// one document line, every source bin as a from-allocation and every target bin as a to-allocation,
/// so SAP posts all of it or none of it. Stock held in WMS (picked-not-synced, open transfer lines)
/// is never moved.
/// </summary>
public class ReAllocateService(
    IExternalSystemAdapter     adapter,
    IPickListValidationService pickListValidation,
    ILogger<ReAllocateService> logger) : IReAllocateService {

    public const int     MaxTargets = 3;
    public const decimal Tolerance  = 0.0001m;

    // Serialises executions for the same warehouse+item in this process so two operators can't both
    // validate against the same available quantity and then both post.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ItemLocks = new();

    public async Task<ReAllocateSourcesResponse> GetSourcesAsync(string itemCode, SessionInfo sessionInfo) {
        var binStock = (await adapter.ItemBinStockAsync(itemCode, sessionInfo.Warehouse))
        .Where(b => b.Quantity > Tolerance)
        .ToArray();

        var reserved = await pickListValidation.GetReservedByBinAsync(itemCode, binStock.Select(b => b.BinEntry).ToArray());

        var bins = binStock
        .Select(b => {
            decimal held = reserved.GetValueOrDefault(b.BinEntry);
            return new ReAllocateSourceBin {
                BinEntry  = b.BinEntry,
                BinCode   = b.BinCode,
                OnHand    = b.Quantity,
                Reserved  = held,
                Available = Math.Max(0, b.Quantity - held)
            };
        })
        .Where(b => b.Available > Tolerance)
        .ToList();

        return new ReAllocateSourcesResponse {
            ItemCode       = itemCode,
            Bins           = bins,
            TotalAvailable = bins.Sum(b => b.Available)
        };
    }

    public async Task<ReAllocateResponse> ExecuteAsync(ReAllocateRequest request, SessionInfo sessionInfo) {
        string? shapeError = ValidateShape(request);
        if (shapeError != null) {
            return Fail(shapeError);
        }

        var item = (await adapter.ItemCheckAsync(request.ItemCode, null)).FirstOrDefault();
        if (item == null) {
            return Fail($"Item {request.ItemCode} not found");
        }

        if (item.IsBatchManaged || item.IsSerialManaged) {
            return Fail($"Item {request.ItemCode} is batch or serial managed; re-allocate is not supported for it");
        }

        var warehouseBins = (await adapter.GetBinsAsync(sessionInfo.Warehouse)).Select(b => b.Entry).ToHashSet();
        var foreignTarget = request.Targets.FirstOrDefault(t => !warehouseBins.Contains(t.BinEntry));
        if (foreignTarget != null) {
            return Fail($"Target bin {foreignTarget.BinEntry} does not belong to warehouse {sessionInfo.Warehouse}");
        }

        var itemLock = ItemLocks.GetOrAdd($"{sessionInfo.Warehouse}|{request.ItemCode}", _ => new SemaphoreSlim(1, 1));
        await itemLock.WaitAsync();
        try {
            // Re-read availability now: stock may have moved or been picked since the screen loaded.
            var available = (await GetSourcesAsync(request.ItemCode, sessionInfo)).Bins.ToDictionary(b => b.BinEntry);
            foreach (var source in request.Sources) {
                if (!available.TryGetValue(source.BinEntry, out var bin)) {
                    return Fail($"Bin {source.BinEntry} no longer has available stock of {request.ItemCode}");
                }

                if (source.Quantity - bin.Available > Tolerance) {
                    return Fail($"Bin {bin.BinCode}: requested {source.Quantity:0.####} but only {bin.Available:0.####} is available");
                }
            }

            decimal total = request.Sources.Sum(s => s.Quantity);
            var data = new Dictionary<string, TransferCreationDataResponse> {
                [request.ItemCode] = new() {
                    ItemCode   = request.ItemCode,
                    Quantity   = total,
                    SourceBins = request.Sources.Select(s => new TransferCreationBinResponse { BinEntry = s.BinEntry, Quantity = s.Quantity }).ToList(),
                    TargetBins = request.Targets.Select(t => new TransferCreationBinResponse { BinEntry = t.BinEntry, Quantity = t.Quantity }).ToList()
                }
            };

            logger.LogInformation(
                "Re-allocate: Item {ItemCode}, Qty {Quantity}, Sources {Sources} -> Targets {Targets}, Warehouse {Warehouse}, User {User}",
                request.ItemCode, total,
                string.Join(",", request.Sources.Select(s => $"{s.BinEntry}:{s.Quantity}")),
                string.Join(",", request.Targets.Select(t => $"{t.BinEntry}:{t.Quantity}")),
                sessionInfo.Warehouse, sessionInfo.Name);

            var result = await adapter.ProcessTransfer(
                transferNumber: 0,
                sourceWarehouse: sessionInfo.Warehouse,
                targetWaarehouse: null,
                comments: $"Re-Allocate by {sessionInfo.Name}",
                data: data,
                alertRecipients: []);

            if (!result.Success) {
                logger.LogError("Re-allocate failed for {ItemCode}: {Error}", request.ItemCode, result.ErrorMessage);
                return Fail(result.ErrorMessage ?? "SAP rejected the stock transfer");
            }

            logger.LogInformation("Re-allocate successful for {ItemCode}: SAP Doc {DocNum} (Entry {DocEntry})",
                request.ItemCode, result.ExternalNumber, result.ExternalEntry);

            return new ReAllocateResponse {
                Success      = true,
                SapDocEntry  = result.ExternalEntry,
                SapDocNumber = result.ExternalNumber
            };
        }
        finally {
            itemLock.Release();
        }
    }

    /// <summary>Request checks that need no external data.</summary>
    public static string? ValidateShape(ReAllocateRequest request) {
        if (string.IsNullOrWhiteSpace(request.ItemCode)) {
            return "Item code is required";
        }

        if (request.Sources.Count == 0) {
            return "At least one source bin is required";
        }

        if (request.Targets.Count is 0 or > MaxTargets) {
            return $"Between 1 and {MaxTargets} target bins are required";
        }

        if (request.Sources.Concat(request.Targets).Any(b => b.Quantity <= 0)) {
            return "Every quantity must be greater than zero";
        }

        if (request.Sources.Select(s => s.BinEntry).Distinct().Count() != request.Sources.Count) {
            return "A source bin is listed more than once";
        }

        if (request.Targets.Select(t => t.BinEntry).Distinct().Count() != request.Targets.Count) {
            return "A target bin is listed more than once";
        }

        if (request.Sources.Any(s => request.Targets.Any(t => t.BinEntry == s.BinEntry))) {
            return "A bin cannot be both a source and a target";
        }

        decimal sourceTotal = request.Sources.Sum(s => s.Quantity);
        decimal targetTotal = request.Targets.Sum(t => t.Quantity);
        if (Math.Abs(sourceTotal - targetTotal) > Tolerance) {
            return $"Target quantity ({targetTotal:0.####}) must equal source quantity ({sourceTotal:0.####})";
        }

        return null;
    }

    private static ReAllocateResponse Fail(string message) => new() { Success = false, ErrorMessage = message };
}
