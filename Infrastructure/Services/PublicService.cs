using Core.DTOs;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PublicService(IExternalSystemAdapter adapter, ISettings settings, IUserService userService, SystemDbContext db) : IPublicService {
    public async Task<IEnumerable<Warehouse>> GetWarehousesAsync(string[]? filter) {
        var warehouses = await adapter.GetWarehousesAsync(filter);
        return warehouses;
    }

    public async Task<HomeInfoResponse> GetHomeInfoAsync(string warehouse) {
        var homeInfo = new HomeInfoResponse();

        var response = await adapter.GetItemAndBinCount(warehouse);
        homeInfo.BinCheck  = response.binCount;
        homeInfo.ItemCheck = response.itemCount;

        var result = db.GoodsReceipts
            .Where(a => a.Status == ObjectStatus.Open || a.Status == ObjectStatus.InProgress);
        homeInfo.GoodsReceipt        = await result.CountAsync(v => v.Type != GoodsReceiptType.SpecificReceipts);
        homeInfo.ReceiptConfirmation = await result.CountAsync(v => v.Type == GoodsReceiptType.SpecificReceipts);

        var pickingDocuments = await adapter.GetPickListsAsync(new PickListsRequest(), warehouse);
        homeInfo.Picking = pickingDocuments.Count();
        homeInfo.Counting = await db.InventoryCountings.CountAsync(v => v.Status == ObjectStatus.Open || v.Status == ObjectStatus.InProgress);
        homeInfo.Transfers = await db.Transfers.CountAsync(v => v.Status == ObjectStatus.Open || v.Status == ObjectStatus.InProgress);

        return homeInfo;
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
        };
    }

    public async Task<IEnumerable<ExternalValue<string>>> GetVendorsAsync() => await adapter.GetVendorsAsync();

    public async Task<BinLocation?> ScanBinLocationAsync(string bin) => await adapter.ScanBinLocationAsync(bin);

    public async Task<IEnumerable<Item>> ScanItemBarCodeAsync(string scanCode, bool item = false) => await adapter.ScanItemBarCodeAsync(scanCode, item);

    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string? itemCode, string? barcode) => await adapter.ItemCheckAsync(itemCode, barcode);

    public async Task<IEnumerable<BinContent>> BinCheckAsync(int binEntry) => await adapter.BinCheckAsync(binEntry);

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