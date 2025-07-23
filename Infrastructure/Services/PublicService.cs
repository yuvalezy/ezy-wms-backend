using Core.DTOs.General;
using Core.DTOs.Items;
using Core.DTOs.Package;
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

public class PublicService(IExternalSystemAdapter adapter, ISettings settings, IUserService userService, IDeviceService deviceService, SystemDbContext db) : IPublicService
{
    public async Task<IEnumerable<WarehouseResponse>> GetWarehousesAsync(string[]? filter)
    {
        var warehouses = await adapter.GetWarehousesAsync(filter);
        return warehouses;
    }

    public async Task<HomeInfoResponse> GetHomeInfoAsync(string warehouse)
    {
        var itemAndBinCountTask = adapter.GetItemAndBinCount(warehouse);
        var pickingDocumentsTask = adapter.GetPickListsAsync(new PickListsRequest(), warehouse);

        int activePackageCount = await db.Packages.CountAsync(v => v.Status == PackageStatus.Active);
        var goodsReceiptResult = db.GoodsReceipts
        .Where(a => a.Status == ObjectStatus.Open || a.Status == ObjectStatus.InProgress);

        int goodsReceiptCount = await goodsReceiptResult.CountAsync(v => v.Type != GoodsReceiptType.SpecificReceipts);
        int receiptConfirmationCount = await goodsReceiptResult.CountAsync(v => v.Type == GoodsReceiptType.SpecificReceipts);

        int countingCount = await db.InventoryCountings.CountAsync(v => v.Status == ObjectStatus.Open || v.Status == ObjectStatus.InProgress);
        int transfersCount = await db.Transfers.CountAsync(v => v.Status == ObjectStatus.Open || v.Status == ObjectStatus.InProgress);

        await Task.WhenAll(itemAndBinCountTask, pickingDocumentsTask);

        var response = await itemAndBinCountTask;
        var pickingDocuments = await pickingDocumentsTask;

        return new HomeInfoResponse
        {
            BinCheck = response.binCount,
            ItemCheck = response.itemCount,
            PackageCheck = activePackageCount,
            GoodsReceipt = goodsReceiptCount,
            ReceiptConfirmation = receiptConfirmationCount,
            Picking = pickingDocuments.Count(),
            Counting = countingCount,
            Transfers = transfersCount
        };
    }

    public async Task<UserInfoResponse> GetUserInfoAsync(SessionInfo info)
    {
        var user = await userService.GetUserAsync(Guid.Parse(info.UserId));

        // Get device information if available
        string? deviceUuid = info.DeviceUuid;

        Device? device = null;
        if (!string.IsNullOrEmpty(deviceUuid))
        {
            device = await deviceService.GetDeviceAsync(deviceUuid);
        }

        return new UserInfoResponse
        {
            ID = info.UserId,
            Name = info.Name,
            CurrentWarehouse = info.Warehouse,
            BinLocations = info.EnableBinLocations,
            Roles = info.Roles,
            Warehouses = await adapter.GetWarehousesAsync(info.SuperUser ? null : user!.Warehouses.ToArray()),
            SuperUser = info.SuperUser,
            Settings = settings.Options,
            ItemMetaData = settings.Item.MetadataDefinition,
            PackageMetaData = settings.Package.MetadataDefinition,
            CustomFields = settings.CustomFields,
            DeviceStatus = device?.Status
        };
    }

    public async Task<IEnumerable<ExternalValue<string>>> GetVendorsAsync() => await adapter.GetVendorsAsync();

    public async Task<BinLocationResponse?> ScanBinLocationAsync(string bin) => await adapter.ScanBinLocationAsync(bin);

    public async Task<IEnumerable<ItemInfoResponse>> ScanItemBarCodeAsync(string scanCode, bool item = false)
    {
        if (!settings.Options.EnablePackages)
            return await adapter.ScanItemBarCodeAsync(scanCode, item);

        bool prefixCheck = string.IsNullOrWhiteSpace(settings.Package.Barcode.Prefix) || scanCode.StartsWith(settings.Package.Barcode.Prefix);
        bool suffixCheck = string.IsNullOrWhiteSpace(settings.Package.Barcode.Suffix) || scanCode.EndsWith(settings.Package.Barcode.Suffix);
        if (prefixCheck && suffixCheck)
        {
            var package = await db.Packages.FirstOrDefaultAsync(p => p.Barcode == scanCode);
            return package == null ? [] : [new ItemInfoResponse(package.Id.ToString(), true)];
        }

        return await adapter.ScanItemBarCodeAsync(scanCode, item);
    }

    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string? itemCode, string? barcode) => await adapter.ItemCheckAsync(itemCode, barcode);

    public async Task<IEnumerable<BinContentResponse>> BinCheckAsync(int binEntry)
    {
        var response = (await adapter.BinCheckAsync(binEntry)).ToArray();
        var packageStatuses = new[] { PackageStatus.Active, PackageStatus.Locked };
        var packages = await db.PackageContents
        .Include(v => v.Package)
        .Where(v => v.BinEntry == binEntry && packageStatuses.Contains(v.Package.Status))
        .OrderBy(v => v.Package.CreatedAt)
        .ToArrayAsync();

        foreach (var value in response)
        {
            var itemPackages = packages.Where(v => v.ItemCode == value.ItemCode).ToArray();
            if (itemPackages.Length > 0)
                value.Packages = itemPackages
                .Select(v => new PackageStockValue(v.PackageId, v.Package.Barcode, (int)v.Quantity))
                .ToArray();
        }

        return response;
    }

    public async Task<IEnumerable<ItemBinStockResponse>> ItemStockAsync(string itemCode, string whsCode)
    {
        var response = (await adapter.ItemStockAsync(itemCode, whsCode)).ToArray();
        int[] bins = response.Select(v => v.BinEntry).ToArray();
        var packagesStatuses = new[] { PackageStatus.Active, PackageStatus.Locked };
        var packages = await db.PackageContents
        .Include(v => v.Package)
        .Where(v => v.BinEntry.HasValue && bins.Contains(v.BinEntry.Value) && v.ItemCode == itemCode && packagesStatuses.Contains(v.Package.Status))
        .OrderBy(v => v.Package.CreatedAt)
        .ToArrayAsync();

        foreach (var value in response)
        {
            var binPackages = packages.Where(v => v.BinEntry!.Value == value.BinEntry).ToArray();
            if (binPackages.Length > 0)
                value.Packages = binPackages
                .Select(v => new PackageStockValue(v.PackageId, v.Package.Barcode, (int)v.Quantity))
                .ToArray();
        }

        return response;
    }

    public async Task<UpdateItemBarCodeResponse> UpdateItemBarCode(string userId, UpdateBarCodeRequest request)
    {
        if (request.AddBarcodes != null)
        {
            foreach (string barcode in request.AddBarcodes)
            {
                var check = (await adapter.ScanItemBarCodeAsync(barcode)).FirstOrDefault();
                if (check != null)
                {
                    return new UpdateItemBarCodeResponse()
                    {
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