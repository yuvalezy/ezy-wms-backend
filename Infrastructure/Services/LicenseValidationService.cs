using Core.Enums;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class LicenseValidationService(
    IAccountStatusService accountStatusService,
    IDeviceService deviceService,
    ILicenseCacheService licenseCacheService,
    ILogger<LicenseValidationService> logger) : ILicenseValidationService {

    public async Task<bool> ValidateDeviceAccessAsync(string deviceUuid) {
        // Check if device exists and is active
        try {
            var device = await deviceService.GetDeviceAsync(deviceUuid);
            if (device == null || device.Status != DeviceStatus.Active) {
                logger.LogWarning("Device {DeviceUuid} not found or not active", deviceUuid);
                return false;
            }
        }
        catch {
            logger.LogWarning("Device {DeviceUuid} not found", deviceUuid);
            return false;
        }

        // Check account status
        var systemAccess = await ValidateSystemAccessAsync();
        if (!systemAccess) {
            logger.LogWarning("System access denied for device {DeviceUuid} due to account status", deviceUuid);
            return false;
        }

        return true;
    }

    public async Task<bool> ValidateSystemAccessAsync() {
        var accountStatus = await accountStatusService.GetCurrentAccountStatusAsync();
        
        return accountStatus.Status switch {
            AccountState.Active or AccountState.PaymentDue or AccountState.PaymentDueUnknown or AccountState.Demo => true,
            AccountState.Disabled or AccountState.DemoExpired => false,
            _ => false
        };
    }

    public async Task<LicenseValidationResult> GetLicenseValidationResultAsync() {
        var accountStatus = await accountStatusService.GetCurrentAccountStatusAsync();
        var licenseCache = await licenseCacheService.GetLicenseCacheAsync();
        
        var result = new LicenseValidationResult {
            AccountStatus = accountStatus.Status,
            ExpirationDate = accountStatus.ExpirationDate ?? accountStatus.DemoExpirationDate,
            IsValid = await ValidateSystemAccessAsync()
        };

        if (result.ExpirationDate.HasValue) {
            var daysUntilExpiration = (int)(result.ExpirationDate.Value - DateTime.UtcNow).TotalDays;
            result.DaysUntilExpiration = Math.Max(0, daysUntilExpiration);
            result.IsInGracePeriod = daysUntilExpiration > 0 && daysUntilExpiration <= 7;
        }

        // Determine warning message
        switch (accountStatus.Status) {
            case AccountState.PaymentDue:
                result.ShowWarning = true;
                result.WarningMessage = "Payment is due. Please contact support.";
                break;
            case AccountState.PaymentDueUnknown:
                result.ShowWarning = true;
                result.WarningMessage = $"Payment status unknown. System will be disabled in {result.DaysUntilExpiration} days.";
                break;
            default:
                if (result.IsInGracePeriod) {
                    result.ShowWarning = true;
                    result.WarningMessage = $"Account expires in {result.DaysUntilExpiration} days. Please renew.";
                }
                break;
        }

        return result;
    }

    public async Task<bool> IsWithinGracePeriodAsync() {
        var result = await GetLicenseValidationResultAsync();
        return result.IsInGracePeriod;
    }

    public async Task<int> GetDaysUntilExpirationAsync() {
        var result = await GetLicenseValidationResultAsync();
        return result.DaysUntilExpiration;
    }
}