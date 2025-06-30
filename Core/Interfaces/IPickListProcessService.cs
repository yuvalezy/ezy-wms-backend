using Core.DTOs.PickList;

namespace Core.Interfaces;

public interface IPickListProcessService {
    Task<ProcessPickListResponse> ProcessPickList(int           absEntry,    Guid                   userId);
    Task                          SyncPendingPickLists();
}