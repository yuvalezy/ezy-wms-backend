using Adapters.Windows.SBO.Repositories;
using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace Adapters.Windows.SBO;

public class SboAdapter(
    SboEmployeeRepository          employeeRepository,
    SboGeneralRepository           generalRepository,
    SboItemRepository              itemRepository,
    SboPickingRepository           pickingRepository,
    SboInventoryCountingRepository inventoryCountingRepository,
    SboGoodsReceiptRepository      goodsReceiptRepository) : IExternalSystemAdapter {
    // General 
    public async Task<string?> GetCompanyNameAsync() => await generalRepository.GetCompanyNameAsync();

    // Vendor
    public async Task<IEnumerable<ExternalValue<string>>> GetVendorsAsync()                     => await generalRepository.GetVendorsAsync();
    public async Task<ExternalValue<string>?>             GetVendorAsync(string       cardCode) => await generalRepository.GetVendorAsync(cardCode);
    public async Task<bool>                               ValidateVendorsAsync(string id)       => await generalRepository.ValidateVendorsAsync(id);

    // Users
    public async Task<ExternalValue<string>?>             GetUserInfoAsync(string id) => await employeeRepository.GetByIdAsync(id);
    public async Task<IEnumerable<ExternalValue<string>>> GetUsersAsync()             => await employeeRepository.GetAllAsync();

    // Warehouses
    public async Task<IEnumerable<Warehouse>> GetWarehousesAsync(string[]? filter = null) => await generalRepository.GetWarehousesAsync(filter);
    public async Task<Warehouse?>             GetWarehouseAsync(string     id)            => (await generalRepository.GetWarehousesAsync([id])).FirstOrDefault();

    // Items, Warehouse & Bins
    public async Task<(int itemCount, int binCount)>                  GetItemAndBinCount(string       warehouse)                      => await generalRepository.GetItemAndBinCountAsync(warehouse);
    public async Task<BinLocation?>                                   ScanBinLocationAsync(string     bin)                            => await generalRepository.ScanBinLocationAsync(bin);
    public async Task<string?>                                        GetBinCodeAsync(int             binEntry)                       => await generalRepository.GetBinCodeAsync(binEntry);
    public async Task<IEnumerable<Item>>                              ScanItemBarCodeAsync(string     scanCode, bool    item = false) => await itemRepository.ScanItemBarCodeAsync(scanCode, item);
    public async Task<IEnumerable<ItemCheckResponse>>                 ItemCheckAsync(string?          itemCode, string? barcode)      => await itemRepository.ItemCheckAsync(itemCode, barcode);
    public async Task<IEnumerable<BinContent>>                        BinCheckAsync(int               binEntry)                    => await generalRepository.BinCheckAsync(binEntry);
    public async Task<IEnumerable<ItemBinStockResponse>>              ItemStockAsync(string           itemCode,  string   whsCode) => await itemRepository.ItemBinStockAsync(itemCode, whsCode);
    public async Task<Dictionary<string, ItemWarehouseStockResponse>> ItemsWarehouseStockAsync(string warehouse, string[] items)   => await itemRepository.ItemsWarehouseStockAsync(warehouse, items);

    public async Task<UpdateItemBarCodeResponse> UpdateItemBarCode(UpdateBarCodeRequest request) => await itemRepository.UpdateItemBarCode(request);

    public async Task<ValidateAddItemResult> GetItemValidationInfo(string itemCode, string barCode, string warehouse, int? binEntry, bool enableBin) =>
        await itemRepository.GetItemValidationInfo(itemCode, barCode, warehouse, binEntry, enableBin);

    // Transfers
    public async Task<ProcessTransferResponse> ProcessTransfer(int transferNumber, string whsCode, string? comments, Dictionary<string, TransferCreationData> data) =>
        await generalRepository.ProcessTransfer(transferNumber, whsCode, comments, data);

    // Pick List
    public async Task<IEnumerable<PickingDocument>> GetPickListsAsync(PickListsRequest request, string warehouse) => await pickingRepository.GetPickLists(request, warehouse);

    public async Task<IEnumerable<PickingDetail>> GetPickingDetails(Dictionary<string, object> parameters) => await pickingRepository.GetPickingDetails(parameters);

    public async Task<IEnumerable<PickingDetailItem>> GetPickingDetailItems(Dictionary<string, object> parameters) => await pickingRepository.GetPickingDetailItems(parameters);

    public async Task<IEnumerable<ItemBinLocationQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters) => await pickingRepository.GetPickingDetailItemsBins(parameters);

    public async Task<PickingValidationResult[]> ValidatePickingAddItem(PickListAddItemRequest request, Guid userId) => await pickingRepository.ValidatePickingAddItem(request, userId);

    public async Task<ProcessPickListResult> ProcessPickList(int absEntry, string warehouse, List<PickList> data) => await pickingRepository.ProcessPickList(absEntry, warehouse, data);

    public async Task<Dictionary<int, bool>> GetPickListStatuses(int[] absEntries) => await pickingRepository.GetPickListStatuses(absEntries);

    //Inventory Counting
    public async Task<ProcessInventoryCountingResponse> ProcessInventoryCounting(int countingNumber, string warehouse, Dictionary<string, InventoryCountingCreationData> data) {
        int series = await generalRepository.GetSeries("1470000065");
        return await inventoryCountingRepository.ProcessInventoryCounting(countingNumber, warehouse, data, series);
    }

    // Goods Receipt methods
    public async Task<GoodsReceiptValidationResult> ValidateGoodsReceiptAddItem(GoodsReceiptAddItemRequest request, List<ObjectKey> specificDocuments, Guid userId, string warehouse) {
        return await goodsReceiptRepository.ValidateGoodsReceiptAddItem(request, warehouse, specificDocuments);
    }

    public async Task<ProcessGoodsReceiptResult> ProcessGoodsReceipt(int number, string warehouse, Dictionary<string, List<GoodsReceiptCreationData>> data) {
        int series = await generalRepository.GetSeries("20");
        return await goodsReceiptRepository.ProcessGoodsReceipt(number, warehouse, data, series);
    }

    public async Task ValidateGoodsReceiptDocuments(string warehouse, GoodsReceiptType type, List<DocumentParameter> documents) {
        await goodsReceiptRepository.ValidateGoodsReceiptDocuments(warehouse, type, documents);
    }

    public async Task<IEnumerable<GoodsReceiptAddItemSourceDocument>> AddItemSourceDocuments(GoodsReceiptAddItemRequest request, string warehouse, GoodsReceiptType type, string cardCode, List<ObjectKey> specificDocuments) {
        return await goodsReceiptRepository.AddItemSourceDocuments(request, warehouse, type, cardCode, specificDocuments);
    }
}