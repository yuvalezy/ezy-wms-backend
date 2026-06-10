using Core.Constants;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;

namespace Infrastructure.Services;

public class PickListDetailService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    IPickPathSequencer pickPathSequencer) : IPickListDetailService {
    public async Task GetPickListItemDetails(int absEntry, PickListDetailRequest request, PickListResponse response, PickList[] dbPick) {
        foreach (var detail in response.Detail!) {
            decimal dbPickQty = dbPick.Where(p => p.AbsEntry == absEntry && p.PickEntry == detail.Entry).Sum(p => p.Quantity);
            detail.TotalOpenItems -= dbPickQty;
        }

        if (request is { Type: not null, Entry: not null }) {
            var responseDetail = response.Detail!.First(v => v.Type == request.Type.Value && v.Entry == request.Entry.Value);
            await GetPickListItemDetailsExecute(absEntry, request, dbPick, responseDetail.Type, responseDetail.Entry, responseDetail);
            return;
        }

        if (request.PickCheck) {
            foreach (var responseDetail in response.Detail!) {
                await GetPickListItemDetailsExecute(absEntry, request, dbPick, responseDetail.Type, responseDetail.Entry, responseDetail);
            }
        }
    }

    private async Task GetPickListItemDetailsExecute(int absEntry, PickListDetailRequest request, PickList[] dbPick, int type, int entry, PickListDetailResponse responseDetail) {
        var itemParams = new Dictionary<string, object> {
            { "@AbsEntry", absEntry },
            { "@Type", type },
            { "@Entry", entry }
        };

        var items = await adapter.GetPickingDetailItems(itemParams);
        var itemDict = new Dictionary<string, PickListDetailItemResponse>();

        foreach (var item in items) {
            PickListDetailItemResponse itemResponse;
            if (!itemDict.TryGetValue(item.ItemCode, out var value)) {
                itemResponse = new PickListDetailItemResponse {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    Picked = item.Picked,
                    OpenQuantity = item.OpenQuantity,
                    NumInBuy = item.NumInBuy,
                    BuyUnitMsr = item.BuyUnitMsr,
                    PurPackUn = item.PurPackUn,
                    PurPackMsr = item.PurPackMsr,
                    Factor1 = item.Factor1,
                    Factor2 = item.Factor2,
                    Factor3 = item.Factor3,
                    Factor4 = item.Factor4,
                    CustomFields = item.CustomFields
                };

                itemDict[item.ItemCode] = itemResponse;
                responseDetail.Items!.Add(itemResponse);
            }
            else {
                itemResponse = value;
                itemResponse.Quantity += item.Quantity;
                itemResponse.Picked += item.Picked;
                itemResponse.OpenQuantity += item.OpenQuantity;
            }

            decimal dbPickQty = dbPick.Where(p => p.AbsEntry == absEntry && p.ItemCode == item.ItemCode).Sum(p => p.Quantity);
            itemResponse.Picked += dbPickQty;
            itemResponse.OpenQuantity -= dbPickQty;
        }

        // Get available bins if requested
        await GetPickListItemDetailsAvailableBins(absEntry, itemDict, responseDetail, type, entry, request.BinEntry, request.AvailableBins);
    }

    private async Task GetPickListItemDetailsAvailableBins(
        int absEntry,
        Dictionary<string, PickListDetailItemResponse> itemDict,
        PickListDetailResponse responseDetail,
        int type,
        int entry,
        int? binEntry,
        bool? availableBins) {
        if (availableBins != true) {
            return;
        }

        var binParams = new Dictionary<string, object> {
            { "@AbsEntry", absEntry },
            { "@Type", type },
            { "@Entry", entry }
        };

        if (binEntry.HasValue) {
            binParams.Add("@BinEntry", binEntry.Value);
        }

        var bins = (await adapter.GetPickingDetailItemsBins(binParams)).ToArray();

        var result = db.PickLists
        .Where(p => (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing) && p.SyncStatus != SyncStatus.ExternalCancel)
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

            // Item fully picked → none of its bins are relevant any more.
            if (item.OpenQuantity < QuantityTolerances.Completed)
                continue;

            item.BinQuantities ??= [];
            var binResponse = new BinLocationQuantityResponse {
                Entry = bin.Entry,
                Code = bin.Code,
                Sequence = pickPathSequencer.GetSequence(bin.Code),
                Quantity = bin.Quantity - result.Where(v => v.ItemCode == item.ItemCode && v.BinEntry == bin.Entry).Sum(v => v.Quantity)
            };

            if (binResponse.Quantity <= 0)
                continue;

            item.BinQuantities.Add(binResponse);
        }


        if (!binEntry.HasValue) {
            return;
        }

        // Calculate available quantities and filter if bin entry specified
        responseDetail.Items!.RemoveAll(v => v.BinQuantities == null || v.OpenQuantity < QuantityTolerances.Completed);
        foreach (var item in responseDetail.Items.Where(i => i.BinQuantities != null)) {
            item.Available = item.BinQuantities!.Sum(b => b.Quantity);
        }
    }
}
