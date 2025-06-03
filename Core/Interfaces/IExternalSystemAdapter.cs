using Core.DTOs;
using Core.Models;

namespace Core.Interfaces;

public interface IExternalSystemAdapter {
    Task<ExternalValue?>                 GetUserInfoAsync(string id);
    Task<IEnumerable<ExternalValue>>     GetUsersAsync();
    Task<string?>                        GetCompanyNameAsync();
    Task<IEnumerable<Warehouse>>         GetWarehousesAsync(string[]? filter = null);
    Task<Warehouse?>                     GetWarehouseAsync(string     id);
    Task<(int itemCount, int binCount)>  GetItemAndBinCount(string    warehouse);
    Task<IEnumerable<ExternalValue>>     GetVendorsAsync();
    Task<bool>                           ValidateVendorsAsync(string            id);
    Task<BinLocation?>                   ScanBinLocationAsync(string            bin);
    Task<IEnumerable<Item>>              ScanItemBarCodeAsync(string            scanCode, bool    item = false);
    Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string?                 itemCode, string? barcode);
    Task<IEnumerable<BinContent>>        BinCheckAsync(int                      binEntry);
    Task<IEnumerable<ItemStockResponse>> ItemStockAsync(string                  itemCode, string whsCode);
    Task<UpdateItemBarCodeResponse>      UpdateItemBarCode(UpdateBarCodeRequest request);
    Task<ValidateAddItemResult>          GetItemValidationInfo(string           itemCode, string barCode, string warehouse, int? binEntry, bool enableBin);
}