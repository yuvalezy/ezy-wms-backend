using System.ComponentModel.DataAnnotations;
using Core.DTOs.General;
using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.DTOs.Settings;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PublicService(IExternalSystemAdapter adapter, ISettings settings, IUserService userService, IDeviceService deviceService, SystemDbContext db) : IPublicService {
    public async Task<IEnumerable<WarehouseResponse>> GetWarehousesAsync(string[]? filter) {
        var warehouses = await adapter.GetWarehousesAsync(filter);
        return warehouses;
    }

    public async Task<IEnumerable<BinLocationResponse>> GetWarehouseBinsAsync(string warehouse) => await adapter.GetBinsAsync(warehouse);

    public async Task<HomeInfoResponse> GetHomeInfoAsync(string warehouse) {
        var itemAndBinCountTask = adapter.GetItemAndBinCount(warehouse);
        var pickingDocumentsTask = adapter.GetPickListsAsync(new PickListsRequest(), warehouse);

        var goodsReceiptResult = db.GoodsReceipts
        .Where(a => (a.Status == ObjectStatus.Open || a.Status == ObjectStatus.InProgress) && a.WhsCode == warehouse);

        int goodsReceiptCount = await goodsReceiptResult.CountAsync(v => v.Type != GoodsReceiptType.SpecificReceipts && v.Type != GoodsReceiptType.SpecificTransfers);
        int receiptConfirmationCount = await goodsReceiptResult.CountAsync(v => v.Type == GoodsReceiptType.SpecificReceipts);
        int transferConfirmationCount = await goodsReceiptResult.CountAsync(v => v.Type == GoodsReceiptType.SpecificTransfers);

        int countingCount = await db.InventoryCountings.CountAsync(v => (v.Status == ObjectStatus.Open || v.Status == ObjectStatus.InProgress) && v.WhsCode == warehouse);
        int transfersCount = await db.Transfers.CountAsync(v => (v.Status == ObjectStatus.Open || v.Status == ObjectStatus.InProgress) && v.WhsCode == warehouse);;
        int transfersApprovalCount = await db.Transfers.CountAsync(v => (v.Status == ObjectStatus.WaitingForApproval) && v.WhsCode == warehouse);;

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
            Transfers = transfersCount,
            TransfersApproval = transfersApprovalCount,
            TransfersConfirmation = transferConfirmationCount
        };
    }

    public async Task<UserInfoResponse> GetUserInfoAsync(SessionInfo info) {
        var user = await userService.GetUserAsync(Guid.Parse(info.UserId));

        // Get device information if available
        string? deviceUuid = info.DeviceUuid;

        Device? device = null;
        if (!string.IsNullOrEmpty(deviceUuid)) {
            device = await deviceService.GetDeviceAsync(deviceUuid);
        }

        return new UserInfoResponse {
            ID = info.UserId,
            Name = info.Name,
            CurrentWarehouse = info.Warehouse,
            BinLocations = info.EnableBinLocations,
            Roles = info.Roles,
            Warehouses = await adapter.GetWarehousesAsync(info.SuperUser ? null : user!.Warehouses.ToArray()),
            SuperUser = info.SuperUser,
            Settings = settings.Options,
            ItemMetaData = settings.Item.MetadataDefinition,
            CustomFields = settings.CustomFields,
            DeviceStatus = device?.Status,
            DeviceName   = device?.DeviceName
        };
    }

    public async Task<string> UpdateMyDeviceNameAsync(SessionInfo info, string newName) {
        // The device UUID comes from the trusted session/header, never from the
        // request body, so a user can only rename their own connected device.
        if (string.IsNullOrEmpty(info.DeviceUuid)) {
            throw new ValidationException("No device is associated with the current session");
        }

        var device = await deviceService.UpdateDeviceNameAsync(info.DeviceUuid, newName, info);
        return device.DeviceName;
    }

    public async Task<IEnumerable<ExternalValue<string>>> GetVendorsAsync() => await adapter.GetVendorsAsync();

    public async Task<BinLocationResponse?> ScanBinLocationAsync(string bin) => await adapter.ScanBinLocationAsync(bin);

    public async Task<IEnumerable<ItemInfoResponse>> ScanItemBarCodeAsync(string scanCode, bool item = false) => await adapter.ScanItemBarCodeAsync(scanCode, item);

    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string? itemCode, string? barcode) => await adapter.ItemCheckAsync(itemCode, barcode);

    public async Task<IEnumerable<BinContentResponse>> BinCheckAsync(int binEntry) => await adapter.BinCheckAsync(binEntry);

    public async Task<IEnumerable<ItemStockResponse>> ItemStockAsync(string itemCode, string warehouse) => await adapter.ItemStockAsync(itemCode, warehouse);

    public async Task<IEnumerable<ItemBinStockResponse>> ItemBinStockAsync(string itemCode, string whsCode) => await adapter.ItemBinStockAsync(itemCode, whsCode);

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
