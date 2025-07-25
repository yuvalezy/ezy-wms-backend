using Core.DTOs.GoodsReceipt;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GoodsReceiptService(SystemDbContext db, IExternalSystemAdapter adapter, IPackageService packageService, ISettings settings) : IGoodsReceiptService {
    public async Task<GoodsReceiptResponse> CreateGoodsReceipt(CreateGoodsReceiptRequest request, SessionInfo session) {
        await ValidateCreateGoodsReceiptRequest(session.Warehouse, request);
        var now = DateTime.UtcNow;
        var goodsReceipt = new GoodsReceipt {
            WhsCode         = session.Warehouse,
            Type            = request.Type,
            CardCode        = request.Type == GoodsReceiptType.All ? request.Vendor : null,
            Name            = request.Name,
            Date            = now,
            Status          = ObjectStatus.Open,
            CreatedByUserId = session.Guid,
            CreatedByUser   = db.Users.First(u => u.Id == session.Guid),
            Documents = request.Documents.Select(d => new GoodsReceiptDocument {
                CreatedByUserId = session.Guid,
                DocEntry        = d.DocumentEntry,
                DocNumber       = d.DocumentNumber,
                ObjType         = d.ObjectType,
            }).ToArray()
        };

        await db.GoodsReceipts.AddAsync(goodsReceipt);
        await db.SaveChangesAsync();

        return await MapToResponse(goodsReceipt);
    }

    private async Task ValidateCreateGoodsReceiptRequest(string warehouse, CreateGoodsReceiptRequest request) {
        if (!string.IsNullOrWhiteSpace(request.Vendor) && request.Type == GoodsReceiptType.All) {
            var vendor = await adapter.GetVendorAsync(request.Vendor);
            if (vendor == null) {
                throw new ArgumentException($"Vendor with card code {request.Vendor} not found");
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

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(r => EF.Functions.Like(r.Name.ToLower(), $"%{request.Name!.ToLower()}%"));

        if (request.Number.HasValue)
            query = query.Where(gr => gr.Number == request.Number);

        if (request.Date.HasValue)
            query = query.Where(gr => gr.Date.Date == request.Date.Value.Date);
        if (request.DateFrom.HasValue)
            query = query.Where(gr => gr.Date.Date >= request.DateFrom.Value.Date);
        if (request.DateTo.HasValue)
            query = query.Where(gr => gr.Date.Date <= request.DateTo.Value.Date);

        if (!string.IsNullOrEmpty(request.Vendor))
            query = query.Where(gr => gr.CardCode == request.Vendor);

        if (request.Statuses?.Length > 0)
            query = query.Where(gr => request.Statuses.Contains(gr.Status));

        if (request.Confirm.HasValue) {
            query = request.Confirm.Value ? query.Where(gr => gr.Type == GoodsReceiptType.SpecificReceipts) : query.Where(gr => gr.Type != GoodsReceiptType.SpecificReceipts);
        }

        if (!string.IsNullOrWhiteSpace(request.GoodsReceipt) && int.TryParse(request.GoodsReceipt, out int goodsReceipt)) {
            query = query.Where(gr => gr.Documents.Any(d => d.DocNumber == goodsReceipt && d.ObjType == 20));
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaseInvoice) && int.TryParse(request.PurchaseInvoice, out int purchaseInvoice)) {
            query = query.Where(gr => gr.Documents.Any(d => d.DocNumber == purchaseInvoice && d.ObjType == 18));
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaseOrder) && int.TryParse(request.PurchaseOrder, out int purchaseOrder)) {
            query = query.Where(gr => gr.Documents.Any(d => d.DocNumber == purchaseOrder && d.ObjType == 22));
        }

        if (!string.IsNullOrWhiteSpace(request.ReservedInvoice) && int.TryParse(request.ReservedInvoice, out int reservedInvoice)) {
            query = query.Where(gr => gr.Documents.Any(d => d.DocNumber == reservedInvoice && d.ObjType == 18));
        }

        var results = await query.AsNoTracking().OrderByDescending(gr => gr.CreatedAt).ToListAsync();
        return await Task.WhenAll(results.Select(MapToResponse));
    }

    public async Task<GoodsReceiptResponse?> GetGoodsReceipt(Guid id) {
        var goodsReceipt = await db.GoodsReceipts
            .Include(gr => gr.CreatedByUser)
            .Include(gr => gr.Documents)
            .Include(gr => gr.Lines)
            .ThenInclude(l => l.CancellationReason)
            .AsNoTracking()
            .FirstOrDefaultAsync(gr => gr.Id == id);

        return goodsReceipt == null ? null : await MapToResponse(goodsReceipt);
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

        var packages = await db
            .Packages
            .Where(v => v.SourceOperationId == id)
            .ToArrayAsync();
        foreach (var package in packages) {
            await packageService.CancelPackageAsync(package.Id, session, "Goods receipt cancelled");
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

            // if configuration, just set status to finished
            if (goodsReceipt.Type == GoodsReceiptType.SpecificReceipts) {
                goodsReceipt.Status          = ObjectStatus.Finished;
                goodsReceipt.UpdatedAt       = DateTime.UtcNow;
                goodsReceipt.UpdatedByUserId = session.Guid;

                // Close all open lines
                foreach (var line in goodsReceipt.Lines) {
                    line.LineStatus      = LineStatus.Closed;
                    line.UpdatedAt       = DateTime.UtcNow;
                    line.UpdatedByUserId = session.Guid;
                }

                // Activate any packages created during this operation if package feature is enabled
                var activatedPackagesCount = new List<Guid>();
                if (settings.Options.EnablePackages) {
                    activatedPackagesCount = await packageService.ActivatePackagesBySourceAsync(ObjectType.GoodsReceipt, goodsReceipt.Id, session);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ProcessGoodsReceiptResponse {
                    Status            = ResponseStatus.Ok,
                    Success           = true,
                    ActivatedPackages = activatedPackagesCount
                };
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

                // Activate any packages created during this operation if package feature is enabled
                var activatedPackagesCount = new List<Guid>();
                if (settings.Options.EnablePackages) {
                    activatedPackagesCount = await packageService.ActivatePackagesBySourceAsync(ObjectType.GoodsReceipt, goodsReceipt.Id, session);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ProcessGoodsReceiptResponse {
                    Status            = ResponseStatus.Ok,
                    Success           = true,
                    DocumentNumber    = result.DocumentNumber,
                    ActivatedPackages = activatedPackagesCount
                };
            }

            await transaction.RollbackAsync();
            return new ProcessGoodsReceiptResponse {
                Status       = ResponseStatus.Error,
                Success      = false,
                ErrorMessage = result.ErrorMessage
            };
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

    // Helper methods
    private async Task<GoodsReceiptResponse> MapToResponse(GoodsReceipt goodsReceipt) {
        var vendor = !string.IsNullOrWhiteSpace(goodsReceipt.CardCode) ? await adapter.GetVendorAsync(goodsReceipt.CardCode) : null;
        return new GoodsReceiptResponse {
            ID                = goodsReceipt.Id,
            Number            = goodsReceipt.Number,
            Name              = goodsReceipt.Name,
            Vendor            = vendor,
            Date              = goodsReceipt.Date,
            Status            = goodsReceipt.Status,
            Type              = goodsReceipt.Type,
            WhsCode           = goodsReceipt.WhsCode,
            CreatedByUserName = goodsReceipt.CreatedByUser?.FullName,
            Lines = goodsReceipt.Lines.Select(l => new GoodsReceiptLineResponse {
                ID                   = l.Id,
                BarCode              = l.BarCode,
                ItemCode             = l.ItemCode,
                ItemName             = l.ItemCode, // todo Would need item master
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
                ObjectType     = d.ObjType,
            }).ToList()
        };
    }

    private async Task<Dictionary<string, List<GoodsReceiptCreationDataResponse>>> PrepareGoodsReceiptData(GoodsReceipt goodsReceipt) {
        var data = new Dictionary<string, List<GoodsReceiptCreationDataResponse>>();

        foreach (var line in goodsReceipt.Lines) {
            if (!data.ContainsKey(line.ItemCode))
                data[line.ItemCode] = [];

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
                ItemCode    = line.ItemCode,
                BarCode     = line.BarCode,
                Quantity    = line.Quantity,
                UseBaseUnit = line.Unit == UnitType.Unit,
                Date        = line.Date,
                Comments    = line.Comments,
                Sources     = sources
            });
        }

        await adapter.LoadGoodsReceiptItemData(data);

        return data;
    }
}