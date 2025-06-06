using Core.DTOs;
using Core.DTOs.Transfer;
using Core.Models;

namespace Core.Interfaces;

public interface ITransferLineService {
    Task<TransferAddItemResponse> AddItem(SessionInfo info, TransferAddItemRequest request);
    Task<UpdateLineResponse> UpdateLine(SessionInfo info, TransferUpdateLineRequest request);
    Task<UpdateLineResponse> UpdateLineQuantity(SessionInfo info, TransferUpdateLineQuantityRequest request);
}