using Core.DTOs.GoodsReceipt;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptAddItemService {
    Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo session, GoodsReceiptAddItemRequest request);
}