using Core.DTOs.InventoryCounting;
using Core.Models;

namespace Core.Interfaces;

public interface IInventoryCountingReportService {
    Task<IEnumerable<InventoryCountingContentResponse>>          GetCountingContent(InventoryCountingContentRequest       request);
    Task<InventoryCountingSummaryResponse>                       GetCountingSummaryReport(Guid                            id);
    Task<IEnumerable<InventoryCountingReportAllDetailsResponse>> GetCountingReportAllDetails(Guid id, string itemCode, int? binEntry);
    Task<string?>                                                UpdateCountingAll(UpdateInventoryCountingAllRequest       request, SessionInfo sessionInfo);
}
