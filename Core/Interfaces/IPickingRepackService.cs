using Core.DTOs.PickList;
using Core.Models;

namespace Core.Interfaces;

public interface IPickingRepackService {
    Task<PickingRepackSummaryResponse> GetSummaryAsync(int absEntry, SessionInfo sessionInfo);
    Task<PickingRepackSummaryResponse> StartAsync(int absEntry, SessionInfo sessionInfo);
    Task<PickingRepackAssignResponse> AssignNextAsync(int absEntry, PickingRepackAssignRequest request, SessionInfo sessionInfo);
    Task<PickingRepackSummaryResponse> CompleteAsync(int absEntry, SessionInfo sessionInfo);
    Task<bool> IsReadyForSyncAsync(int absEntry, string warehouse);
}
