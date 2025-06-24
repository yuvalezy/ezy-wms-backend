using Core.DTOs.GoodsReceipt;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptService {
    // CRUD Operations
    Task<GoodsReceiptResponse>              CreateGoodsReceipt(CreateGoodsReceiptRequest request, SessionInfo session);
    Task<IEnumerable<GoodsReceiptResponse>> GetGoodsReceipts(GoodsReceiptsRequest        request, string      warehouse);
    Task<GoodsReceiptResponse?>             GetGoodsReceipt(Guid                         number);

    // Document Operations
    Task<bool>                        CancelGoodsReceipt(Guid        id,      SessionInfo                           session);
    Task<ProcessGoodsReceiptResponse> ProcessGoodsReceipt(Guid       id,      SessionInfo                           session);
}