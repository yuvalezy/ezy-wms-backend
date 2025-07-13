using Core.DTOs.PickList;
using Core.Models;

namespace Core.Services;

public interface IPickListPackageService {
    /// <summary>
    /// Adds a source package to a pick operation, creating commitments for all package contents
    /// </summary>
    Task<PickListPackageResponse> HandleSourcePackageScanAsync(PickListAddSourcePackageRequest request, SessionInfo sessionInfo);

    /// <summary>
    /// Automatically picks entire package if contents match pending pick requirements
    /// </summary>
    Task<PickListPackageResponse> HandleAutoPickPackageAsync(PickListAutoPickRequest request, SessionInfo sessionInfo);

    /// <summary>
    /// Checks if a package can fulfill all pending pick list requirements for a specific operation
    /// </summary>
    Task<bool> CanAutoPickPackageAsync(int absEntry, Guid packageId);

    /// <summary>
    /// Handles partial picking from a source package with commitment tracking
    /// </summary>
    Task<PickListAddItemResponse> HandlePartialPickAsync(PickListAddItemRequest request, SessionInfo sessionInfo);

    /// <summary>
    /// Clears all package commitments for a specific pick operation
    /// </summary>
    Task ClearPickListCommitmentsAsync(int absEntry, int pickEntry, SessionInfo sessionInfo);

    /// <summary>
    /// Clears all package commitments for an entire pick operation (all pick entries)
    /// </summary>
    Task ClearAllPickListCommitmentsAsync(int absEntry, SessionInfo sessionInfo);
}