using Core.DTOs;

namespace Core.Interfaces;

public interface ITransferLineService {
    Task<TransferAddItemResponse> AddItem(string warehouse, TransferAddItemRequest request);
}