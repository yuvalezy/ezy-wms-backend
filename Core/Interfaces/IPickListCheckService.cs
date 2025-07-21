using Core.DTOs.PickList;
using Core.Models;

namespace Core.Interfaces;

public interface IPickListCheckService {
    Task<Core.Entities.PickListCheckSession?>        StartCheck(int                     pickListId, SessionInfo sessionInfo);
    Task<PickListCheckItemResponse>    CheckItem(PickListCheckItemRequest request,    SessionInfo sessionInfo);
    Task<PickListCheckPackageResponse> CheckPackage(PickListCheckPackageRequest request, SessionInfo sessionInfo);
    Task<PickListCheckSummaryResponse> GetCheckSummary(int                pickListId, string      warehouse);
    Task<bool> CompleteCheck(int pickListId, Guid userId);
    Task<bool> CancelCheck(int pickListId, Guid userId);
    Task<Core.Entities.PickListCheckSession?> GetActiveCheckSession(int pickListId);
}