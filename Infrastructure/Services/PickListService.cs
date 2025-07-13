using Core.DTOs.Package;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListService(SystemDbContext db, IExternalSystemAdapter adapter, ILogger<PickListService> logger, ISettings settings) : IPickListService {
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
        await ValidateAndCloseStalePickLists();

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
        await ValidateAndCloseStalePickLists();

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

        await GetPickListItemDetails(absEntry, request, response, dbPick);

        return response;
    }

    // Get detail items if specific type and entry provided
    private async Task GetPickListItemDetails(int absEntry, PickListDetailRequest request, PickListResponse response, PickList[] dbPick) {
        if (request is not { Type: not null, Entry: not null })
            return;

        var responseDetail = response.Detail!.First(v => v.Type == request.Type.Value && v.Entry == request.Entry.Value);
        var itemParams = new Dictionary<string, object> {
            { "@AbsEntry", absEntry },
            { "@Type", request.Type.Value },
            { "@Entry", request.Entry.Value }
        };

        var items    = await adapter.GetPickingDetailItems(itemParams);
        var itemDict = new Dictionary<string, PickListDetailItemResponse>();

        foreach (var item in items) {
            PickListDetailItemResponse itemResponse;
            if (!itemDict.TryGetValue(item.ItemCode, out var value)) {
                itemResponse = new PickListDetailItemResponse {
                    ItemCode     = item.ItemCode,
                    ItemName     = item.ItemName,
                    Quantity     = item.Quantity,
                    Picked       = item.Picked,
                    OpenQuantity = item.OpenQuantity,
                    NumInBuy     = item.NumInBuy,
                    BuyUnitMsr   = item.BuyUnitMsr,
                    PurPackUn    = item.PurPackUn,
                    PurPackMsr   = item.PurPackMsr,
                    CustomFields = item.CustomFields
                };
                itemDict[item.ItemCode] = itemResponse;
                responseDetail.Items!.Add(itemResponse);
            }
            else {
                itemResponse              =  value;
                itemResponse.Quantity     += item.Quantity;
                itemResponse.Picked       += item.Picked;
                itemResponse.OpenQuantity += item.OpenQuantity;
            }

            int dbPickQty = dbPick.Where(p => p.AbsEntry == absEntry && p.ItemCode == item.ItemCode).Sum(p => p.Quantity);
            itemResponse.Picked       += dbPickQty;
            itemResponse.OpenQuantity -= dbPickQty;
        }

        // Get available bins if requested
        await GetPickListItemDetailsAvailableBins(absEntry, request, itemDict, responseDetail);
    }

    private async Task GetPickListItemDetailsAvailableBins(int absEntry,
        PickListDetailRequest                                  request,
        Dictionary<string, PickListDetailItemResponse>         itemDict,
        PickListDetailResponse                                 responseDetail) {
        if (request.AvailableBins != true) {
            return;
        }

        var binParams = new Dictionary<string, object> {
            { "@AbsEntry", absEntry },
            { "@Type", request.Type.Value },
            { "@Entry", request.Entry.Value }
        };

        if (request.BinEntry.HasValue) {
            binParams.Add("@BinEntry", request.BinEntry.Value);
        }

        var bins = (await adapter.GetPickingDetailItemsBins(binParams)).ToArray();

        // Validate and close stale pick lists before querying
        await ValidateAndCloseStalePickLists();

        var result = db.PickLists
            .Where(p => p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing)
            .Select(p => new { p.ItemCode, p.BinEntry, p.Quantity })
            .Concat(
                db.TransferLines
                    .Where(t => t.LineStatus == LineStatus.Open || t.LineStatus == LineStatus.Processing)
                    .Select(t => new { t.ItemCode, t.BinEntry, t.Quantity })
            )
            .GroupBy(x => new { x.ItemCode, x.BinEntry })
            .Select(g => new {
                g.Key.ItemCode,
                g.Key.BinEntry,
                Quantity = g.Sum(x => x.Quantity)
            });

        string[]                             items      = itemDict.Keys.ToArray();
        int[]                                binEntries = bins.Select(v => v.Entry).Distinct().ToArray();
        BinLocationPackageQuantityResponse[] packages   = [];

        if (enablePackages) {
            var packagesStatuses = new[] { PackageStatus.Active, PackageStatus.Locked };
            packages = await db
                .PackageContents
                .Include(p => p.Package)
                .Where(p => items.Contains(p.ItemCode) && p.BinEntry != null && binEntries.Contains(p.BinEntry.Value) && packagesStatuses.Contains(p.Package.Status))
                .Select(p => new BinLocationPackageQuantityResponse(p.PackageId, p.Package.Barcode, p.BinEntry!.Value, p.ItemCode, p.Quantity))
                .ToArrayAsync();
        }

        foreach (var bin in bins) {
            if (!itemDict.TryGetValue(bin.ItemCode, out var item))
                continue;
            item.BinQuantities ??= [];
            var binResponse = new BinLocationQuantityResponse {
                Entry    = bin.Entry,
                Code     = bin.Code,
                Quantity = bin.Quantity - result.Where(v => v.ItemCode == item.ItemCode && v.BinEntry == bin.Entry).Sum(v => v.Quantity)
            };
            if (binResponse.Quantity <= 0)
                continue;

            if (enablePackages) {
                binResponse.Packages = packages.Where(p => p.BinEntry == bin.Entry && p.ItemCode == bin.ItemCode).ToArray();
            }

            item.BinQuantities.Add(binResponse);
        }


        if (!request.BinEntry.HasValue) {
            return;
        }

        // Calculate available quantities and filter if bin entry specified
        responseDetail.Items!.RemoveAll(v => v.BinQuantities == null || v.OpenQuantity == 0);
        foreach (var item in responseDetail.Items.Where(i => i.BinQuantities != null)) {
            item.Available = item.BinQuantities.Sum(b => b.Quantity);
            item.Packages  = item.BinQuantities.Where(b => b.Packages != null).SelectMany(b => b.Packages).ToArray();
        }
    }



    private async Task ValidateAndCloseStalePickLists() {
        var openPickLists = await db.PickLists
            .Where(p => p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing)
            .Select(p => p.AbsEntry)
            .Distinct()
            .ToArrayAsync();

        if (openPickLists.Length == 0) {
            return;
        }

        // Get all statuses in a single query
        var pickListStatuses = await adapter.GetPickListStatuses(openPickLists);

        // Find closed pick lists
        var closedPickListEntries = pickListStatuses
            .Where(kvp => !kvp.Value)
            .Select(kvp => kvp.Key)
            .ToArray();

        if (closedPickListEntries.Length > 0) {
            // Close all local pick lists that are closed in SAP
            var pickListsToClose = await db.PickLists
                .Where(p => closedPickListEntries.Contains(p.AbsEntry) &&
                            (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
                .ToArrayAsync();

            foreach (var pickList in pickListsToClose) {
                pickList.Status    = ObjectStatus.Closed;
                pickList.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
        }
    }
}