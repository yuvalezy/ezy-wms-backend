using Core.DTOs;
using Core.DTOs.Items;
using Core.DTOs.Settings;
using Core.Models;

namespace Core.Interfaces;

public interface IPublicService {
    Task<IEnumerable<WarehouseResponse>>     GetWarehousesAsync(string[]? filter);
    Task<HomeInfoResponse>                   GetHomeInfoAsync(string      warehouse);
    Task<UserInfoResponse>                   GetUserInfoAsync(SessionInfo info);
    Task<IEnumerable<ExternalValue<string>>> GetVendorsAsync();
    Task<BinLocationResponse?>               ScanBinLocationAsync(string bin);
    Task<IEnumerable<ItemResponse>>          ScanItemBarCodeAsync(string scanCode, bool    item = false);
    Task<IEnumerable<ItemCheckResponse>>     ItemCheckAsync(string?      itemCode, string? barcode);
    Task<IEnumerable<BinContentResponse>>    BinCheckAsync(int           binEntry);
    Task<IEnumerable<ItemBinStockResponse>>  ItemStockAsync(string       itemCode, string               whsCode);
    Task<UpdateItemBarCodeResponse>          UpdateItemBarCode(string    userId,   UpdateBarCodeRequest request);
}