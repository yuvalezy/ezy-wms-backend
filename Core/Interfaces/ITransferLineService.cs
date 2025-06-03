using Core.DTOs;
using Core.Models;

namespace Core.Interfaces;

public interface ITransferLineService {
    Task<TransferAddItemResponse> AddItem(SessionInfo info, TransferAddItemRequest request);
    Task<UpdateLineResponse> UpdateLine(SessionInfo info, UpdateLineRequest request);
}