using Core.Models;

namespace Core.Services;

public interface ILicenseCacheService {
    Task<LicenseCacheData> GetLicenseCacheAsync();
    Task                   UpdateLicenseCacheAsync(LicenseCacheData data);
    Task<bool>             IsLicenseCacheValidAsync();
    Task                   InvalidateLicenseCacheAsync();
    Task<DateTime?>        GetLastValidationTimestampAsync();
}