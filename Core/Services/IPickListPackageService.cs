using Core.DTOs.Package;
using Core.DTOs.PickList;
using Core.Models;

namespace Core.Services;

public interface IPickListPackageService {
    /// <summary>
    /// Adds an entire package to a pick list, creating pick list entries for all package contents
    /// </summary>
    Task<PickListPackageResponse> AddPackageAsync(PickListAddPackageRequest request, SessionInfo sessionInfo);

    /// <summary>
    /// Processes pick list closure by clearing commitments and optionally processing package movements
    /// </summary>
    Task ProcessPickListClosureAsync(int absEntry, PickListClosureInfo closureInfo, Guid userId);

    Task<PackageDto> CreatePackageAsync(int absEntry, SessionInfo sessionInfo);
}

