using Core.Entities;

namespace Core.Interfaces;

public interface IPickListPackageEligibilityService {
    bool CanPackageBeFullyPicked(List<PackageContent> packageContents, Dictionary<string, int> itemOpenQuantities);
    bool ValidatePackageForPicking(List<PackageContent> packageContents, Dictionary<string, int> itemOpenQuantities, out string? errorMessage);
}