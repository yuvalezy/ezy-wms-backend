using Adapters.Common.SBO.Services;
using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.Interfaces;

namespace Adapters.Common.SBO.Repositories;

/// <summary>
/// Facade that composes focused picking repositories.
/// Keeps the public API unchanged so consumers require no modification.
/// </summary>
public class SboPickingRepository(SboDatabaseService dbService, ISettings settings) {
    private readonly SboPickListRepository           _pickList     = new(dbService, settings);
    private readonly SboPickingDetailRepository      _detail       = new(dbService, settings);
    private readonly SboPickingBinRepository         _bin          = new(dbService);
    private readonly SboPickingValidationRepository  _validation   = new(dbService, settings);

    // --- Pick list ---
    public Task<IEnumerable<PickingDocumentResponse>> GetPickLists(PickListsRequest request, string warehouse)
        => _pickList.GetPickLists(request, warehouse);

    public Task<Dictionary<int, bool>> GetPickListStatuses(int[] absEntries)
        => _pickList.GetPickListStatuses(absEntries);

    // --- Detail ---
    public Task<IEnumerable<PickingDetailResponse>> GetPickingDetails(Dictionary<string, object> parameters)
        => _detail.GetPickingDetails(parameters);

    public Task<IEnumerable<PickingDetailItemResponse>> GetPickingDetailItems(Dictionary<string, object> parameters)
        => _detail.GetPickingDetailItems(parameters);

    // --- Bins / selection ---
    public Task<IEnumerable<ItemBinLocationResponseQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters)
        => _bin.GetPickingDetailItemsBins(parameters);

    public Task<IEnumerable<PickingSelectionResponse>> GetPickingSelection(int absEntry)
        => _bin.GetPickingSelection(absEntry);

    // --- Validation / closure ---
    public Task<PickingValidationResult[]> ValidatePickingAddItem(PickListAddItemRequest request)
        => _validation.ValidatePickingAddItem(request);

    public Task<PickListClosureInfo> GetPickListClosureInfo(int absEntry)
        => _validation.GetPickListClosureInfo(absEntry);
}
