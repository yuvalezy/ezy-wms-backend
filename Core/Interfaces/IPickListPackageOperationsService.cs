using Core.DTOs.PickList;
using Core.Entities;
using Core.Models;

namespace Core.Interfaces;

public interface IPickListPackageOperationsService {
    Task<(Package?, PackageContent?, PickListAddItemResponse?)> ValidatePackageForItem(PickListAddItemRequest request);
    Task<(Package?, PickListPackageResponse?)> ValidatePackageForFullPicking(PickListAddPackageRequest request);
    Task CreatePackageCommitment(SessionInfo sessionInfo, PickList pickList, PickListAddItemRequest request);
    Task<bool> CreatePickListPackageIfNotExists(SessionInfo sessionInfo, PickList pickList, PickListAddItemRequest request, Package package);
    Task AddOrUpdatePackageContent(SessionInfo sessionInfo, Guid packageId, string itemCode, decimal quantity, int? binEntry, int id, int type, int entry, Guid pickListId);
}