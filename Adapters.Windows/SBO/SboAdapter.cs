using Adapters.Windows.SBO.Repositories;
using Core.DTOs;
using Core.Interfaces;
using Core.Models;

namespace Adapters.Windows.SBO;

public class SboAdapter(SboEmployeeRepository employeeRepository, SboGeneralRepository generalRepository, SboItemRepository itemRepository, SboPickingRepository pickingRepository, SboInventoryCountingRepository inventoryCountingRepository)
    : IExternalSystemAdapter {
    public async Task<ExternalValue?>                 GetUserInfoAsync(string id)                                            => await employeeRepository.GetByIdAsync(id);
    public async Task<IEnumerable<ExternalValue>>     GetUsersAsync()                                                        => await employeeRepository.GetAllAsync();
    public async Task<string?>                        GetCompanyNameAsync()                                                  => await generalRepository.GetCompanyNameAsync();
    public async Task<IEnumerable<Warehouse>>         GetWarehousesAsync(string[]? filter = null)                            => await generalRepository.GetWarehousesAsync(filter);
    public async Task<Warehouse?>                     GetWarehouseAsync(string     id)                                       => (await generalRepository.GetWarehousesAsync([id])).FirstOrDefault();
    public async Task<(int itemCount, int binCount)>  GetItemAndBinCount(string    warehouse)                                => await generalRepository.GetItemAndBinCountAsync(warehouse);
    public async Task<IEnumerable<ExternalValue>>     GetVendorsAsync()                                                      => await generalRepository.GetVendorsAsync();
    public async Task<bool>                           ValidateVendorsAsync(string            id)                             => await generalRepository.ValidateVendorsAsync(id);
    public async Task<BinLocation?>                   ScanBinLocationAsync(string            bin)                            => await generalRepository.ScanBinLocationAsync(bin);
    public async Task<IEnumerable<Item>>              ScanItemBarCodeAsync(string            scanCode, bool    item = false) => await itemRepository.ScanItemBarCodeAsync(scanCode, item);
    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string?                 itemCode, string? barcode)      => await itemRepository.ItemCheckAsync(itemCode, barcode);
    public async Task<IEnumerable<BinContent>>        BinCheckAsync(int                      binEntry)                 => await generalRepository.BinCheckAsync(binEntry);
    public async Task<IEnumerable<ItemStockResponse>> ItemStockAsync(string                  itemCode, string whsCode) => await itemRepository.ItemStockAsync(itemCode, whsCode);
    public async Task<UpdateItemBarCodeResponse>      UpdateItemBarCode(UpdateBarCodeRequest request) => await itemRepository.UpdateItemBarCode(request);

    public async Task<ValidateAddItemResult> GetItemValidationInfo(string itemCode, string barCode, string warehouse, int? binEntry, bool enableBin) =>
        await itemRepository.GetItemValidationInfo(itemCode, barCode, warehouse, binEntry, enableBin);

    public Task<ProcessTransferResponse> ProcessTransfer(Guid transferId, string whsCode, string? comments, Dictionary<string, TransferCreationData> data) {
        var response = generalRepository.ProcessTransfer(transferId, whsCode, comments, data);
        return Task.FromResult(response);
    }

    public async Task<IEnumerable<PickingDocument>> GetPickLists(Dictionary<string, object> parameters, string whereClause) {
        return await pickingRepository.GetPickLists(parameters, whereClause);
    }

    public async Task<IEnumerable<PickingDetail>> GetPickingDetails(Dictionary<string, object> parameters) {
        return await pickingRepository.GetPickingDetails(parameters);
    }

    public async Task<IEnumerable<PickingDetailItem>> GetPickingDetailItems(Dictionary<string, object> parameters) {
        return await pickingRepository.GetPickingDetailItems(parameters);
    }

    public async Task<IEnumerable<ItemBinLocationQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters) {
        return await pickingRepository.GetPickingDetailItemsBins(parameters);
    }

    public async Task<PickingValidationResult> ValidatePickingAddItem(PickListAddItemRequest request, Guid userId) {
        return await pickingRepository.ValidatePickingAddItem(request, userId);
    }

    public async Task AddPickingItem(PickListAddItemRequest request, Guid employeeId, int pickEntry) {
        await pickingRepository.AddPickingItem(request, employeeId, pickEntry);
    }

    public async Task<ProcessPickListResult> ProcessPickList(int absEntry, string warehouse) {
        return await pickingRepository.ProcessPickList(absEntry, warehouse);
    }
    
    public async Task ProcessInventoryCounting(int countingNumber, string warehouse) {
        await inventoryCountingRepository.ProcessInventoryCounting(countingNumber, warehouse);
    }
    
    public async Task<IEnumerable<InventoryCountingContentResponse>> GetInventoryCountingContent(Guid countingId, int? binEntry) {
        return await inventoryCountingRepository.GetInventoryCountingContent(countingId, binEntry);
    }
    
    public async Task<InventoryCountingSummaryResponse> GetInventoryCountingSummary(Guid countingId) {
        return await inventoryCountingRepository.GetInventoryCountingSummary(countingId);
    }
}