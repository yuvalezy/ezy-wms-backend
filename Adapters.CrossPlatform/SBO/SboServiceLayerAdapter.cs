using Adapters.Common.SBO.Enums;
using Adapters.Common.SBO.Repositories;
using Adapters.Common.SBO.Services;
using Adapters.CrossPlatform.SBO.Helpers;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.GoodsReceipt;
using Core.DTOs.InventoryCounting;
using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.DTOs.Settings;
using Core.DTOs.Transfer;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO;

public class SboServiceLayerAdapter : IExternalSystemAdapter {
    private readonly SboEmployeeRepository employeeRepository;
    private readonly SboGeneralRepository generalRepository;
    private readonly SboItemRepository itemRepository;
    private readonly SboPickingRepository pickingRepository;
    private readonly SboGoodsReceiptRepository goodsReceiptRepository;
    private readonly SboInventoryCountingRepository inventoryCountingRepository;
    private readonly SboCompany sboCompany;
    private readonly SboDatabaseService databaseService;
    private readonly ILoggerFactory loggerFactory;
    private readonly ItemSettings itemSettings;

    public SboServiceLayerAdapter(SboEmployeeRepository employeeRepository,
        SboGeneralRepository generalRepository,
        SboItemRepository itemRepository,
        SboPickingRepository pickingRepository,
        SboGoodsReceiptRepository goodsReceiptRepository,
        SboInventoryCountingRepository inventoryCountingRepository,
        ISettings settings,
        SboCompany sboCompany,
        SboDatabaseService databaseService,
        ILoggerFactory loggerFactory) {
        this.employeeRepository = employeeRepository;
        this.generalRepository = generalRepository;
        this.itemRepository = itemRepository;
        this.pickingRepository = pickingRepository;
        this.goodsReceiptRepository = goodsReceiptRepository;
        this.inventoryCountingRepository = inventoryCountingRepository;
        this.sboCompany = sboCompany;
        this.databaseService = databaseService;
        this.itemSettings = settings.Item;
        this.loggerFactory = loggerFactory;
        if (string.IsNullOrWhiteSpace(settings.SboSettings?.ServiceLayerUrl)) {
            throw new Exception("Service Layer Url is not set");
        }
        //todo validate rest of the settings
    }

    // General 
    public async Task<string?> GetCompanyNameAsync() => await generalRepository.GetCompanyNameAsync();

    // Vendor
    public async Task<IEnumerable<ExternalValue<string>>> GetVendorsAsync() => await generalRepository.GetVendorsAsync();
    public async Task<ExternalValue<string>?> GetVendorAsync(string cardCode) => await generalRepository.GetVendorAsync(cardCode);
    public async Task<bool> ValidateVendorsAsync(string id) => await generalRepository.ValidateVendorsAsync(id);

    // Users
    public async Task<ExternalValue<string>?> GetUserInfoAsync(string id) => await employeeRepository.GetByIdAsync(id);
    public async Task<IEnumerable<ExternalValue<string>>> GetUsersAsync() => await employeeRepository.GetAllAsync();

    // Warehouses
    public async Task<IEnumerable<WarehouseResponse>> GetWarehousesAsync(string[]? filter = null) => await generalRepository.GetWarehousesAsync(filter);
    public async Task<WarehouseResponse?> GetWarehouseAsync(string id) => (await generalRepository.GetWarehousesAsync([id])).FirstOrDefault();

    // Items, Warehouse & Bins
    public async Task<(int itemCount, int binCount)> GetItemAndBinCount(string warehouse) => await generalRepository.GetItemAndBinCountAsync(warehouse);
    public async Task<BinLocationResponse?> ScanBinLocationAsync(string bin) => await generalRepository.ScanBinLocationAsync(bin);
    public async Task<string?> GetBinCodeAsync(int binEntry) => await generalRepository.GetBinCodeAsync(binEntry);
    public async Task<IEnumerable<ItemInfoResponse>> ScanItemBarCodeAsync(string scanCode, bool item = false) => await itemRepository.ScanItemBarCodeAsync(scanCode, item);
    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string? itemCode, string? barcode) => await itemRepository.ItemCheckAsync(itemCode, barcode);
    public async Task<IEnumerable<BinContentResponse>> BinCheckAsync(int binEntry) => await generalRepository.BinCheckAsync(binEntry);
    public async Task<IEnumerable<ItemStockResponse>> ItemStockAsync(string itemCode, string whsCode) => await itemRepository.ItemStockAsync(itemCode, whsCode);

    public async Task<IEnumerable<ItemBinStockResponse>> ItemBinStockAsync(string itemCode, string whsCode) => await itemRepository.ItemBinStockAsync(itemCode, whsCode);
    public async Task<Dictionary<string, ItemWarehouseStockResponse>> ItemsWarehouseStockAsync(string warehouse, string[] items) => await itemRepository.ItemsWarehouseStockAsync(warehouse, items);

    public async Task<UpdateItemBarCodeResponse> UpdateItemBarCode(UpdateBarCodeRequest request) {
        using var update = new ItemBarCodeUpdate(sboCompany, request.ItemCode, request.AddBarcodes, request.RemoveBarcodes);
        return await update.Execute();
    }

    public async Task<ValidateAddItemResult> GetItemValidationInfo(string itemCode, string? barCode, string warehouse, int? binEntry, bool enableBin) =>
    await itemRepository.GetItemValidationInfo(itemCode, barCode, warehouse, binEntry, enableBin);

    public async Task<ItemUnitResponse> GetItemInfo(string itemCode) => await itemRepository.GetItemPurchaseUnits(itemCode);

    public async Task<ItemMetadataResponse?> GetItemMetadataAsync(string itemCode) {
        using var processor = new ItemMetadataProcessor(sboCompany, itemSettings, itemCode, loggerFactory);
        return await processor.GetItemMetadata();
    }

    public async Task<ItemMetadataResponse> UpdateItemMetadataAsync(string itemCode, ItemMetadataRequest request) {
        using var processor = new ItemMetadataProcessor(sboCompany, itemSettings, itemCode, loggerFactory);
        return await processor.SetItemMetadata(request);
    }

    // Transfers
    public async Task<ProcessTransferResponse> ProcessTransfer(int transferNumber, string whsCode, string? comments, Dictionary<string, TransferCreationDataResponse> data) {
        int series = await generalRepository.GetSeries(ObjectTypes.oStockTransfer);
        using var transferCreation = new TransferCreation(sboCompany, transferNumber, whsCode, comments, series, data, loggerFactory);
        try {
            return await transferCreation.Execute();
        }
        catch (Exception e) {
            return new ProcessTransferResponse {
                Success = false,
                Status = ResponseStatus.Error,
                ErrorMessage = e.Message
            };
        }
        //todo send alert to sap
//     private void ProcessTransferSendAlert(int id, List<string> sendTo, TransferCreation creation) {
//         try {
//             using var alert = new Alert();
//             alert.Subject = string.Format(ErrorMessages.WMSTransactionAlert, id);
//             var transactionColumn = new AlertColumn(ErrorMessages.WMSTransaction);
//             var transferColumn    = new AlertColumn(ErrorMessages.InventoryTransfer, true);
//             alert.Columns.AddRange([transactionColumn, transferColumn]);
//             transactionColumn.Values.Add(new AlertValue(id.ToString()));
//             transferColumn.Values.Add(new AlertValue(creation.Number.ToString(), "67", creation.Entry.ToString()));
//
//             alert.Send(sendTo);
//         }
//         catch (Exception e) {
//         }
//     }
    }

    public async Task Canceltransfer(int transferEntry) {
        using var transferCancel = new TransferCancel(sboCompany, transferEntry, loggerFactory);
        await transferCancel.Execute();
    }

    // Pick List
    public async Task<IEnumerable<PickingDocumentResponse>> GetPickListsAsync(PickListsRequest request, string warehouse) => await pickingRepository.GetPickLists(request, warehouse);

    public async Task<IEnumerable<PickingDetailResponse>> GetPickingDetails(Dictionary<string, object> parameters) => await pickingRepository.GetPickingDetails(parameters);

    public async Task<IEnumerable<PickingDetailItemResponse>> GetPickingDetailItems(Dictionary<string, object> parameters) => await pickingRepository.GetPickingDetailItems(parameters);

    public async Task<IEnumerable<ItemBinLocationResponseQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters) => await pickingRepository.GetPickingDetailItemsBins(parameters);

    public async Task<PickingValidationResult[]> ValidatePickingAddItem(PickListAddItemRequest request) => await pickingRepository.ValidatePickingAddItem(request);
    public async Task<bool> ValidatePickingAddPackage(int absEntry, IEnumerable<PickListValidateAddPackageRequest> values) => await pickingRepository.ValidatePickingAddPackage(absEntry, values);

    public async Task<ProcessPickListResult> ProcessPickList(int absEntry, List<PickList> data) {
        using var update = new PickingUpdate(absEntry, data, sboCompany, databaseService, loggerFactory);
        var result = new ProcessPickListResult {
            Success = true,
            DocumentNumber = absEntry,
        };

        try {
            await update.Execute();
        }
        catch (Exception e) {
            result.ErrorMessage = e.Message;
            result.Success = false;
        }

        return result;
    }

    public async Task<Dictionary<int, bool>> GetPickListStatuses(int[] absEntries) => await pickingRepository.GetPickListStatuses(absEntries);
    public async Task<PickListClosureInfo> GetPickListClosureInfo(int absEntry) => await pickingRepository.GetPickListClosureInfo(absEntry);

    public async Task<IEnumerable<PickingSelectionResponse>> GetPickingSelection(int absEntry) => await pickingRepository.GetPickingSelection(absEntry);

    public async Task<ProcessPickListResponse> CancelPickList(int absEntry, PickingSelectionResponse[] selection, string warehouse, int transferBinEntry, bool enableBinLocations) {
        var pickingCancellation = new PickingCancellation(sboCompany, absEntry, selection, warehouse, transferBinEntry, loggerFactory, enableBinLocations);
        return await pickingCancellation.Execute();
    }


    //Inventory Counting
    public async Task<ProcessInventoryCountingResponse> ProcessInventoryCounting(int countingNumber, string warehouse, Dictionary<string, InventoryCountingCreationDataResponse> data) {
        int series = await generalRepository.GetSeries("1470000065");
        using var creation = new CountingCreation(sboCompany, countingNumber, warehouse, series, data, loggerFactory);
        try {
            return await creation.Execute();
        }
        catch (Exception e) {
            return new ProcessInventoryCountingResponse {
                Success = false,
                Status = ResponseStatus.Error,
                ErrorMessage = e.Message
            };
        }
    }

    public async Task<bool> ValidateOpenInventoryCounting(string whsCode, int binEntry, string itemCode) {
        return await inventoryCountingRepository.ValidateOpenInventoryCounting(whsCode, binEntry, itemCode);
    }

    // Goods Receipt methods
    public async Task<GoodsReceiptValidationResult> ValidateGoodsReceiptAddItem(string itemCode, string? barcode, List<ObjectKey> specificDocuments, string warehouse, bool useBaseUnit) {
        return await goodsReceiptRepository.ValidateGoodsReceiptAddItem(itemCode, barcode, warehouse, specificDocuments, useBaseUnit);
    }

    public async Task<ProcessGoodsReceiptResult> ProcessGoodsReceipt(int number, string warehouse, Dictionary<string, List<GoodsReceiptCreationDataResponse>> data) {
        int series = await generalRepository.GetSeries("20");
        var creation = new GoodsReceiptCreation(sboCompany, number, warehouse, series, data, loggerFactory);
        return await creation.Execute();
    }

    public async Task ValidateGoodsReceiptDocuments(string warehouse, GoodsReceiptType type, List<DocumentParameter> documents) {
        await goodsReceiptRepository.ValidateGoodsReceiptDocuments(warehouse, type, documents);
    }

    public async Task<IEnumerable<GoodsReceiptAddItemSourceDocumentResponse>> AddItemSourceDocuments(
        string itemCode,
        UnitType unit,
        string warehouse,
        GoodsReceiptType type,
        string? cardCode,
        List<ObjectKey> specificDocuments) {
        return await goodsReceiptRepository.AddItemSourceDocuments(itemCode, unit, warehouse, type, cardCode, specificDocuments);
    }

    public async Task<IEnumerable<GoodsReceiptAddItemTargetDocumentsResponse>> AddItemTargetDocuments(string warehouse, string itemCode) =>
    await goodsReceiptRepository.AddItemTargetDocuments(warehouse, itemCode);

    public async Task<IEnumerable<GoodsReceiptValidateProcessDocumentsDataResponse>> GoodsReceiptValidateProcessDocumentsData(ObjectKey[] docs) =>
    await goodsReceiptRepository.GoodsReceiptValidateProcessDocumentsData(docs);

    public async Task<(bool success, string? errorMessage)> ProcessConfirmationAdjustments(int number, string warehouse, bool enableBinLocation, int? defaultBinLocation,
        List<(string ItemCode, decimal Quantity)> negativeItems,
        List<(string ItemCode, decimal Quantity)> positiveItems) {
        int entrySeries = await generalRepository.GetSeries(ObjectTypes.oInventoryGenEntry);
        int exitSeries = await generalRepository.GetSeries(ObjectTypes.oInventoryGenExit);
        var confirmationAdjustments =
        new ConfirmationAdjustments(number, warehouse, enableBinLocation, defaultBinLocation, negativeItems, positiveItems, entrySeries, exitSeries, sboCompany, loggerFactory);

        return await confirmationAdjustments.Execute();
    }

    public async Task LoadGoodsReceiptItemData(Dictionary<string, List<GoodsReceiptCreationDataResponse>> data) => await goodsReceiptRepository.LoadGoodsReceiptItemData(data);
}