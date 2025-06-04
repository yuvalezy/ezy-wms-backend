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
    Task<ValidateAddItemResult>          GetItemValidationInfo(string           itemCode,       string barCode, string  warehouse, int?                                     binEntry, bool enableBin);
    Task<ProcessTransferResponse>        ProcessTransfer(int                    transferNumber, string whsCode, string? comments,  Dictionary<string, TransferCreationData> data);

    // Picking methods
    Task<IEnumerable<PickingDocument>>         GetPickLists(Dictionary<string, object>              parameters, string whereClause);
    Task<IEnumerable<PickingDetail>>           GetPickingDetails(Dictionary<string, object>         parameters);
    Task<IEnumerable<PickingDetailItem>>       GetPickingDetailItems(Dictionary<string, object>     parameters);
    Task<IEnumerable<ItemBinLocationQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters);
    Task<PickingValidationResult>              ValidatePickingAddItem(PickListAddItemRequest        request,  Guid   userId);
    Task                                       AddPickingItem(PickListAddItemRequest                request,  Guid   employeeId, int pickEntry);
    Task<ProcessPickListResult>                ProcessPickList(int                                  absEntry, string warehouse);

    // Inventory Counting methods
    Task                                                ProcessInventoryCounting(int     countingNumber, string warehouse);
}