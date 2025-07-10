# IExternalSystemAdapter Refactoring Plan

## Domain Breakdown

### IExternalSystemAdapter.User
- `GetUserInfoAsync(string id)`
- `GetUsersAsync()`

### IExternalSystemAdapter.General
- `GetCompanyNameAsync()`
- `GetWarehousesAsync(string[]? filter = null)`
- `GetWarehouseAsync(string id)`
- `GetItemAndBinCount(string warehouse)`
- `GetVendorsAsync()`
- `GetVendorAsync(string cardCode)`
- `ValidateVendorsAsync(string id)`
- `ScanBinLocationAsync(string bin)`
- `GetBinCodeAsync(int binEntry)`
- `ScanItemBarCodeAsync(string scanCode, bool item = false)`
- `ItemCheckAsync(string? itemCode, string? barcode)`
- `BinCheckAsync(int binEntry)`
- `ItemStockAsync(string itemCode, string whsCode)`
- `ItemsWarehouseStockAsync(string warehouse, string[] items)`
- `UpdateItemBarCode(UpdateBarCodeRequest request)`
- `GetItemValidationInfo(string itemCode, string barCode, string warehouse, int? binEntry, bool enableBin)`
- `GetItemInfo(string itemCode)`

### IExternalSystemAdapter.Transfer
- `ProcessTransfer(int transferNumber, string whsCode, string? comments, Dictionary<string, TransferCreationDataResponse> data)`

### IExternalSystemAdapter.Picking
- `GetPickListsAsync(PickListsRequest request, string warehouse)`
- `GetPickingDetails(Dictionary<string, object> parameters)`
- `GetPickingDetailItems(Dictionary<string, object> parameters)`
- `GetPickingDetailItemsBins(Dictionary<string, object> parameters)`
- `ValidatePickingAddItem(PickListAddItemRequest request)`
- `ProcessPickList(int absEntry, List<PickList> data)`
- `GetPickListStatuses(int[] absEntries)`
- `GetPickingSelection(int absEntry)`
- `CancelPickList(int absEntry, PickingSelectionResponse[] selection, string warehouse, int transferBinEntry)`

### IExternalSystemAdapter.InventoryCounting
- `ProcessInventoryCounting(int countingNumber, string warehouse, Dictionary<string, InventoryCountingCreationDataResponse> data)`

### IExternalSystemAdapter.GoodsReceipt
- `LoadGoodsReceiptItemData(Dictionary<string, List<GoodsReceiptCreationDataResponse>> data)`
- `ValidateGoodsReceiptAddItem(string itemCode, string barcode, List<ObjectKey> specificDocuments, string warehouse)`
- `ProcessGoodsReceipt(int number, string warehouse, Dictionary<string, List<GoodsReceiptCreationDataResponse>> data)`
- `ValidateGoodsReceiptDocuments(string warehouse, GoodsReceiptType type, List<DocumentParameter> documents)`
- `AddItemSourceDocuments(string itemCode, UnitType unit, string warehouse, GoodsReceiptType type, string? cardCode, List<ObjectKey> specificDocuments)`
- `AddItemTargetDocuments(string warehouse, string itemCode)`
- `GoodsReceiptValidateProcessDocumentsData(ObjectKey[] docs)`