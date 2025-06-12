using Core.DTOs;
using Core.DTOs.GoodsReceipt;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptService {
    // CRUD Operations
    Task<GoodsReceiptResponse>              CreateGoodsReceipt(CreateGoodsReceiptRequest request, SessionInfo session);
    Task<IEnumerable<GoodsReceiptResponse>> GetGoodsReceipts(GoodsReceiptsRequest        request, string      warehouse);
    Task<GoodsReceiptResponse?>             GetGoodsReceipt(Guid                         number);

    // Document Operations
    Task<bool>                        CancelGoodsReceipt(Guid  id, SessionInfo session);
    Task<ProcessGoodsReceiptResponse> ProcessGoodsReceipt(Guid id, SessionInfo session);

    // Report Operations
    Task<IEnumerable<GoodsReceiptReportAllResponse>>                  GetGoodsReceiptAllReport(Guid id, string warehouse);
    Task<IEnumerable<GoodsReceiptReportAllDetailsResponse>>           GetGoodsReceiptAllReportDetails(Guid id, string itemCode);
    Task<string?>                                                     UpdateGoodsReceiptAll(UpdateGoodsReceiptAllRequest request, SessionInfo sessionInfo);
    Task                                                              RemoveRows(Guid[] rows, SessionInfo session);
    Task<IEnumerable<GoodsReceiptVSExitReportResponse>>               GetGoodsReceiptVSExitReport(Guid id);
    Task<IEnumerable<GoodsReceiptValidateProcessResponse>>            GetGoodsReceiptValidateProcess(Guid id);
    Task<IEnumerable<GoodsReceiptValidateProcessLineDetailsResponse>> GetGoodsReceiptValidateProcessLineDetails(GoodsReceiptValidateProcessLineDetailsRequest request);
    Task<UpdateLineResponse>                                          UpdateLine(SessionInfo session, UpdateGoodsReceiptLineRequest request);
    Task<GoodsReceiptAddItemResponse>                                 AddItem(SessionInfo            session, GoodsReceiptAddItemRequest            request);
    Task<UpdateLineResponse>                                          UpdateLineQuantity(SessionInfo session, UpdateGoodsReceiptLineQuantityRequest request);
}