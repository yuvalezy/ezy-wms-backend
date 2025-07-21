using Core.DTOs.PickList;
using Core.Models;

namespace Core.Interfaces;

public interface IPickListCheckService {
    Task<PickListCheckSession?>        StartCheck(int                     pickListId, SessionInfo sessionInfo);
    Task<PickListCheckItemResponse>    CheckItem(PickListCheckItemRequest request,    SessionInfo sessionInfo);
    Task<PickListCheckSummaryResponse> GetCheckSummary(int                pickListId, string      warehouse);
    Task<bool> CompleteCheck(int pickListId, Guid userId);
    Task<PickListCheckSession?> GetActiveCheckSession(int pickListId);
}