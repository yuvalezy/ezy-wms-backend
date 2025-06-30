using Core.DTOs.PickList;
using Core.Models;

namespace Core.Interfaces;

public interface IPickListProcessService {
    Task<ProcessPickListResponse> ProcessPickList(int absEntry, Guid userId);
    Task                          SyncPendingPickLists();
    Task<ProcessPickListResponse> CancelPickList(int absEntry, Guid userId);
}