using Core.DTOs.PickList;

namespace Core.Interfaces;

public interface IPickListValidationService {
    Task<(bool IsValid, string? ErrorMessage, PickingValidationResult? ValidationResult)> ValidateItemForPicking(PickListAddItemRequest request);
    Task<(decimal ItemStock, decimal OpenQuantity)> CalculateBinOnHandQuantity(string itemCode, int? binEntry, decimal validationResult, decimal openQuantity);
    Task<(bool IsValid, string? ErrorMessage, PickingValidationResult? SelectedValidation)> ValidateQuantityAgainstPickList(int absEntry, string itemCode, decimal quantity, IEnumerable<PickingValidationResult> validationResults);
    Task<Dictionary<string, decimal>> CalculateOpenQuantitiesForPickList(int absEntry, IEnumerable<PickingDetailItemResponse> pickingDetails);
}