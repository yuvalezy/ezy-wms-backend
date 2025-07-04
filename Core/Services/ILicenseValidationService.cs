using Core.Models;

namespace Core.Services;

public interface ILicenseValidationService {
    Task<bool>                    ValidateDeviceAccessAsync(string deviceUuid);
    Task<bool>                    ValidateSystemAccessAsync();
    Task<LicenseValidationResult> GetLicenseValidationResultAsync();
    Task<bool>                    IsWithinGracePeriodAsync();
    Task<int>                     GetDaysUntilExpirationAsync();
}