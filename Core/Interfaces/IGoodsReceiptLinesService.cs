using Core.DTOs;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptLinesService {
    Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo            session, GoodsReceiptAddItemRequest            request);
    Task<UpdateLineResponse>          UpdateLine(SessionInfo         session, UpdateGoodsReceiptLineRequest         request);
    Task<UpdateLineResponse>          UpdateLineQuantity(SessionInfo session, UpdateGoodsReceiptLineQuantityRequest request);
}