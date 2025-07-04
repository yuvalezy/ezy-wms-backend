using Core.DTOs.GoodsReceipt;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptReportService {

    // Report Operations
    Task<GoodsReceiptReportAllResponse> GetGoodsReceiptAllReport(Guid id, string warehouse);
    Task<IEnumerable<GoodsReceiptReportAllDetailsResponse>>           GetGoodsReceiptAllReportDetails(Guid id, string itemCode);
    Task<string?>                                                     UpdateGoodsReceiptAll(UpdateGoodsReceiptAllRequest request, SessionInfo sessionInfo);
    Task<IEnumerable<GoodsReceiptVSExitReportResponse>>               GetGoodsReceiptVSExitReport(Guid id);
    Task<IEnumerable<GoodsReceiptValidateProcessResponse>>            GetGoodsReceiptValidateProcess(Guid id);
    Task<IEnumerable<GoodsReceiptValidateProcessLineDetailsResponse>> GetGoodsReceiptValidateProcessLineDetails(GoodsReceiptValidateProcessLineDetailsRequest request);
}