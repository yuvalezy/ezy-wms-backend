using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Models;

namespace Core.Interfaces;

public interface IExternalSystemAdapter {
    Task<ExternalValue<string>?>                         GetUserInfoAsync(string id);
    Task<IEnumerable<ExternalValue<string>>>             GetUsersAsync();
    Task<string?>                                        GetCompanyNameAsync();
    Task<IEnumerable<Warehouse>>                         GetWarehousesAsync(string[]? filter = null);
    Task<Warehouse?>                                     GetWarehouseAsync(string id);
    Task<(int itemCount, int binCount)>                  GetItemAndBinCount(string warehouse);
    Task<IEnumerable<ExternalValue<string>>>             GetVendorsAsync();
    Task<ExternalValue<string>?>                         GetVendorAsync(string                  cardCode);
    Task<bool>                                           ValidateVendorsAsync(string            id);
    Task<BinLocation?>                                   ScanBinLocationAsync(string            bin);
    Task<string?>                                        GetBinCodeAsync(int                    binEntry);
    Task<IEnumerable<Item>>                              ScanItemBarCodeAsync(string            scanCode, bool item = false);
    Task<IEnumerable<ItemCheckResponse>>                 ItemCheckAsync(string?                 itemCode, string? barcode);
    Task<IEnumerable<BinContent>>                        BinCheckAsync(int                      binEntry);
    Task<IEnumerable<ItemBinStockResponse>>              ItemStockAsync(string                  itemCode, string whsCode);
    Task<Dictionary<string, ItemWarehouseStockResponse>> ItemsWarehouseStockAsync(string        warehouse, string[] items);
    Task<UpdateItemBarCodeResponse>                      UpdateItemBarCode(UpdateBarCodeRequest request);
    Task<ValidateAddItemResult>                          GetItemValidationInfo(string           itemCode,       string barCode, string  warehouse, int? binEntry, bool enableBin);
    Task<ProcessTransferResponse>                        ProcessTransfer(int                    transferNumber, string whsCode, string? comments,  Dictionary<string, TransferCreationData> data);

    // Picking methods
    Task<IEnumerable<PickingDocument>>         GetPickListsAsync(PickListsRequest                   request, string warehouse);
    Task<IEnumerable<PickingDetail>>           GetPickingDetails(Dictionary<string, object>         parameters);
    Task<IEnumerable<PickingDetailItem>>       GetPickingDetailItems(Dictionary<string, object>     parameters);
    Task<IEnumerable<ItemBinLocationQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters);
    Task<PickingValidationResult[]>            ValidatePickingAddItem(PickListAddItemRequest        request,  Guid   userId);
    Task<ProcessPickListResult>                ProcessPickList(int                                  absEntry, string warehouse, List<PickList> data);
    Task<Dictionary<int, bool>>                GetPickListStatuses(int[]                            absEntries);

    // Inventory Counting methods
    Task<ProcessInventoryCountingResponse> ProcessInventoryCounting(int countingNumber, string warehouse, Dictionary<string, InventoryCountingCreationData> data);

    // Goods Receipt methods
    Task<GoodsReceiptValidationResult> ValidateGoodsReceiptAddItem(GoodsReceiptAddItemRequest request,   List<ObjectKey>  specificDocuments, Guid userId, string warehouse);
    Task<ProcessGoodsReceiptResult>    ProcessGoodsReceipt(int                                number,    string           warehouse,         Dictionary<string, List<GoodsReceiptCreationData>> data);
    Task                               ValidateGoodsReceiptDocuments(string                   warehouse, GoodsReceiptType type,              List<DocumentParameter> documents);

    Task<IEnumerable<GoodsReceiptAddItemSourceDocument>> AddItemSourceDocuments(GoodsReceiptAddItemRequest request, string warehouse, GoodsReceiptType type, string? cardCode,
        List<ObjectKey>                                                                                    specificDocuments);

    Task<IEnumerable<GoodsReceiptAddItemTargetDocuments>> AddItemTargetDocuments(string warehouse, string itemCode);
}