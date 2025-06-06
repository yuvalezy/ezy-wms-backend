using Core.DTOs;
using Core.DTOs.GoodsReceipt;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptLinesService {
    Task<UpdateLineResponse>          UpdateLine(SessionInfo         session, UpdateGoodsReceiptLineRequest         request);
    Task<UpdateLineResponse>          UpdateLineQuantity(SessionInfo session, UpdateGoodsReceiptLineQuantityRequest request);
}