using Core.DTOs;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptService {
    // CRUD Operations
    Task<GoodsReceiptResponse>              CreateGoodsReceipt(CreateGoodsReceiptRequest request, SessionInfo session);
    Task<IEnumerable<GoodsReceiptResponse>> GetGoodsReceipts(GoodsReceiptsRequest        request, string      warehouse);
    Task<GoodsReceiptResponse?>             GetGoodsReceipt(Guid                         number);

    // Line Operations
    Task<GoodsReceiptAddItemResponse> AddItem(SessionInfo            session, GoodsReceiptAddItemRequest            request);
    Task<UpdateLineResponse>          UpdateLine(SessionInfo         session, UpdateGoodsReceiptLineRequest         request);
    Task<UpdateLineResponse>          UpdateLineQuantity(SessionInfo session, UpdateGoodsReceiptLineQuantityRequest request);

    // Document Operations
    Task<bool>                        CancelGoodsReceipt(Guid  id, SessionInfo session);
    Task<ProcessGoodsReceiptResponse> ProcessGoodsReceipt(Guid id, SessionInfo session);

    // Report Operations
    Task<IEnumerable<GoodsReceiptReportAllResponse>>                  GetGoodsReceiptAllReport(Guid                                                           id,      string      warehouse);
    Task<IEnumerable<GoodsReceiptReportAllDetailsResponse>>           GetGoodsReceiptAllReportDetails(Guid                                                    id,      string      itemCode);
    Task<bool>                                                        UpdateGoodsReceiptAll(UpdateGoodsReceiptAllRequest                                      request, SessionInfo session);
    Task<IEnumerable<GoodsReceiptVSExitReportResponse>>               GetGoodsReceiptVSExitReport(Guid                                                        id);
    Task<IEnumerable<GoodsReceiptValidateProcessResponse>>            GetGoodsReceiptValidateProcess(Guid                                                     id);
    Task<IEnumerable<GoodsReceiptValidateProcessLineDetailsResponse>> GetGoodsReceiptValidateProcessLineDetails(GoodsReceiptValidateProcessLineDetailsRequest request);
}