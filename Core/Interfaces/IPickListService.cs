using Core.DTOs.PickList;

namespace Core.Interfaces;

public interface IPickListService {
    Task<IEnumerable<PickListResponse>> GetPickLists(PickListsRequest request,     string                 warehouse);
    Task<PickListResponse?>             GetPickList(int               absEntry,    PickListDetailRequest  request, string warehouse);
}