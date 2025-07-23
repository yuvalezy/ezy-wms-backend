using Core.DTOs.GoodsReceipt;
using Core.DTOs.InventoryCounting;
using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.DTOs.Settings;
using Core.DTOs.Transfer;
using Core.Entities;
using Core.Enums;
using Core.Models;

namespace Core.Interfaces;

public interface IExternalSystemAdapter {
    //Users
    Task<ExternalValue<string>?>             GetUserInfoAsync(string id);
    Task<IEnumerable<ExternalValue<string>>> GetUsersAsync();

    Task<string?> GetCompanyNameAsync();

    // Warehouse
    Task<IEnumerable<WarehouseResponse>> GetWarehousesAsync(string[]? filter = null);

    Task<WarehouseResponse?> GetWarehouseAsync(string id);

    // Vendor
    Task<IEnumerable<ExternalValue<string>>> GetVendorsAsync();
    Task<ExternalValue<string>?>             GetVendorAsync(string cardCode);

    Task<bool> ValidateVendorsAsync(string id);

    // Items
    Task<(int itemCount, int binCount)>                  GetItemAndBinCount(string              warehouse);
    Task<BinLocationResponse?>                           ScanBinLocationAsync(string            bin);
    Task<string?>                                        GetBinCodeAsync(int                    binEntry);
    Task<IEnumerable<ItemInfoResponse>>                  ScanItemBarCodeAsync(string            scanCode, bool    item = false);
    Task<IEnumerable<ItemCheckResponse>>                 ItemCheckAsync(string?                 itemCode, string? barcode);
    Task<IEnumerable<BinContentResponse>>                BinCheckAsync(int                      binEntry);
    Task<IEnumerable<ItemBinStockResponse>>              ItemStockAsync(string                  itemCode,  string   whsCode);
    Task<Dictionary<string, ItemWarehouseStockResponse>> ItemsWarehouseStockAsync(string        warehouse, string[] items);
    Task<UpdateItemBarCodeResponse>                      UpdateItemBarCode(UpdateBarCodeRequest request);
    Task<ValidateAddItemResult>                          GetItemValidationInfo(string           itemCode, string barCode, string warehouse, int? binEntry, bool enableBin);

    Task<ItemUnitResponse> GetItemInfo(string itemCode);

    // Item Metadata methods
    /// <summary>
    /// Retrieves item metadata from the external system by item code
    /// </summary>
    /// <param name="itemCode">The item code to retrieve metadata for</param>
    /// <returns>Item metadata response with all configured fields, or null if item not found</returns>
    Task<ItemMetadataResponse?> GetItemMetadataAsync(string itemCode);

    /// <summary>
    /// Updates item metadata in the external system
    /// </summary>
    /// <param name="itemCode">The item code to update</param>
    /// <param name="request">The metadata update request containing writable field values</param>
    /// <returns>Updated item metadata response</returns>
    Task<ItemMetadataResponse> UpdateItemMetadataAsync(string itemCode, ItemMetadataRequest request);

    // Transfer
    Task<ProcessTransferResponse> ProcessTransfer(int transferNumber, string whsCode, string? comments, Dictionary<string, TransferCreationDataResponse> data);

    // Picking methods
    Task<IEnumerable<PickingDocumentResponse>>         GetPickListsAsync(PickListsRequest                   request, string warehouse);
    Task<IEnumerable<PickingDetailResponse>>           GetPickingDetails(Dictionary<string, object>         parameters);
    Task<IEnumerable<PickingDetailItemResponse>>       GetPickingDetailItems(Dictionary<string, object>     parameters);
    Task<IEnumerable<ItemBinLocationResponseQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters);
    Task<PickingValidationResult[]>                    ValidatePickingAddItem(PickListAddItemRequest        request);
    Task<bool>                                         ValidatePickingAddPackage(int                        absEntry, IEnumerable<PickListValidateAddPackageRequest> values);
    Task<ProcessPickListResult>                        ProcessPickList(int                                  absEntry, List<PickList>                                 data);
    Task<Dictionary<int, bool>>                        GetPickListStatuses(int[]                            absEntries);
    Task<PickListClosureInfo>                          GetPickListClosureInfo(int                           absEntry);
    Task<IEnumerable<PickingSelectionResponse>>        GetPickingSelection(int                              absEntry);
    Task<ProcessPickListResponse>                      CancelPickList(int                                   absEntry, PickingSelectionResponse[] selection, string warehouse, int transferBinEntry);

    // Inventory Counting methods
    Task<ProcessInventoryCountingResponse> ProcessInventoryCounting(int         countingNumber, string warehouse, Dictionary<string, InventoryCountingCreationDataResponse> data);
    Task<bool>                             ValidateOpenInventoryCounting(string whsCode,        int    binEntry,  string                                                    itemCode);

    // Goods Receipt methods
    Task                               LoadGoodsReceiptItemData(Dictionary<string, List<GoodsReceiptCreationDataResponse>> data);
    Task<GoodsReceiptValidationResult> ValidateGoodsReceiptAddItem(string itemCode, string barcode, List<ObjectKey> specificDocuments, string warehouse);
    Task<ProcessGoodsReceiptResult>    ProcessGoodsReceipt(int number, string warehouse, Dictionary<string, List<GoodsReceiptCreationDataResponse>> data);
    Task                               ValidateGoodsReceiptDocuments(string warehouse, GoodsReceiptType type, List<DocumentParameter> documents);

    Task<IEnumerable<GoodsReceiptAddItemSourceDocumentResponse>> AddItemSourceDocuments(string itemCode, UnitType unit,
        string                                                                                 warehouse,
        GoodsReceiptType                                                                       type,
        string?                                                                                cardCode,
        List<ObjectKey>                                                                        specificDocuments);

    Task<IEnumerable<GoodsReceiptAddItemTargetDocumentsResponse>>       AddItemTargetDocuments(string                        warehouse, string itemCode);
    Task<IEnumerable<GoodsReceiptValidateProcessDocumentsDataResponse>> GoodsReceiptValidateProcessDocumentsData(ObjectKey[] docs);
}