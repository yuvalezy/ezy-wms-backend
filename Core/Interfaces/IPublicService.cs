using Core.DTOs;
using Core.Models;

namespace Core.Interfaces;

public interface IPublicService {
    Task<IEnumerable<Warehouse>>         GetWarehousesAsync(string[]? filter);
    Task<HomeInfoResponse>                       GetHomeInfoAsync(string      warehouse);
    Task<UserInfoResponse>               GetUserInfoAsync(SessionInfo info);
    Task<IEnumerable<ExternalValue<string>>>     GetVendorsAsync();
    Task<BinLocation?>                   ScanBinLocationAsync(string bin);
    Task<IEnumerable<Item>>              ScanItemBarCodeAsync(string scanCode, bool    item = false);
    Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string?      itemCode, string? barcode);
    Task<IEnumerable<BinContent>>        BinCheckAsync(int           binEntry);
    Task<IEnumerable<ItemStockResponse>> ItemStockAsync(string       itemCode, string               whsCode);
    Task<UpdateItemBarCodeResponse>      UpdateItemBarCode(string    userId,   UpdateBarCodeRequest request);
}