using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListService(SystemDbContext db, IExternalSystemAdapter adapter, ILogger<PickListService> logger, ISettings settings, IPickListDetailService detailService) : IPickListService {
    private readonly bool enablePackages = settings.Options.EnablePackages;

    public async Task<IEnumerable<PickListResponse>> GetPickLists(PickListsRequest request, string warehouse, bool enableBinLocations) {
        var picks = await adapter.GetPickListsAsync(request, warehouse);
        var response = picks.Select(p => new PickListResponse {
            Entry = p.Entry,
            Date = p.Date,
            SalesOrders = p.SalesOrders,
            Invoices = p.Invoices,
            Transfers = p.Transfers,
            Remarks = p.Remarks,
            Status = p.Status,
            Quantity = p.Quantity,
            OpenQuantity = p.OpenQuantity,
            UpdateQuantity = p.UpdateQuantity,
            PickPackOnly = p.PickPackOnly,
            SyncStatus = SyncStatus.Unknown,
            CheckStarted = false,
            HasCheck = false,
        })
        .ToList();

        int[] entries = response.Select(p => p.Entry).Distinct().ToArray();

        var dbPick = await db.PickLists
        .Where(p => entries.Contains(p.AbsEntry) && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing || p.Status == ObjectStatus.Closed))
        .ToArrayAsync();

        // Get check session info if picking check is enabled
        var checkSessionInfo = settings.Options.EnablePickingCheck
        ? await db.PickListCheckSessions
        .Where(s => entries.Contains(s.PickListId) && !s.Deleted)
        .GroupBy(s => s.PickListId)
        .Select(g => new {
            PickListId = g.Key,
            HasActiveSession = g.Any(s => !s.IsCompleted && !s.IsCancelled)
        })
        .ToDictionaryAsync(x => x.PickListId, x => x.HasActiveSession)
        : new Dictionary<int, bool>();

        foreach (var r in response) {
            var values = dbPick.Where(p => p.AbsEntry == r.Entry).ToArray();
            int pickedQuantity = values.Where(p => p.Status != ObjectStatus.Closed).Sum(p => p.Quantity);
            r.OpenQuantity -= pickedQuantity;
            r.UpdateQuantity += pickedQuantity;
            if (values.Any(v => v.SyncStatus is SyncStatus.Pending or SyncStatus.Failed && v.Status != ObjectStatus.Closed))
                r.SyncStatus = SyncStatus.Pending;

            if (!enableBinLocations) {
                if (values.Any(v => v.SyncStatus == SyncStatus.Synced && v.Status == ObjectStatus.Closed && v.BinEntry == null))
                    r.SyncStatus = SyncStatus.Synced;
            }

            // Check if picking check exists and if it's active
            if (settings.Options.EnablePickingCheck && checkSessionInfo.TryGetValue(r.Entry, out var hasActiveSession)) {
                r.HasCheck = true;
                r.CheckStarted = hasActiveSession;
            }
        }

        if (!request.DisplayCompleted) {
            response.RemoveAll(v => v is { OpenQuantity: 0, CheckStarted: false });
            if (!enableBinLocations) {
                response.RemoveAll(v => v is { SyncStatus: SyncStatus.Synced, CheckStarted: false });
            }
        }

        return response;
    }

    public async Task<PickListResponse?> GetPickList(int absEntry, PickListDetailRequest request, string warehouse) {
        var pickListRequest = new PickListsRequest { ID = absEntry };
        var picks = await adapter.GetPickListsAsync(pickListRequest, warehouse);
        var pick = picks.FirstOrDefault();

        if (pick == null)
            return null;

        var response = new PickListResponse {
            Entry = pick.Entry,
            Date = pick.Date,
            SalesOrders = pick.SalesOrders,
            Invoices = pick.Invoices,
            Transfers = pick.Transfers,
            Remarks = pick.Remarks,
            Status = pick.Status,
            Quantity = pick.Quantity,
            OpenQuantity = pick.OpenQuantity,
            UpdateQuantity = pick.UpdateQuantity,
            PickPackOnly = pick.PickPackOnly,
            Detail = []
        };

        var dbPick = await db.PickLists
        .Where(p => p.AbsEntry == absEntry && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing) && p.SyncStatus != SyncStatus.ExternalCancel)
        .ToArrayAsync();

        int dbPickQty = dbPick.Sum(p => p.Quantity);
        response.OpenQuantity -= dbPickQty;
        response.UpdateQuantity += dbPickQty;

        // Check if picking check has started or exists
        if (settings.Options.EnablePickingCheck) {
            var checkSession = await db.PickListCheckSessions
            .Where(s => s.PickListId == absEntry && !s.Deleted)
            .Select(s => new { s.IsCompleted, s.IsCancelled })
            .FirstOrDefaultAsync();

            if (checkSession != null) {
                response.HasCheck = true;
                response.CheckStarted = !checkSession.IsCompleted && !checkSession.IsCancelled;
            }
        }

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
            var exists = response.Detail.FirstOrDefault(d => d.Type == detail.Type && d.Entry == detail.Entry);
            if (exists == null) {
                detailResponse = new PickListDetailResponse {
                    Type = detail.Type,
                    Entry = detail.Entry,
                    Number = detail.Number,
                    Date = detail.Date,
                    CardCode = detail.CardCode,
                    CardName = detail.CardName,
                    TotalItems = detail.TotalItems,
                    TotalOpenItems = detail.TotalOpenItems,
                    CustomFields = detail.CustomFields,
                    Items = []
                };

                response.Detail.Add(detailResponse);
            }
            else {
                detailResponse = exists;
                detailResponse.TotalItems += detail.TotalItems;
                detailResponse.TotalOpenItems += detail.TotalOpenItems;
            }
        }

        await detailService.GetPickListItemDetails(absEntry, request, response, dbPick);

        return response;
    }
}