using Core.DTOs.PickList;

namespace Core.Interfaces;

public interface IPickListValidationService {
    Task<(bool IsValid, string? ErrorMessage, PickingValidationResult? ValidationResult)> ValidateItemForPicking(PickListAddItemRequest request);
    Task<(int ItemStock, int OpenQuantity)> CalculateBinOnHandQuantity(string itemCode, int? binEntry, int validationResult, int openQuantity);
    Task<(bool IsValid, string? ErrorMessage, PickingValidationResult? SelectedValidation)> ValidateQuantityAgainstPickList(int absEntry, string itemCode, int quantity, IEnumerable<PickingValidationResult> validationResults);
    Task<Dictionary<string, int>> CalculateOpenQuantitiesForPickList(int absEntry, IEnumerable<PickingDetailItemResponse> pickingDetails);
}