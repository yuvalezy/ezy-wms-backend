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

    public Task<ValidateAddItemTransferResult> ValidateAddItemTransfer(string itemCode, string barCode, string warehouse, int? binEntry) {
        throw new NotImplementedException();
    }
}