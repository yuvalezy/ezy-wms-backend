using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListService(SystemDbContext db, IExternalSystemAdapter adapter, ILogger<PickListService> logger, ISettings settings, PickListDetailService detailService) : IPickListService {
    private readonly bool enablePackages = settings.Options.EnablePackages;

    public async Task<IEnumerable<PickListResponse>> GetPickLists(PickListsRequest request, string warehouse) {
        var picks = await adapter.GetPickListsAsync(request, warehouse);
        var response = picks.Select(p => new PickListResponse {
                Entry          = p.Entry,
                Date           = p.Date,
                SalesOrders    = p.SalesOrders,
                Invoices       = p.Invoices,
                Transfers      = p.Transfers,
                Remarks        = p.Remarks,
                Status         = p.Status,
                Quantity       = p.Quantity,
                OpenQuantity   = p.OpenQuantity,
                UpdateQuantity = p.UpdateQuantity,
                PickPackOnly   = p.PickPackOnly,
                SyncStatus     = SyncStatus.Synced,
            })
            .ToArray();
        int[] entries = response.Select(p => p.Entry).Distinct().ToArray();

        // Validate and close stale pick lists before calculating quantities
        await detailService.ValidateAndCloseStalePickLists();

        var dbPick = await db.PickLists
            .Where(p => entries.Contains(p.AbsEntry) && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .ToArrayAsync();
        foreach (var r in response) {
            var values         = dbPick.Where(p => p.AbsEntry == r.Entry).ToArray();
            int pickedQuantity = values.Sum(p => p.Quantity);
            r.OpenQuantity   -= pickedQuantity;
            r.UpdateQuantity += pickedQuantity;
            if (values.Any(v => v.SyncStatus is SyncStatus.Pending or SyncStatus.Failed))
                r.SyncStatus = SyncStatus.Pending;
        }

        return response;
    }

    public async Task<PickListResponse?> GetPickList(int absEntry, PickListDetailRequest request, string warehouse) {
        var pickListRequest = new PickListsRequest { ID = absEntry };
        var picks           = await adapter.GetPickListsAsync(pickListRequest, warehouse);
        var pick            = picks.FirstOrDefault();

        if (pick == null)
            return null;

        var response = new PickListResponse {
            Entry          = pick.Entry,
            Date           = pick.Date,
            SalesOrders    = pick.SalesOrders,
            Invoices       = pick.Invoices,
            Transfers      = pick.Transfers,
            Remarks        = pick.Remarks,
            Status         = pick.Status,
            Quantity       = pick.Quantity,
            OpenQuantity   = pick.OpenQuantity,
            UpdateQuantity = pick.UpdateQuantity,
            PickPackOnly   = pick.PickPackOnly,
            Detail         = []
        };

        // Validate and close stale pick lists before calculating quantities
        await detailService.ValidateAndCloseStalePickLists();

        var dbPick = await db.PickLists
            .Where(p => p.AbsEntry == absEntry && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .ToArrayAsync();
        int dbPickQty = dbPick.Sum(p => p.Quantity);
        response.OpenQuantity   -= dbPickQty;
        response.UpdateQuantity += dbPickQty;

        // Get picking details
        var detailParams = new Dictionary<string, object> {
            { "@AbsEntry", absEntry }
        };

        if (request.Type.HasValue) {
            detailParams.Add("@Type", request.Type.Value);
        }

        if (request.Entry.HasValue) {
            detailParams.Add("@Entry", request.Entry.Value);
        }

        var details = await adapter.GetPickingDetails(detailParams);

        foreach (var detail in details) {
            detail.TotalOpenItems -= dbPick.Where(p => p.AbsEntry == response.Entry && p.PickEntry == detail.PickEntry).Sum(p => p.Quantity);

            PickListDetailResponse detailResponse;
            var                    exists = response.Detail.FirstOrDefault(d => d.Type == detail.Type && d.Entry == detail.Entry);
            if (exists == null) {
                detailResponse = new PickListDetailResponse {
                    Type           = detail.Type,
                    Entry          = detail.Entry,
                    Number         = detail.Number,
                    Date           = detail.Date,
                    CardCode       = detail.CardCode,
                    CardName       = detail.CardName,
                    TotalItems     = detail.TotalItems,
                    TotalOpenItems = detail.TotalOpenItems,
                    Items          = []
                };
                response.Detail.Add(detailResponse);
            }
            else {
                detailResponse                =  exists;
                detailResponse.TotalItems     += detail.TotalItems;
                detailResponse.TotalOpenItems += detail.TotalOpenItems;
            }
        }

        await detailService.GetPickListItemDetails(absEntry, request, response, dbPick);

        return response;
    }
}