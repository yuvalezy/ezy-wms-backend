using Core.DTOs.General;
using Core.DTOs.InventoryCounting;
using Core.Models;

namespace Core.Interfaces;

public interface IInventoryCountingsLineService {
    Task<InventoryCountingAddItemResponse> AddItem(SessionInfo      sessionInfo, InventoryCountingAddItemRequest    request);
    Task<UpdateLineResponse>               UpdateLine(SessionInfo   sessionInfo, InventoryCountingUpdateLineRequest request);
    Task<bool>                             ValidateScanPackage(Guid packageId,   Guid                               id, int? binEntry);
}