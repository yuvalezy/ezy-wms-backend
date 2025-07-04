using Core.DTOs;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListService(SystemDbContext db, IExternalSystemAdapter adapter, ILogger<PickListService> logger) : IPickListService {
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
            var values      = dbPick.Where(p => p.AbsEntry == r.Entry).ToArray();
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

        var bins = await adapter.GetPickingDetailItemsBins(binParams);

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
            item.BinQuantities.Add(binResponse);
        }


        if (!request.BinEntry.HasValue) {
            return;
        }

        // Calculate available quantities and filter if bin entry specified
        responseDetail.Items!.RemoveAll(v => v.BinQuantities == null || v.OpenQuantity == 0);
        foreach (var item in responseDetail.Items) {
            if (item.BinQuantities != null) {
                item.Available = item.BinQuantities.Sum(b => b.Quantity);
            }
        }
    }

    public async Task<PickListAddItemResponse> AddItem(SessionInfo sessionInfo, PickListAddItemRequest request) {
        if (request.Unit != UnitType.Unit) {
            var items = await adapter.ItemCheckAsync(request.ItemCode, null);
            var item  = items.FirstOrDefault();
            if (item == null) {
                throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, BarCode = (string?)null });
            }

            request.Quantity *= item.NumInBuy * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
        }

        // Validate the add item request
        var validationResults = await adapter.ValidatePickingAddItem(request);

        if (validationResults.Length == 0) {
            return new PickListAddItemResponse {
                ErrorMessage   = "Item entry not found in pick",
                Status         = ResponseStatus.Error,
            };
        }

        if (!validationResults[0].IsValid)
            return new PickListAddItemResponse {
                ErrorMessage = validationResults[0].ErrorMessage,
                Status       = ResponseStatus.Error
            };

        int result = db.PickLists
            .Where(p => p.ItemCode == request.ItemCode && p.BinEntry == request.BinEntry && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .Select(p => p.Quantity)
            .Concat(
                db.TransferLines
                    .Where(t => t.ItemCode == request.ItemCode && t.BinEntry == request.BinEntry && (t.LineStatus == LineStatus.Open || t.LineStatus == LineStatus.Processing))
                    .Select(t => t.Quantity)
            )
            .Sum();

        int binOnHand = validationResults.First().BinOnHand - result;

        var dbPickedQuantity = await db.PickLists.Where(v => v.AbsEntry == request.ID && v.ItemCode == request.ItemCode && (v.Status == ObjectStatus.Open || v.Status == ObjectStatus.Processing))
            .GroupBy(v => v.PickEntry)
            .Select(v => new { PickEntry = v.Key, Quantity = v.Sum(vv => vv.Quantity) })
            .ToArrayAsync();

        var check = (from v in validationResults.Where(a => a.IsValid)
                join p in dbPickedQuantity on v.PickEntry equals p.PickEntry into gj
                from sub in gj.DefaultIfEmpty()
                where v.OpenQuantity - (sub?.Quantity ?? 0) >= 0
                select new { ValidationResult = v, PickedQuantity = sub?.Quantity ?? 0 })
            .FirstOrDefault();
        if (check == null) {
            return new PickListAddItemResponse {
                Status       = ResponseStatus.Error,
                ErrorMessage = "Quantity exceeds open quantity",
            };
        }

        check.ValidationResult.OpenQuantity -= check.PickedQuantity;

        if (request.Quantity > binOnHand) {
            return new PickListAddItemResponse {
                Status       = ResponseStatus.Error,
                ErrorMessage = "Quantity exceeds bin available stock",
            };
        }

        var pickList = new PickList {
            AbsEntry        = request.ID,
            PickEntry       = check.ValidationResult.PickEntry ?? request.PickEntry ?? 0,
            ItemCode        = request.ItemCode,
            Quantity        = request.Quantity,
            BinEntry        = request.BinEntry,
            Unit            = request.Unit,
            Status          = ObjectStatus.Open,
            SyncStatus      = SyncStatus.Pending,
            CreatedAt       = DateTime.UtcNow,
            CreatedByUserId = sessionInfo.Guid
        };

        await db.PickLists.AddAsync(pickList);
        await db.SaveChangesAsync();

        return PickListAddItemResponse.OkResponse;
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