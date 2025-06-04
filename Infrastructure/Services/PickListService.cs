using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Infrastructure.Services;

public class PickListService(SystemDbContext db, IExternalSystemAdapter adapter) : IPickListService {
    public async Task<IEnumerable<PickListResponse>> GetPickLists(PickListsRequest request, string warehouse) {
        var parameters = new Dictionary<string, object> {
            { "@WhsCode", warehouse }
        };
        
        var whereClause = new StringBuilder();
        
        if (request.ID.HasValue) {
            parameters.Add("@AbsEntry", request.ID.Value);
            whereClause.AppendLine(" and PICKS.\"AbsEntry\" = @AbsEntry ");
        }
        
        if (request.Date.HasValue) {
            parameters.Add("@Date", request.Date.Value);
            whereClause.AppendLine(" and DATEDIFF(day,PICKS.\"U_StatusDate\",@Date) = 0 ");
        }
        
        if (request.Statuses?.Length > 0) {
            whereClause.AppendLine("and PICKS.\"Status\" in (");
            for (int i = 0; i < request.Statuses.Length; i++) {
                if (i > 0)
                    whereClause.Append(", ");
                whereClause.Append($"'{(char)request.Statuses[i]}'");
            }
            whereClause.Append(") ");
        }
        
        var picks = await adapter.GetPickLists(parameters, whereClause.ToString());
        return picks.Select(p => new PickListResponse {
            Entry = p.Entry,
            Date = p.Date,
            SalesOrders = p.SalesOrders,
            Invoices = p.Invoices,
            Transfers = p.Transfers,
            Remarks = p.Remarks,
            Status = p.Status,
            Quantity = p.Quantity,
            OpenQuantity = p.OpenQuantity,
            UpdateQuantity = p.UpdateQuantity
        });
    }
    
    public async Task<PickListResponse> GetPickList(int absEntry, PickListDetailRequest request) {
        var parameters = new Dictionary<string, object> {
            { "@AbsEntry", absEntry }
        };
        
        var picks = await adapter.GetPickLists(parameters, " and PICKS.\"AbsEntry\" = @AbsEntry ");
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
            Detail = new List<PickListDetailResponse>()
        };
        
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
            var detailResponse = new PickListDetailResponse {
                Type = detail.Type,
                Entry = detail.Entry,
                Number = detail.Number,
                Date = detail.Date,
                CardCode = detail.CardCode,
                CardName = detail.CardName,
                TotalItems = detail.TotalItems,
                TotalOpenItems = detail.TotalOpenItems,
                Items = new List<PickListDetailItemResponse>()
            };
            
            // Get detail items if specific type and entry provided
            if (request.Type.HasValue && request.Entry.HasValue) {
                var itemParams = new Dictionary<string, object> {
                    { "@AbsEntry", absEntry },
                    { "@Type", request.Type.Value },
                    { "@Entry", request.Entry.Value }
                };
                
                var items = await adapter.GetPickingDetailItems(itemParams);
                var itemDict = new Dictionary<string, PickListDetailItemResponse>();
                
                foreach (var item in items) {
                    var itemResponse = new PickListDetailItemResponse {
                        ItemCode = item.ItemCode,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        Picked = item.Picked,
                        OpenQuantity = item.OpenQuantity,
                        NumInBuy = item.NumInBuy,
                        BuyUnitMsr = item.BuyUnitMsr,
                        PurPackUn = item.PurPackUn,
                        PurPackMsr = item.PurPackMsr
                    };
                    
                    itemDict[item.ItemCode] = itemResponse;
                    detailResponse.Items.Add(itemResponse);
                }
                
                // Get available bins if requested
                if (request.AvailableBins == true) {
                    var binParams = new Dictionary<string, object> {
                        { "@AbsEntry", absEntry },
                        { "@Type", request.Type.Value },
                        { "@Entry", request.Entry.Value }
                    };
                    
                    if (request.BinEntry.HasValue) {
                        binParams.Add("@BinEntry", request.BinEntry.Value);
                    }
                    
                    var bins = await adapter.GetPickingDetailItemsBins(binParams);
                    
                    foreach (var bin in bins) {
                        if (itemDict.TryGetValue(bin.ItemCode, out var item)) {
                            item.BinQuantities ??= new List<BinLocationQuantityResponse>();
                            item.BinQuantities.Add(new BinLocationQuantityResponse {
                                Entry = bin.Entry,
                                Code = bin.Code,
                                Quantity = bin.Quantity
                            });
                        }
                    }
                    
                    // Calculate available quantities and filter if bin entry specified
                    if (request.BinEntry.HasValue) {
                        detailResponse.Items.RemoveAll(v => v.BinQuantities == null || v.OpenQuantity == 0);
                    }
                    
                    foreach (var item in detailResponse.Items) {
                        if (item.BinQuantities != null) {
                            item.Available = item.BinQuantities.Sum(b => b.Quantity);
                        }
                    }
                }
            }
            
            response.Detail.Add(detailResponse);
        }
        
        return response;
    }
    
    public async Task<PickListAddItemResponse> AddItem(SessionInfo sessionInfo, PickListAddItemRequest request) {
        // Validate the add item request
        var validationResult = await adapter.ValidatePickingAddItem(request, sessionInfo.Guid);
        
        if (!validationResult.IsValid) {
            return new PickListAddItemResponse {
                Status = ResponseStatus.Error,
                ErrorMessage = validationResult.ErrorMessage,
                ClosedDocument = validationResult.ReturnValue == -6
            };
        }
        
        // Create pick list entry
        var pickList = new PickList {
            AbsEntry = request.ID,
            PickEntry = validationResult.PickEntry ?? request.PickEntry ?? 0,
            ItemCode = request.ItemCode,
            Quantity = request.Quantity,
            BinEntry = request.BinEntry,
            Status = ObjectStatus.Open
        };
        
        await db.PickLists.AddAsync(pickList);
        await db.SaveChangesAsync();
        
        // Execute the add item in SAP
        await adapter.AddPickingItem(request, sessionInfo.Guid, pickList.PickEntry);
        
        return PickListAddItemResponse.OkResponse;
    }
    
    public async Task<ProcessPickListResponse> ProcessPickList(int absEntry, SessionInfo sessionInfo) {
        try {
            var result = await adapter.ProcessPickList(absEntry, sessionInfo.Warehouse);
            
            return new ProcessPickListResponse {
                Status = ResponseStatus.Ok,
                DocumentNumber = result.DocumentNumber
            };
        }
        catch (Exception ex) {
            return new ProcessPickListResponse {
                Status = ResponseStatus.Error,
                Message = "Failed to process pick list",
                ErrorMessage = ex.Message
            };
        }
    }
}