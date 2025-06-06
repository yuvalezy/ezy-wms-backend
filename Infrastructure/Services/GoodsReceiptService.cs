using Azure.Core;
using Core.DTOs;
using Core.DTOs.GoodsReceipt;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GoodsReceiptService(SystemDbContext db, IExternalSystemAdapter adapter) : IGoodsReceiptService {
    public async Task<GoodsReceiptResponse> CreateGoodsReceipt(CreateGoodsReceiptRequest request, SessionInfo session) {
        await ValidateCreateGoodsReceiptRequest(session.Warehouse, request);
        var now = DateTime.UtcNow;
        var goodsReceipt = new GoodsReceipt {
            Type            = request.Type,
            CardCode        = request.Type == GoodsReceiptType.All ? request.CardCode : null,
            Name            = request.Name,
            Date            = now,
            Status          = ObjectStatus.Open,
            WhsCode         = session.Warehouse,
            CreatedByUserId = session.Guid,
            Documents = request.Documents.Select(d => new GoodsReceiptDocument {
                CreatedByUserId = session.Guid,
                DocEntry        = d.DocumentEntry,
                DocNumber       = d.DocumentNumber,
                ObjType         = d.ObjectType,
            }).ToArray()
        };

        await db.GoodsReceipts.AddAsync(goodsReceipt);
        await db.SaveChangesAsync();

        return MapToResponse(goodsReceipt);
    }

    private async Task ValidateCreateGoodsReceiptRequest(string warehouse, CreateGoodsReceiptRequest request) {
        if (!string.IsNullOrWhiteSpace(request.CardCode) && request.Type == GoodsReceiptType.All) {
            var vendor = await adapter.GetVendorAsync(request.CardCode);
            if (vendor == null) {
                throw new ArgumentException($"Vendor with card code {request.CardCode} not found");
            }
        }

        if (request.Type != GoodsReceiptType.All && request.Documents.Count > 0) {
            await adapter.ValidateGoodsReceiptDocuments(warehouse, request.Type, request.Documents);
        }
    }

    public async Task<IEnumerable<GoodsReceiptResponse>> GetGoodsReceipts(GoodsReceiptsRequest request, string warehouse) {
        var query = db.GoodsReceipts
            .Include(gr => gr.Documents)
            .Include(gr => gr.CreatedByUser)
            .Where(gr => gr.WhsCode == warehouse);

        if (request.ID.HasValue)
            query = query.Where(gr => gr.Number == request.ID.Value);

        if (request.Date.HasValue)
            query = query.Where(gr => gr.Date.Date == request.Date.Value.Date);

        if (!string.IsNullOrEmpty(request.CardCode))
            query = query.Where(gr => gr.CardCode == request.CardCode);

        if (request.Statuses?.Length > 0)
            query = query.Where(gr => request.Statuses.Contains(gr.Status));

        if (request.Confirm.HasValue) {
            query = request.Confirm.Value ? query.Where(gr => gr.Type == GoodsReceiptType.SpecificReceipts) : query.Where(gr => gr.Type != GoodsReceiptType.SpecificReceipts);
        }

        var results = await query.AsNoTracking().OrderByDescending(gr => gr.CreatedAt).ToListAsync();
        return results.Select(MapToResponse);
    }

    public async Task<GoodsReceiptResponse?> GetGoodsReceipt(Guid id) {
        var goodsReceipt = await db.GoodsReceipts
            .Include(gr => gr.CreatedByUser)
            .Include(gr => gr.Documents)
            .Include(gr => gr.Lines)
            .ThenInclude(l => l.CancellationReason)
            .AsNoTracking()
            .FirstOrDefaultAsync(gr => gr.Id == id);

        return goodsReceipt == null ? null : MapToResponse(goodsReceipt);
    }

    public async Task<bool> CancelGoodsReceipt(Guid id, SessionInfo session) {
        var goodsReceipt = await db.GoodsReceipts
            .Include(gr => gr.Lines)
            .FirstOrDefaultAsync(gr => gr.Id == id);

        if (goodsReceipt == null)
            return false;

        if (goodsReceipt.Status != ObjectStatus.Open && goodsReceipt.Status != ObjectStatus.InProgress)
            return false;

        goodsReceipt.Status          = ObjectStatus.Cancelled;
        goodsReceipt.UpdatedAt       = DateTime.UtcNow;
        goodsReceipt.UpdatedByUserId = session.Guid;

        // Cancel all open lines
        foreach (var line in goodsReceipt.Lines.Where(l => l.LineStatus != LineStatus.Closed)) {
            line.LineStatus      = LineStatus.Closed;
            line.UpdatedAt       = DateTime.UtcNow;
            line.UpdatedByUserId = session.Guid;
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ProcessGoodsReceiptResponse> ProcessGoodsReceipt(Guid id, SessionInfo session) {
        var transaction = await db.Database.BeginTransactionAsync();
        try {
            var goodsReceipt = await db.GoodsReceipts
                .Include(gr => gr.Lines.Where(l => l.LineStatus != LineStatus.Closed))
                .FirstOrDefaultAsync(gr => gr.Id == id);

            if (goodsReceipt == null) {
                throw new InvalidOperationException($"Goods receipt {id} not found");
            }

            if (goodsReceipt.Status != ObjectStatus.Open && goodsReceipt.Status != ObjectStatus.InProgress) {
                throw new InvalidOperationException("Cannot process goods receipt if status is not Open or In Progress");
            }

            // Update status to Processing
            goodsReceipt.Status          = ObjectStatus.Processing;
            goodsReceipt.UpdatedAt       = DateTime.UtcNow;
            goodsReceipt.UpdatedByUserId = session.Guid;
            await db.SaveChangesAsync();

            // Prepare data for SAP B1
            var goodsReceiptData = await PrepareGoodsReceiptData(goodsReceipt);

            // Process in external system
            var result = await adapter.ProcessGoodsReceipt(goodsReceipt.Number, session.Warehouse, goodsReceiptData);

            if (result.Success) {
                goodsReceipt.Status          = ObjectStatus.Finished;
                goodsReceipt.UpdatedAt       = DateTime.UtcNow;
                goodsReceipt.UpdatedByUserId = session.Guid;

                // Close all open lines
                foreach (var line in goodsReceipt.Lines) {
                    line.LineStatus      = LineStatus.Closed;
                    line.UpdatedAt       = DateTime.UtcNow;
                    line.UpdatedByUserId = session.Guid;
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ProcessGoodsReceiptResponse {
                    Status         = ResponseStatus.Ok,
                    Success        = true,
                    DocumentNumber = result.DocumentNumber
                };
            }
            else {
                await transaction.RollbackAsync();
                return new ProcessGoodsReceiptResponse {
                    Status       = ResponseStatus.Error,
                    Success      = false,
                    ErrorMessage = result.ErrorMessage
                };
            }
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            return new ProcessGoodsReceiptResponse {
                Status       = ResponseStatus.Error,
                Success      = false,
                ErrorMessage = ex.Message
            };
        }
    }

    // Report methods
    public async Task<IEnumerable<GoodsReceiptReportAllResponse>> GetGoodsReceiptAllReport(Guid id, string warehouse) {
        var response = await db.GoodsReceiptLines
            .Include(l => l.GoodsReceipt)
            .Include(l => l.Targets)
            .Where(l => l.GoodsReceiptId == id && l.LineStatus != LineStatus.Closed)
            .GroupBy(l => new { l.ItemCode })
            .Select(g => new GoodsReceiptReportAllResponse {
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

        return response;
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

    public async Task<bool> UpdateGoodsReceiptAll(UpdateGoodsReceiptAllRequest request, SessionInfo session) {
        var goodsReceipt = await db.GoodsReceipts
            .Include(gr => gr.Lines)
            .FirstOrDefaultAsync(gr => gr.Id == request.Id);

        if (goodsReceipt == null)
            return false;
        
        
        foreach (var line in goodsReceipt.Lines) {
            if (request.RemoveRows.Contains(line.Id)) {
                line.UpdatedAt       = DateTime.UtcNow;
                line.UpdatedByUserId = session.Guid;
                line.LineStatus      = LineStatus.Closed;
                continue;
            }

            if (!request.QuantityChanges.TryGetValue(line.Id, out decimal change)) 
                continue;
            line.Quantity        = change;
            line.UpdatedAt       = DateTime.UtcNow;
            line.UpdatedByUserId = session.Guid;
        }
        
        await db.SaveChangesAsync();
        return true;
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
        throw new NotImplementedException();
        // var lines = await db.GoodsReceiptLines
        //     .Include(l => l.GoodsReceipt)
        //     .Include(l => l.Sources)
        //     .Where(l => l.GoodsReceiptId == id && l.LineStatus == LineStatus.Open)
        //     .Select(l => new GoodsReceiptValidateProcessResponse {
        //         LineID   = l.Id,
        //         ItemCode = l.ItemCode,
        //         ItemName = l.ItemCode, // Would need item master
        //         BarCode  = l.BarCode,
        //         Quantity = l.Quantity,
        //         IsValid  = true, // Would need validation logic
        //         Sources = l.Sources.Select(s => new GoodsReceiptSourceInfo {
        //             SourceType  = s.SourceType,
        //             SourceEntry = s.SourceEntry,
        //             SourceLine  = s.SourceLine,
        //             Quantity    = s.Quantity
        //         }).ToList()
        //     })
        //     .ToListAsync();
        //
        // return lines;
    }

    public async Task<IEnumerable<GoodsReceiptValidateProcessLineDetailsResponse>> GetGoodsReceiptValidateProcessLineDetails(GoodsReceiptValidateProcessLineDetailsRequest request) {
        // This would need to query source documents from SAP
        return await Task.FromResult(new List<GoodsReceiptValidateProcessLineDetailsResponse>());
    }

    // Helper methods
    private GoodsReceiptResponse MapToResponse(GoodsReceipt goodsReceipt) {
        return new GoodsReceiptResponse {
            ID                = goodsReceipt.Id,
            Number            = goodsReceipt.Number,
            Name              = goodsReceipt.Name,
            CardCode          = goodsReceipt.CardCode,
            Date              = goodsReceipt.Date,
            Status            = goodsReceipt.Status,
            Type              = goodsReceipt.Type,
            WhsCode           = goodsReceipt.WhsCode,
            CreatedByUserName = goodsReceipt.CreatedByUser?.FullName,
            Lines = goodsReceipt.Lines?.Select(l => new GoodsReceiptLineResponse {
                ID                   = l.Id,
                BarCode              = l.BarCode,
                ItemCode             = l.ItemCode,
                ItemName             = l.ItemCode, // Would need item master
                Quantity             = l.Quantity,
                Unit                 = l.Unit,
                LineStatus           = l.LineStatus,
                Date                 = l.Date,
                Comments             = l.Comments,
                StatusReason         = l.StatusReason,
                CancellationReasonId = l.CancellationReasonId
            }).ToList(),
            Documents = goodsReceipt.Documents?.Select(d => new GoodsReceiptDocumentResponse {
                DocumentEntry  = d.DocEntry,
                DocumentNumber = d.DocNumber,
                ObjectType   = d.ObjType,
            }).ToList()
        };
    }

    private async Task<Dictionary<string, List<GoodsReceiptCreationDataResponse>>> PrepareGoodsReceiptData(GoodsReceipt goodsReceipt) {
        var data = new Dictionary<string, List<GoodsReceiptCreationDataResponse>>();

        foreach (var line in goodsReceipt.Lines) {
            if (!data.ContainsKey(line.ItemCode))
                data[line.ItemCode] = new List<GoodsReceiptCreationDataResponse>();

            var sources = await db.GoodsReceiptSources
                .Where(s => s.GoodsReceiptLineId == line.Id)
                .Select(s => new GoodsReceiptSourceDataResponse {
                    SourceType  = s.SourceType,
                    SourceEntry = s.SourceEntry,
                    SourceLine  = s.SourceLine,
                    Quantity    = s.Quantity
                })
                .ToListAsync();

            data[line.ItemCode].Add(new GoodsReceiptCreationDataResponse {
                ItemCode = line.ItemCode,
                BarCode  = line.BarCode,
                Quantity = line.Quantity,
                Unit     = line.Unit,
                Date     = line.Date,
                Comments = line.Comments,
                Sources  = sources
            });
        }

        return data;
    }
}