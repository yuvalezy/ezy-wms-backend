using Core.DTOs;
using Core.DTOs.GoodsReceipt;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Services;

public class GoodsReceiptReportService(SystemDbContext db, IExternalSystemAdapter adapter, IGoodsReceiptLineService lineService) : IGoodsReceiptReportService {
    public async Task<GoodsReceiptReportAllResponse> GetGoodsReceiptAllReport(Guid id, string warehouse) {
        var response = await db.GoodsReceiptLines
            .Include(l => l.GoodsReceipt)
            .Include(l => l.Targets)
            .Where(l => l.GoodsReceiptId == id)
            .GroupBy(l => new { l.ItemCode })
            .Select(g => new GoodsReceiptReportAllResponseLine {
                ItemCode = g.Key.ItemCode,
                Quantity = g.Sum(a => a.Quantity),
                Delivery = g.SelectMany(a => a.Targets
                        .Where(b => b.TargetType == 13 || b.TargetType == 17)
                        .Select(b => b.TargetQuantity))
                    .Sum(),
                Showroom = g.SelectMany(a => a.Targets
                        .Where(b => b.TargetType == 1250000001)
                        .Select(b => b.TargetQuantity))
                    .Sum(),
            })
            .ToListAsync();

        string[] items          = response.Select(r => r.ItemCode).ToArray();
        var      itemsStockData = await adapter.ItemsWarehouseStockAsync(warehouse, items);
        response.ForEach(r => {
            if (!itemsStockData.TryGetValue(r.ItemCode, out var itemStockData))
                throw new InvalidOperationException($"Item {r.ItemCode} not found in stock data from external adapter");
            r.ItemName   = itemStockData.ItemName;
            r.Stock      = itemStockData.Stock;
            r.NumInBuy   = itemStockData.NumInBuy;
            r.BuyUnitMsr = itemStockData.BuyUnitMsr;
            r.PurPackUn  = itemStockData.PurPackUn;
            r.PurPackMsr = itemStockData.PurPackMsr;
        });

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
            .Select(l => new GoodsReceiptReportAllDetailsResponse {
                LineId            = l.Id,
                CreatedByUserName = l.CreatedByUser!.FullName,
                TimeStamp         = l.UpdatedAt ?? l.CreatedAt,
                Quantity          = l.Quantity,
                Unit              = l.Unit
            })
            .ToListAsync();

        return lines;
    }

    public async Task<string?> UpdateGoodsReceiptAll(UpdateGoodsReceiptAllRequest request, SessionInfo sessionInfo) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            await lineService.RemoveRows(request.RemoveRows, sessionInfo);

            foreach (var pair in request.QuantityChanges) {
                var lineId   = pair.Key;
                int quantity = (int)pair.Value;
                var response = await lineService.UpdateLineQuantity(sessionInfo, new UpdateGoodsReceiptLineQuantityRequest {
                    Id       = request.Id,
                    LineId   = lineId,
                    Quantity = quantity
                });
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
                ItemCode         = g.Key.ItemCode,
                ItemName         = g.Key.ItemCode, // Would need item master
                ReceivedQuantity = g.Sum(l => l.Quantity),
                ExitQuantity     = 0, // Would need exit data
                Variance         = g.Sum(l => l.Quantity)
            })
            .ToListAsync();

        return lines;
    }

    public async Task<IEnumerable<GoodsReceiptValidateProcessResponse>> GetGoodsReceiptValidateProcess(Guid id) {
        var linesQuery = db.GoodsReceiptLines
            .Where(v => v.GoodsReceiptId == id);
        var linesIds = linesQuery.Select(v => v.Id).ToList();

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


        var docsData = await adapter.GoodsReceiptValidateProcessDocumentsData(docs);

        var response = new List<GoodsReceiptValidateProcessResponse>();

        foreach (var doc in docsData) {
            var value = new GoodsReceiptValidateProcessResponse {
                DocumentNumber = doc.DocumentNumber,
                Vendor         = doc.Vendor,
                BaseType       = doc.ObjectType,
                BaseEntry      = doc.DocumentEntry,
            };
            foreach (var docLine in doc.Lines) {
                var baseLine = (await sourcesQuery
                    .Where(v => v.SourceType == doc.ObjectType &&
                                v.SourceEntry == doc.DocumentEntry &&
                                v.SourceLine == docLine.LineNumber)
                    .FirstOrDefaultAsync())?.GoodsReceiptLineId ?? Guid.Empty;
                int sourceQuantity = (int)await sourcesQuery
                    .Where(v => v.SourceType == doc.ObjectType &&
                                v.SourceEntry == doc.DocumentEntry &&
                                v.SourceLine == docLine.LineNumber)
                    .SumAsync(v => v.Quantity);
                var lineValue = new GoodsReceiptValidateProcessLineResponse {
                    VisualLineNumber = docLine.VisualLineNumber,
                    LineNumber       = docLine.LineNumber,
                    ItemCode         = docLine.ItemCode,
                    ItemName         = docLine.ItemName,
                    Quantity         = sourceQuantity,
                    BaseLine         = baseLine,
                    DocumentQuantity = docLine.DocumentQuantity,
                    NumInBuy         = docLine.NumInBuy,
                    BuyUnitMsr       = docLine.BuyUnitMsr,
                    PurPackUn        = docLine.PurPackUn,
                    PurPackMsr       = docLine.PurPackMsr,
                    LineStatus       = GoodsReceiptValidateProcessLineStatus.OK
                };
                if (docLine.DocumentQuantity < sourceQuantity)
                    lineValue.LineStatus = GoodsReceiptValidateProcessLineStatus.LessScan;
                else if (docLine.DocumentQuantity > sourceQuantity)
                    lineValue.LineStatus = GoodsReceiptValidateProcessLineStatus.MoreScan;
                else if (sourceQuantity == 0)
                    lineValue.LineStatus = GoodsReceiptValidateProcessLineStatus.NotReceived;

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

        return data.SelectMany(a => a.Sources.Select(b => new GoodsReceiptValidateProcessLineDetailsResponse {
            TimeStamp         = b.CreatedAt,
            CreatedByUserName = b.CreatedByUser!.FullName,
            Quantity          = b.Quantity,
            ScannedQuantity   = a.Quantity,
            Unit              = a.Unit,
        }));
    }
}