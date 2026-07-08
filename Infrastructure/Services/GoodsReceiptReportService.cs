using Core.DTOs.GoodsReceipt;
using Core.DTOs.Items;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GoodsReceiptReportService(SystemDbContext db, IExternalSystemAdapter adapter, IGoodsReceiptLineService lineService) : IGoodsReceiptReportService {
    public async Task<GoodsReceiptReportAllResponse> GetGoodsReceiptAllReport(Guid id, string warehouse) {
        // Pull raw lines (with the scanned count) so base pieces can be re-derived from the CURRENT
        // SAP factor below, instead of summing a base quantity frozen at scan time.
        var lines = await db.GoodsReceiptLines
        .Include(l => l.Targets)
        .Where(l => l.GoodsReceiptId == id)
        .Select(l => new {
            l.ItemCode,
            l.Quantity,
            l.ScannedQuantity,
            l.Unit,
            Delivery = l.Targets.Where(b => b.TargetType == 13 || b.TargetType == 17).Sum(b => b.TargetQuantity),
            Showroom = l.Targets.Where(b => b.TargetType == 1250000001).Sum(b => b.TargetQuantity),
        })
        .ToListAsync();

        string[] items = lines.Select(l => l.ItemCode).Distinct().ToArray();
        var itemsStockData = await adapter.ItemsWarehouseStockAsync(warehouse, items);

        var response = lines
        .GroupBy(l => l.ItemCode)
        .Select(g =>
        {
            if (!itemsStockData.TryGetValue(g.Key, out var itemStockData))
                throw new InvalidOperationException($"Item {g.Key} not found in stock data from external adapter");

            // Re-derive each line's base pieces from its scanned count and the item's current factor
            // (legacy lines with ScannedQuantity == 0 fall back to the stored base), then sum.
            decimal quantity = g.Sum(l => l.ScannedQuantity > 0
                ? GoodsReceiptQuantityCalculator.ToBaseQuantity(l.ScannedQuantity, l.Unit, itemStockData)
                : l.Quantity);

            return new GoodsReceiptReportAllResponseLine {
                ItemCode = g.Key,
                Quantity = quantity,
                Delivery = g.Sum(l => l.Delivery),
                Showroom = g.Sum(l => l.Showroom),
                ItemName = itemStockData.ItemName,
                Stock = itemStockData.Stock,
                NumInBuy = itemStockData.NumInBuy,
                BuyUnitMsr = itemStockData.BuyUnitMsr,
                PurPackUn = itemStockData.PurPackUn,
                PurPackMsr = itemStockData.PurPackMsr,
                Factor1 = itemStockData.Factor1,
                Factor2 = itemStockData.Factor2,
                Factor3 = itemStockData.Factor3,
                Factor4 = itemStockData.Factor4,
                CustomFields = itemStockData.CustomFields,
            };
        })
        .ToList();

        var status = (await db.GoodsReceipts.FirstAsync(v => v.Id == id)).Status;

        return new GoodsReceiptReportAllResponse {
            Status = status,
            Lines = response,
        };
    }

    public async Task<IEnumerable<GoodsReceiptReportAllDetailsResponse>> GetGoodsReceiptAllReportDetails(Guid id, string itemCode) {
        var lines = await db.GoodsReceiptLines
        .Include(l => l.CreatedByUser)
        .Where(l => l.GoodsReceiptId == id && l.ItemCode == itemCode && l.LineStatus != LineStatus.Closed)
        .ToListAsync();

        // Fetch the item's current conversion factor so each row's base pieces are re-derived from its
        // scanned count (keeps the displayed box/dozen count stable when the SAP factor is corrected).
        var item = (await adapter.ItemCheckAsync(itemCode, null)).FirstOrDefault();

        return lines.Select(l =>
        {
            decimal quantity = item != null && l.ScannedQuantity > 0
                ? GoodsReceiptQuantityCalculator.ToBaseQuantity(l.ScannedQuantity, l.Unit, item)
                : l.Quantity;
            return l.TotReportAllDetailsResponseDto(quantity);
        }).ToList();
    }


    public async Task<string?> UpdateGoodsReceiptAll(UpdateGoodsReceiptAllRequest request, SessionInfo sessionInfo) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            await lineService.RemoveRows(request.RemoveRows, sessionInfo);

            foreach (var pair in request.QuantityChanges) {
                var lineId = pair.Key;
                decimal quantity = pair.Value;
                var updateRequest = new UpdateGoodsReceiptLineQuantityRequest {
                    Id = request.Id,
                    LineId = lineId,
                    Quantity = quantity
                };

                var updateGoodsReceiptLineQuantityRequest = updateRequest;
                var response = await lineService.UpdateLineQuantity(sessionInfo, updateGoodsReceiptLineQuantityRequest);
                if (!string.IsNullOrWhiteSpace(response.ErrorMessage) || response.ReturnValue != UpdateLineReturnValue.Ok) {
                    return !string.IsNullOrWhiteSpace(response.ErrorMessage) ? response.ErrorMessage : $"Return Value Code {response.ReturnValue} for Line ID {lineId}";
                }
            }

            await transaction.CommitAsync();
            return null;
        }
        catch (Exception e) {
            await transaction.RollbackAsync();
            return e.Message;
        }
    }

    public async Task<IEnumerable<GoodsReceiptVSExitReportResponse>> GetGoodsReceiptVSExitReport(Guid id) {
        // This would need integration with exit/shipment data
        var lines = await db.GoodsReceiptLines
        .Include(l => l.GoodsReceipt)
        .Where(l => l.GoodsReceipt.Id == id)
        .GroupBy(l => new { l.ItemCode })
        .Select(g => new GoodsReceiptVSExitReportResponse {
            ItemCode = g.Key.ItemCode,
            ItemName = g.Key.ItemCode, // Would need item master
            ReceivedQuantity = g.Sum(l => l.Quantity),
            ExitQuantity = 0, // Would need exit data
            Variance = g.Sum(l => l.Quantity)
        })
        .ToListAsync();

        return lines;
    }

    public async Task<IEnumerable<GoodsReceiptValidateProcessResponse>> GetGoodsReceiptValidateProcess(Guid id) {
        // Materialize lines with the scanned count so each source's base pieces can be re-derived from the
        // CURRENT SAP factor below — the confirmation "Finish" flow builds its SAP stock adjustment from
        // this, so summing the frozen (scan-time) source quantity would send the wrong difference.
        var lines = await db.GoodsReceiptLines
        .Where(v => v.GoodsReceiptId == id)
        .Select(l => new { l.Id, l.ItemCode, l.ScannedQuantity, l.Unit, l.Quantity })
        .ToListAsync();

        var linesIds = lines.Select(l => l.Id).ToList();

        var sourcesQuery = db.GoodsReceiptSources
        .Where(v => linesIds.Contains(v.GoodsReceiptLineId));

        var documentsQuery = db.GoodsReceiptDocuments
        .Where(v => v.GoodsReceiptId == id);

        var baseDocs = sourcesQuery
        .Select(v => new { Type = v.SourceType, Entry = v.SourceEntry })
        .Union(documentsQuery.Select(v => new { Type = v.ObjType, Entry = v.DocEntry }));

        var docs = await baseDocs
        .Distinct()                                        // still SQL-translatable
        .Select(x => new ObjectKey(x.Type, x.Entry, null)) // custom projection
        .ToArrayAsync();

        var sources = await sourcesQuery
        .Select(s => new { s.GoodsReceiptLineId, s.SourceType, s.SourceEntry, s.SourceLine, s.Quantity })
        .ToListAsync();

        var docsData = await adapter.GoodsReceiptValidateProcessDocumentsData(docs);

        // The doc lines carry each item's CURRENT conversion factor (same item master) — reuse it to
        // re-derive each line's base pieces, no extra SAP fetch.
        var factorByItem = new Dictionary<string, GoodsReceiptValidateProcessDocumentsDataLineResponse>();
        foreach (var doc in docsData)
            foreach (var docLine in doc.Lines)
                factorByItem.TryAdd(docLine.ItemCode, docLine);

        // Per line: stored (frozen) base and current effective base — used to rescale each source allocation.
        var lineBaseById = lines.ToDictionary(
            l => l.Id,
            l => (Stored: l.Quantity,
                  Effective: factorByItem.TryGetValue(l.ItemCode, out var f)
                      ? GoodsReceiptQuantityCalculator.EffectiveBaseQuantity(l.ScannedQuantity, l.Unit, l.Quantity, f)
                      : l.Quantity));

        var response = new List<GoodsReceiptValidateProcessResponse>();

        foreach (var doc in docsData) {
            var value = new GoodsReceiptValidateProcessResponse {
                DocumentNumber = doc.DocumentNumber,
                Vendor = doc.Vendor,
                BaseType = doc.ObjectType,
                BaseEntry = doc.DocumentEntry,
            };

            foreach (var docLine in doc.Lines) {
                var matching = sources
                .Where(s => s.SourceType == doc.ObjectType &&
                            s.SourceEntry == doc.DocumentEntry &&
                            s.SourceLine == docLine.LineNumber)
                .ToList();

                var baseLine = matching.Count > 0 ? matching[0].GoodsReceiptLineId : Guid.Empty;

                // Sum each source rescaled from its frozen base to the line's current effective base.
                decimal sourceQuantity = matching.Sum(s =>
                {
                    var b = lineBaseById.TryGetValue(s.GoodsReceiptLineId, out var v) ? v : (Stored: s.Quantity, Effective: s.Quantity);
                    return GoodsReceiptQuantityCalculator.RescaleAllocation(s.Quantity, b.Stored, b.Effective);
                });

                var lineValue = docLine.ToValidateProcessLineDto(sourceQuantity, baseLine);
                value.Lines!.Add(lineValue);
            }

            response.Add(value);
        }

        return response;
    }


    public async Task<IEnumerable<GoodsReceiptValidateProcessLineDetailsResponse>> GetGoodsReceiptValidateProcessLineDetails(GoodsReceiptValidateProcessLineDetailsRequest request) {
        var data =
        await db.GoodsReceiptLines
        .Where(v => v.GoodsReceiptId == request.ID)
        .Include(a => a.Sources
        .Where(b => b.SourceType == request.BaseType &&
                    b.SourceEntry == request.BaseEntry &&
                    b.SourceLine == request.BaseLine))
        .ThenInclude(v => v.CreatedByUser)
        .ToArrayAsync();

        // Re-derive each source/line quantity from the current factor (keeps the drill-down consistent
        // with the corrected report and the confirmation adjustment).
        var factorByItem = new Dictionary<string, ItemResponse>();
        foreach (var itemCode in data.Select(a => a.ItemCode).Distinct()) {
            var item = (await adapter.ItemCheckAsync(itemCode, null)).FirstOrDefault();
            if (item != null)
                factorByItem[itemCode] = item;
        }

        return data.SelectMany(a =>
        {
            bool hasFactor = factorByItem.TryGetValue(a.ItemCode, out var item);
            decimal effectiveBase = hasFactor
                ? GoodsReceiptQuantityCalculator.EffectiveBaseQuantity(a.ScannedQuantity, a.Unit, a.Quantity, item!)
                : a.Quantity;

            return a.Sources.Select(b => new GoodsReceiptValidateProcessLineDetailsResponse {
                TimeStamp = b.CreatedAt,
                CreatedByUserName = b.CreatedByUser!.FullName,
                Quantity = GoodsReceiptQuantityCalculator.RescaleAllocation(b.Quantity, a.Quantity, effectiveBase),
                ScannedQuantity = effectiveBase,
                Unit = a.Unit,
            });
        });
    }
}
