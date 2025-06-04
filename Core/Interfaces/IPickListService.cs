using Core.DTOs;
using Core.Models;

namespace Core.Interfaces;

public interface IPickListService {
    Task<IEnumerable<PickListResponse>> GetPickLists(PickListsRequest request, string warehouse);
    Task<PickListResponse> GetPickList(int absEntry, PickListDetailRequest request);
    Task<PickListAddItemResponse> AddItem(SessionInfo sessionInfo, PickListAddItemRequest request);
    Task<ProcessPickListResponse> ProcessPickList(int absEntry, SessionInfo sessionInfo);
}