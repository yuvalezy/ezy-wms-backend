using Core.DTOs;
using Core.Interfaces;
using Core.Models;

namespace Adapters.CrossPlatform.SBO;

public class SapBusinessOneServiceLayerAdapter : IExternalSystemAdapter {
    public Task<ExternalValue?> GetUserInfoAsync(string id) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ExternalValue>> GetUsersAsync() {
        throw new NotImplementedException();
    }

    public Task<string?> GetCompanyNameAsync() {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Warehouse>> GetWarehousesAsync(string[]? filter = null) {
        throw new NotImplementedException();
    }

    public Task<Warehouse?> GetWarehouseAsync(string id) {
        throw new NotImplementedException();
    }

    public Task<(int itemCount, int binCount)> GetItemAndBinCount(string warehouse) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ExternalValue>> GetVendorsAsync() {
        throw new NotImplementedException();
    }

    public Task<bool> ValidateVendorsAsync(string id) {
        throw new NotImplementedException();
    }

    public Task<BinLocation?> ScanBinLocationAsync(string bin) {
        throw new NotImplementedException();
    }

    public Task<string?> GetBinCodeAsync(int binEntry) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Item>> ScanItemBarCodeAsync(string scanCode, bool item = false) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string? itemCode, string? barcode) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<BinContent>> BinCheckAsync(int binEntry) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ItemStockResponse>> ItemStockAsync(string itemCode, string whsCode) {
        throw new NotImplementedException();
    }

    public Task<UpdateItemBarCodeResponse> UpdateItemBarCode(UpdateBarCodeRequest request) {
        throw new NotImplementedException();
    }

    public Task<ValidateAddItemResult> GetItemValidationInfo(string itemCode, string barCode, string warehouse, int? binEntry, bool enableBin) {
        throw new NotImplementedException();
    }

    public Task<ProcessTransferResponse> ProcessTransfer(int transferNumber, string whsCode, string? comments, Dictionary<string, TransferCreationData> data) {
        throw new NotImplementedException();
    }

    // Picking methods
    public Task<IEnumerable<PickingDocument>> GetPickLists(PickListsRequest request, string warehouse) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<PickingDetail>> GetPickingDetails(Dictionary<string, object> parameters) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<PickingDetailItem>> GetPickingDetailItems(Dictionary<string, object> parameters) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ItemBinLocationQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters) {
        throw new NotImplementedException();
    }

    public Task<PickingValidationResult[]> ValidatePickingAddItem(PickListAddItemRequest request, Guid userId) {
        throw new NotImplementedException();
    }

    public Task AddPickingItem(PickListAddItemRequest request, Guid employeeId, int pickEntry) {
        throw new NotImplementedException();
    }

    public Task<ProcessPickListResult> ProcessPickList(int absEntry, string warehouse) {
        throw new NotImplementedException();
    }

    // Inventory Counting methods
    public Task<ProcessInventoryCountingResponse> ProcessInventoryCounting(int countingNumber, string warehouse, Dictionary<string, InventoryCountingCreationData> data) {
        throw new NotImplementedException();
    }
}