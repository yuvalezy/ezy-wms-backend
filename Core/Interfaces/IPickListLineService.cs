using Core.DTOs.PickList;
using Core.Models;

namespace Core.Interfaces;

public interface IPickListLineService {
    Task<PickListAddItemResponse>       AddItem(SessionInfo           sessionInfo, PickListAddItemRequest request);
}