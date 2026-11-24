using Core.DTOs.PickList;

namespace Core.Interfaces;

public interface IPickListValidationService {
    Task<(bool IsValid, string? ErrorMessage, PickingValidationResult? ValidationResult)> ValidateItemForPicking(PickListAddItemRequest request);
    Task<(decimal ItemStock, decimal OpenQuantity)> CalculateBinOnHandQuantity(string itemCode, int? binEntry, decimal validationResult, decimal openQuantity);

    /// <summary>
    /// Base-unit quantity of the item held in WMS per bin (picked-not-yet-synced pick-list rows and
    /// open transfer lines) that is still included in SAP's bin on-hand. Bins with nothing reserved are omitted.
    /// </summary>
    Task<Dictionary<int, decimal>> GetReservedByBinAsync(string itemCode, IReadOnlyCollection<int> binEntries);
    Task<(bool IsValid, string? ErrorMessage, PickingValidationResult? SelectedValidation)> ValidateQuantityAgainstPickList(int absEntry, string itemCode, decimal quantity, IEnumerable<PickingValidationResult> validationResults);
    Task<Dictionary<string, decimal>> CalculateOpenQuantitiesForPickList(int absEntry, IEnumerable<PickingDetailItemResponse> pickingDetails);

    /// <summary>
    /// True when the item belongs to a non-finalized goods receipt (put-away in progress) in the
    /// given warehouse. Used to block picking of stock that has not yet been posted to SAP.
    /// </summary>
    Task<bool> IsItemInInProgressGoodsReceipt(string itemCode, string warehouse);

    /// <summary>
    /// Returns the first of the given items that belongs to a non-finalized goods receipt in any
    /// of the given warehouses, or null if none. Batch form used to gate pick-list processing.
    /// </summary>
    Task<string?> FindItemInInProgressGoodsReceipt(IReadOnlyCollection<string> itemCodes, IReadOnlyCollection<string> warehouses);

    /// <summary>
    /// Returns the subset of the given items that belong to a non-finalized goods receipt in the
    /// given warehouse. Used to flag rows on the picking screen so operators are warned up front.
    /// </summary>
    Task<HashSet<string>> GetInProgressGoodsReceiptItems(IReadOnlyCollection<string> itemCodes, string warehouse);
}