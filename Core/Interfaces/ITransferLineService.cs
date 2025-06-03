using Core.DTOs;

namespace Core.Interfaces;

public interface ITransferLineService {
    Task<TransferAddItemResponse> AddItem(Guid userId, string warehouse, TransferAddItemRequest request);
}