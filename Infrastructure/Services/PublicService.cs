using Core.DTOs;
using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.DTOs.Settings;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PublicService(IExternalSystemAdapter adapter, ISettings settings, IUserService userService, SystemDbContext db) : IPublicService {
    public async Task<IEnumerable<WarehouseResponse>> GetWarehousesAsync(string[]? filter) {
        var warehouses = await adapter.GetWarehousesAsync(filter);
        return warehouses;
    }

    public async Task<HomeInfoResponse> GetHomeInfoAsync(string warehouse) {
        var itemAndBinCountTask = adapter.GetItemAndBinCount(warehouse);
        var pickingDocumentsTask = adapter.GetPickListsAsync(new PickListsRequest(), warehouse);

        var goodsReceiptResult = db.GoodsReceipts
            .Where(a => a.Status == ObjectStatus.Open || a.Status == ObjectStatus.InProgress);
        int goodsReceiptCount = await goodsReceiptResult.CountAsync(v => v.Type != GoodsReceiptType.SpecificReceipts);
        int receiptConfirmationCount = await goodsReceiptResult.CountAsync(v => v.Type == GoodsReceiptType.SpecificReceipts);

        int countingCount = await db.InventoryCountings.CountAsync(v => v.Status == ObjectStatus.Open || v.Status == ObjectStatus.InProgress);
        int transfersCount = await db.Transfers.CountAsync(v => v.Status == ObjectStatus.Open || v.Status == ObjectStatus.InProgress);

        await Task.WhenAll(itemAndBinCountTask, pickingDocumentsTask);

        var response = await itemAndBinCountTask;
        var pickingDocuments = await pickingDocumentsTask;

        return new HomeInfoResponse {
            BinCheck = response.binCount,
            ItemCheck = response.itemCount,
            GoodsReceipt = goodsReceiptCount,
            ReceiptConfirmation = receiptConfirmationCount,
            Picking = pickingDocuments.Count(),
            Counting = countingCount,
            Transfers = transfersCount
        };
    }

    public async Task<UserInfoResponse> GetUserInfoAsync(SessionInfo info) {
        var user = await userService.GetUserAsync(Guid.Parse(info.UserId));
        return new UserInfoResponse {
            ID               = info.UserId,
            Name             = info.Name,
            CurrentWarehouse = info.Warehouse,
            BinLocations     = info.EnableBinLocations,
            Roles            = info.Roles,
            Warehouses       = await adapter.GetWarehousesAsync(info.SuperUser ? null : user!.Warehouses.ToArray()),
            SuperUser        = info.SuperUser,
            Settings         = settings.Options,
            CustomFields = settings.CustomFields,
        };
    }

    public async Task<IEnumerable<ExternalValue<string>>> GetVendorsAsync() => await adapter.GetVendorsAsync();

    public async Task<BinLocationResponse?> ScanBinLocationAsync(string bin) => await adapter.ScanBinLocationAsync(bin);

    public async Task<IEnumerable<ItemResponse>> ScanItemBarCodeAsync(string scanCode, bool item = false) => await adapter.ScanItemBarCodeAsync(scanCode, item);

    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string? itemCode, string? barcode) => await adapter.ItemCheckAsync(itemCode, barcode);

    public async Task<IEnumerable<BinContentResponse>> BinCheckAsync(int binEntry) => await adapter.BinCheckAsync(binEntry);

    public async Task<IEnumerable<ItemBinStockResponse>> ItemStockAsync(string itemCode, string whsCode) => await adapter.ItemStockAsync(itemCode, whsCode);

    public async Task<UpdateItemBarCodeResponse> UpdateItemBarCode(string userId, UpdateBarCodeRequest request) {
        if (request.AddBarcodes != null) {
            foreach (string barcode in request.AddBarcodes) {
                var check = (await adapter.ScanItemBarCodeAsync(barcode)).FirstOrDefault();
                if (check != null) {
                    return new UpdateItemBarCodeResponse() {
                        ExistItem = check.Code, Status = ResponseStatus.Error
                    };
                }
            }
        }

        var response = await adapter.UpdateItemBarCode(request);
        //todo create logs for the user id
        // item.UserFields.Fields.Item("U_LW_UPDATE_USER").Value      = employeeID;
        // item.UserFields.Fields.Item("U_LW_UPDATE_TIMESTAMP").Value = DateTime.Now;

        return response;
    }
}