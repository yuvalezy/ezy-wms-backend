using Core.DTOs;
using Core.DTOs.GoodsReceipt;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptLineItemService {
    Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo            session, GoodsReceiptAddItemRequest            request);
    Task<UpdateLineResponse>          UpdateLineQuantity(SessionInfo session, UpdateGoodsReceiptLineQuantityRequest request);
}