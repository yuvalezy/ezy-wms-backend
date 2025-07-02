using Core.DTOs.Package;
using Core.Entities;

namespace Core.Services;

public interface IPackageValidationService {
    // Validation & Consistency
    Task<PackageValidationResult>           ValidatePackageConsistencyAsync(Guid packageId);
    Task<IEnumerable<PackageInconsistency>> DetectInconsistenciesAsync(string?   whsCode = null);
    
    // Barcode Management
    Task<string> GeneratePackageBarcodeAsync();
    Task<bool>   ValidatePackageBarcodeAsync(string barcode);
}