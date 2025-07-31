using Core.DTOs.PickList;
using Core.Entities;

namespace Core.Interfaces;

public interface IPickListDetailService {
    Task GetPickListItemDetails(int absEntry, PickListDetailRequest request, PickListResponse response, PickList[] dbPick);
    Task ProcessClosedPickListsWithPackages();
}