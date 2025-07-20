using Core.DTOs.PickList;
using Core.Models;

namespace Core.Services;

public interface IPickListPackageService {
    /// <summary>
    /// Adds an entire package to a pick list, creating pick list entries for all package contents
    /// </summary>
    Task<PickListPackageResponse> AddPackageAsync(PickListAddPackageRequest request, SessionInfo sessionInfo);

    /// <summary>
    /// Clears all package commitments for a specific pick operation
    /// </summary>
    Task ClearPickListCommitmentsAsync(int absEntry, SessionInfo sessionInfo);
}