using Core.Entities;
using Core.DTOs.Package;
using Core.Enums;
using Core.Models;

namespace Core.Services;

public interface IPackageService {
    // Core Package Operations
    Task<Package>              CreatePackageAsync(SessionInfo                   sessionInfo, CreatePackageRequest request);
    Task<Package?>             GetPackageAsync(Guid                             packageId);
    Task<Package?>             GetPackageByBarcodeAsync(PackageByBarcodeRequest parameters);
    Task<IEnumerable<Package>> GetActivePackagesAsync(string?                   whsCode = null);
    Task<int>                  ActivatePackagesBySourceAsync(ObjectType         sourceOperationType, Guid        sourceOperationId, SessionInfo sessionInfo);
    Task<Package>              ClosePackageAsync(Guid                           packageId,           SessionInfo sessionInfo);
    Task<Package>              CancelPackageAsync(Guid                          packageId,           SessionInfo sessionInfo, string? reason);
    Task<Package>              LockPackageAsync(Guid                            packageId,           SessionInfo sessionInfo, string? reason);
    Task<Package>              UnlockPackageAsync(Guid                          packageId,           SessionInfo sessionInfo);
    
    // Package Metadata Operations
    Task<Package>              UpdatePackageMetadataAsync(Guid                  packageId,           UpdatePackageMetadataRequest request, SessionInfo sessionInfo);
}