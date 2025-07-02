using Core.Entities;
using Core.DTOs.Package;
using Core.Enums;
using Core.Models;

namespace Core.Services;

public interface IPackageService {
    // Core Package Operations
    Task<Package>              CreatePackageAsync(SessionInfo  sessionInfo, CreatePackageRequest request);
    Task<Package?>             GetPackageAsync(Guid            packageId);
    Task<Package?>             GetPackageByBarcodeAsync(string barcode);
    Task<IEnumerable<Package>> GetActivePackagesAsync(string?  whsCode = null);
    Task<IEnumerable<Package>> GetActivePackagesBySourceAsync(ObjectType sourceOperationType, Guid sourceOperationId);
    Task<int>                  ActivatePackagesBySourceAsync(ObjectType sourceOperationType, Guid sourceOperationId, SessionInfo sessionInfo);
    Task<Package>              ClosePackageAsync(Guid          packageId, SessionInfo sessionInfo);
    Task<Package>              CancelPackageAsync(Guid         packageId, SessionInfo sessionInfo, string  reason);
    Task<Package>              LockPackageAsync(Guid           packageId, SessionInfo sessionInfo, string? reason);
    Task<Package>              UnlockPackageAsync(Guid         packageId, SessionInfo sessionInfo);

    // Content Management (used internally and by other services)
    Task<PackageContent>              AddItemToPackageAsync(AddItemToPackageRequest request, SessionInfo sessionInfo);
    Task<IEnumerable<PackageContent>> GetPackageContentsAsync(Guid                  packageId);

    // Barcode Management (used internally for package creation)
    Task<string> GeneratePackageBarcodeAsync();

    // Transaction Logging (used internally by other services)
    Task LogPackageTransactionAsync(LogPackageTransactionRequest request);
}