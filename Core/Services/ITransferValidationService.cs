using Core.DTOs.Transfer;
using Core.Models;

namespace Core.Services;

public interface ITransferValidationService {
    Task<bool> ValidateAddItemAsync(SessionInfo sessionInfo, TransferAddItemRequest request);
}