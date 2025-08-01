using Core.DTOs.PickList;

namespace Core.Interfaces;

public interface IPickListService {
    Task<IEnumerable<PickListResponse>> GetPickLists(PickListsRequest request, string warehouse, bool sessionInfoEnableBinLocations);
    Task<PickListResponse?>             GetPickList(int               absEntry,    PickListDetailRequest  request, string warehouse);
}