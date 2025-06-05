using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PickListService(SystemDbContext db, IExternalSystemAdapter adapter) : IPickListService {
    public async Task<IEnumerable<PickListResponse>> GetPickLists(PickListsRequest request, string warehouse) {
        var picks = await adapter.GetPickLists(request, warehouse);
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
                UpdateQuantity = p.UpdateQuantity
            })
            .ToArray();
        int[] entries = response.Select(p => p.Entry).Distinct().ToArray();
        var dbPick = await db.PickLists
            .Where(p => entries.Contains(p.AbsEntry) && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .ToArrayAsync();
        foreach (var r in response) {
            r.OpenQuantity -= dbPick.Where(p => p.AbsEntry == r.Entry).Sum(p => p.Quantity);
        }

        return response;
    }

    public async Task<PickListResponse?> GetPickList(int absEntry, PickListDetailRequest request, string warehouse) {
        var pickListRequest = new PickListsRequest { ID = absEntry };
        var picks           = await adapter.GetPickLists(pickListRequest, warehouse);
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
            Detail         = []
        };

        var dbPick = await db.PickLists
            .Where(p => p.AbsEntry == absEntry && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .ToArrayAsync();
        response.OpenQuantity -= dbPick.Sum(p => p.Quantity);

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

        var details        = await adapter.GetPickingDetails(detailParams);

        foreach (var detail in details) {
            detail.TotalOpenItems -= dbPick.Where(p => p.AbsEntry == response.Entry && p.PickEntry == detail.PickEntry).Sum(p => p.Quantity);

            PickListDetailResponse detailResponse; 
            var exists = response.Detail.FirstOrDefault(d => d.Type == detail.Type && d.Entry == detail.Entry);
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
                detailResponse            =  exists;
                detailResponse.TotalItems += detail.TotalItems;
                detailResponse.TotalOpenItems += detail.TotalOpenItems;
            }


            // // Get detail items if specific type and entry provided
            // if (request is { Type: not null, Entry: not null }) {
            //     var itemParams = new Dictionary<string, object> {
            //         { "@AbsEntry", absEntry },
            //         { "@Type", request.Type.Value },
            //         { "@Entry", request.Entry.Value }
            //     };
            //
            //     var items    = await adapter.GetPickingDetailItems(itemParams);
            //     var itemDict = new Dictionary<string, PickListDetailItemResponse>();
            //
            //     foreach (var item in items) {
            //         PickListDetailItemResponse itemResponse;
            //         if (!itemDict.ContainsKey(item.ItemCode)) {
            //             itemResponse = new PickListDetailItemResponse {
            //                 ItemCode     = item.ItemCode,
            //                 ItemName     = item.ItemName,
            //                 Quantity     = item.Quantity,
            //                 Picked       = item.Picked,
            //                 OpenQuantity = item.OpenQuantity,
            //                 NumInBuy     = item.NumInBuy,
            //                 BuyUnitMsr   = item.BuyUnitMsr,
            //                 PurPackUn    = item.PurPackUn,
            //                 PurPackMsr   = item.PurPackMsr
            //             };
            //             itemDict[item.ItemCode] = itemResponse;
            //             detailResponse.Items!.Add(itemResponse);
            //         }
            //         else {
            //             itemResponse = itemDict[item.ItemCode];
            //             itemResponse.Quantity += item.Quantity;
            //             itemResponse.OpenQuantity += item.OpenQuantity;
            //         }
            //
            //         // itemResponse.OpenQuantity -= dbPick.Where(p => p.AbsEntry == detail.Entry && p.PickEntry == detail.PickEntry).Sum(p => p.Quantity);
            //     }
            //
            //     // Get available bins if requested
            //     if (request.AvailableBins == true) {
            //         var binParams = new Dictionary<string, object> {
            //             { "@AbsEntry", absEntry },
            //             { "@Type", request.Type.Value },
            //             { "@Entry", request.Entry.Value }
            //         };
            //
            //         if (request.BinEntry.HasValue) {
            //             binParams.Add("@BinEntry", request.BinEntry.Value);
            //         }
            //
            //         var bins = await adapter.GetPickingDetailItemsBins(binParams);
            //
            //         foreach (var bin in bins) {
            //             if (!itemDict.TryGetValue(bin.ItemCode, out var item))
            //                 continue;
            //             item.BinQuantities ??= [];
            //             item.BinQuantities.Add(new BinLocationQuantityResponse {
            //                 Entry    = bin.Entry,
            //                 Code     = bin.Code,
            //                 Quantity = bin.Quantity
            //             });
            //         }
            //
            //         // Calculate available quantities and filter if bin entry specified
            //         if (request.BinEntry.HasValue) {
            //             detailResponse.Items.RemoveAll(v => v.BinQuantities == null || v.OpenQuantity == 0);
            //             foreach (var item in detailResponse.Items) {
            //                 if (item.BinQuantities != null) {
            //                     item.Available = item.BinQuantities.Sum(b => b.Quantity);
            //                 }
            //             }
            //         }
            //     }
            // }
        }

        return response;
    }

    public async Task<PickListAddItemResponse> AddItem(SessionInfo sessionInfo, PickListAddItemRequest request) {
        // Validate the add item request
        var validationResult = await adapter.ValidatePickingAddItem(request, sessionInfo.Guid);

        if (!validationResult.IsValid) {
            return new PickListAddItemResponse {
                Status         = ResponseStatus.Error,
                ErrorMessage   = validationResult.ErrorMessage,
                ClosedDocument = validationResult.ReturnValue == -6
            };
        }

        // Create pick list entry
        int quantity = request.Quantity;
        if (request.Unit != UnitType.Unit) {
            var items = await adapter.ItemCheckAsync(request.ItemCode, null);
            var item  = items.FirstOrDefault();
            if (item == null) {
                throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, BarCode = (string?)null });
            }

            quantity *= item.NumInBuy * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
        }

        var pickList = new PickList {
            AbsEntry        = request.ID,
            PickEntry       = validationResult.PickEntry ?? request.PickEntry ?? 0,
            ItemCode        = request.ItemCode,
            Quantity        = quantity,
            BinEntry        = request.BinEntry,
            Unit            = request.Unit,
            Status          = ObjectStatus.Open,
            CreatedAt       = DateTime.UtcNow,
            CreatedByUserId = sessionInfo.Guid
        };

        await db.PickLists.AddAsync(pickList);
        await db.SaveChangesAsync();

        return PickListAddItemResponse.OkResponse;
    }

    public async Task<ProcessPickListResponse> ProcessPickList(int absEntry, SessionInfo sessionInfo) {
        try {
            var result = await adapter.ProcessPickList(absEntry, sessionInfo.Warehouse);

            return new ProcessPickListResponse {
                Status         = ResponseStatus.Ok,
                DocumentNumber = result.DocumentNumber
            };
        }
        catch (Exception ex) {
            return new ProcessPickListResponse {
                Status       = ResponseStatus.Error,
                Message      = "Failed to process pick list",
                ErrorMessage = ex.Message
            };
        }
    }
}