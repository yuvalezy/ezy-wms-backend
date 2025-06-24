using Core.DTOs;
using Core.DTOs.GoodsReceipt;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptLineService {
    Task<UpdateLineResponse>          UpdateLine(SessionInfo         session, UpdateGoodsReceiptLineRequest         request);
    Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo            session, GoodsReceiptAddItemRequest            request);
    Task<UpdateLineResponse>          UpdateLineQuantity(SessionInfo session, UpdateGoodsReceiptLineQuantityRequest request);
    Task RemoveRows(Guid[] rows, SessionInfo session);
}