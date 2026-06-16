using Core.DTOs.InventoryCounting;
using Core.Models;

namespace Core.Interfaces;

public interface IInventoryCountingBatchService {
    Task<ProcessInventoryCountingResponse>        ProcessCounting(Guid           id,        SessionInfo sessionInfo);
    Task                                          ProcessBatchesInBackground(Guid countingId, SessionInfo sessionInfo);
    Task<ProcessInventoryCountingResponse>        RetryFailedBatches(RetryBatchRequest request, SessionInfo sessionInfo);
    Task<IEnumerable<InventoryCountingBatchResponse>> GetBatches(Guid             countingId);
}
