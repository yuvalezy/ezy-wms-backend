using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class LicenseCacheService(
    SystemDbContext context,
    ILicenseEncryptionService encryptionService,
    IAccountStatusService accountStatusService,
    ILogger<LicenseCacheService> logger) : ILicenseCacheService {

    public async Task<LicenseCacheData> GetLicenseCacheAsync() {
        var cache = await context.LicenseCaches
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (cache == null || cache.ExpirationTimestamp < DateTime.UtcNow) {
            logger.LogWarning("License cache not found or expired");
            return await CreateDefaultLicenseCacheAsync();
        }

        try {
            var data = encryptionService.DecryptLicenseData(cache.EncryptedData);
            
            // Validate data integrity
            if (!string.IsNullOrEmpty(cache.DataHash) && !encryptionService.ValidateDataHash(data, cache.DataHash)) {
                logger.LogError("License cache data integrity check failed");
                return await CreateDefaultLicenseCacheAsync();
            }

            return data;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to decrypt license cache");
            return await CreateDefaultLicenseCacheAsync();
        }
    }

    public async Task UpdateLicenseCacheAsync(LicenseCacheData data) {
        try {
            string encryptedData = encryptionService.EncryptLicenseData(data);
            string dataHash = encryptionService.GenerateDataHash(data);

            var cache = new Core.Entities.LicenseCache {
                EncryptedData = encryptedData,
                DataHash = dataHash,
                CacheTimestamp = DateTime.UtcNow,
                ExpirationTimestamp = DateTime.UtcNow.AddHours(24) // Cache for 24 hours
            };

            context.LicenseCaches.Add(cache);
            await context.SaveChangesAsync();

            logger.LogInformation("License cache updated successfully");
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to update license cache");
            throw;
        }
    }

    public async Task<bool> IsLicenseCacheValidAsync() {
        var cache = await context.LicenseCaches
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        return cache != null && cache.ExpirationTimestamp > DateTime.UtcNow;
    }

    public async Task InvalidateLicenseCacheAsync() {
        var cache = await context.LicenseCaches
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (cache != null) {
            cache.ExpirationTimestamp = DateTime.UtcNow.AddMinutes(-1); // Expire immediately
            await context.SaveChangesAsync();
            logger.LogInformation("License cache invalidated");
        }
    }

    public async Task<DateTime?> GetLastValidationTimestampAsync() {
        var cache = await GetLicenseCacheAsync();
        return cache?.LastValidationTimestamp;
    }

    private async Task<LicenseCacheData> CreateDefaultLicenseCacheAsync() {
        var accountStatus = await accountStatusService.GetCurrentAccountStatusAsync();
        
        return new LicenseCacheData {
            AccountStatus = accountStatus.Status,
            ExpirationDate = accountStatus.ExpirationDate,
            PaymentCycleDate = accountStatus.PaymentCycleDate,
            DemoExpirationDate = accountStatus.DemoExpirationDate,
            InactiveReason = accountStatus.InactiveReason,
            LastValidationTimestamp = DateTime.UtcNow,
            ActiveDeviceCount = 0,
            MaxAllowedDevices = 1, // Default
            AdditionalData = new Dictionary<string, object>()
        };
    }
}