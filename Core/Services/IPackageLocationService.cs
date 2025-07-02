using Core.DTOs.Package;
using Core.Entities;
using Core.Enums;

namespace Core.Services;

public interface IPackageLocationService {
    // Location Management
    Task<Package>                             MovePackageAsync(MovePackageRequest request);
    Task<IEnumerable<PackageLocationHistory>> GetPackageLocationHistoryAsync(Guid packageId);
    
    // Location Tracking
    Task LogLocationMovementAsync(
        Guid                packageId,
        PackageMovementType movementType,
        string?             fromWhsCode,
        int?                fromBinEntry,
        string              toWhsCode,
        int?                toBinEntry,
        ObjectType          sourceOperationType,
        Guid?               sourceOperationId,
        Guid                userId);
}