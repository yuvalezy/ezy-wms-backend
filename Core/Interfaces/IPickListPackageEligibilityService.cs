using Core.Entities;

namespace Core.Interfaces;

public interface IPickListPackageEligibilityService {
    bool CanPackageBeFullyPicked(List<PackageContent> packageContents, Dictionary<string, decimal> itemOpenQuantities);
    bool ValidatePackageForPicking(List<PackageContent> packageContents, Dictionary<string, decimal> itemOpenQuantities, out string? errorMessage);
}