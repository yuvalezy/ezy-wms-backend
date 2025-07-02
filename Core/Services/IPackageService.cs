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

    // Content Management
    Task<PackageContent>              AddItemToPackageAsync(AddItemToPackageRequest           request, SessionInfo sessionInfo);
    Task<PackageContent>              RemoveItemFromPackageAsync(RemoveItemFromPackageRequest request, SessionInfo sessionInfo);
    Task<IEnumerable<PackageContent>> GetPackageContentsAsync(Guid                            packageId);
    Task<decimal>                     GetItemQuantityInPackageAsync(Guid                      packageId, string itemCode);

    // Location Management
    Task<Package>                             MovePackageAsync(MovePackageRequest request);
    Task<IEnumerable<PackageLocationHistory>> GetPackageLocationHistoryAsync(Guid packageId);

    // Validation & Consistency
    Task<PackageValidationResult>           ValidatePackageConsistencyAsync(Guid packageId);
    Task<IEnumerable<PackageInconsistency>> DetectInconsistenciesAsync(string?   whsCode = null);

    // Barcode Management
    Task<string> GeneratePackageBarcodeAsync();
    Task<bool>   ValidatePackageBarcodeAsync(string barcode);

    // Transaction History
    Task<IEnumerable<PackageTransaction>> GetPackageTransactionHistoryAsync(Guid                  packageId);
    Task                                  LogPackageTransactionAsync(LogPackageTransactionRequest request);
}