using Core.DTOs.License;
using Core.Enums;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class LicenseValidationService(
    IAccountStatusService             accountStatusService,
    IDeviceService                    deviceService,
    // ILicenseCacheService              licenseCacheService,
    ILogger<LicenseValidationService> logger) : ILicenseValidationService {
    public async Task<bool> ValidateDeviceAccessAsync(string deviceUuid) {
        // Check if device exists and is active
        try {
            var device = await deviceService.GetDeviceAsync(deviceUuid);
            if (device is not { Status: DeviceStatus.Active }) {
                logger.LogWarning("Device {DeviceUuid} not found or not active", deviceUuid);
                return false;
            }
        }
        catch {
            logger.LogWarning("Device {DeviceUuid} not found", deviceUuid);
            return false;
        }

        // Check account status
        bool systemAccess = await ValidateSystemAccessAsync();
        if (systemAccess) 
            return true;
        logger.LogWarning("System access denied for device {DeviceUuid} due to account status", deviceUuid);
        return false;
    }

    public async Task<bool> ValidateSystemAccessAsync() {
        var accountStatus = await accountStatusService.GetCurrentAccountStatusAsync();

        return accountStatus.Status switch {
            AccountState.Active or AccountState.PaymentDue or AccountState.PaymentDueUnknown or AccountState.Demo => true,
            AccountState.Disabled or AccountState.DemoExpired                                                     => false,
            _                                                                                                     => false
        };
    }

    public async Task<LicenseValidationResult> GetLicenseValidationResultAsync() {
        var accountStatus = await accountStatusService.GetCurrentAccountStatusAsync();
        // var licenseCache  = await licenseCacheService.GetLicenseCacheAsync(); // TODO check if I need this

        var result = new LicenseValidationResult {
            AccountStatus  = accountStatus.Status,
            ExpirationDate = accountStatus.ExpirationDate ?? accountStatus.DemoExpirationDate,
            IsValid        = await ValidateSystemAccessAsync()
        };

        if (result.ExpirationDate.HasValue) {
            int daysUntilExpiration = (int)(result.ExpirationDate.Value - DateTime.UtcNow).TotalDays;
            result.DaysUntilExpiration = Math.Max(0, daysUntilExpiration);
            result.IsInGracePeriod     = daysUntilExpiration is > 0 and <= 7;
        }

        // Determine warning message
        switch (accountStatus.Status) {
            case AccountState.PaymentDue:
                result.ShowWarning = true;
                result.Warning     = new LicenseWarning(LicenseWarningType.PaymentDue);
                break;
            case AccountState.PaymentDueUnknown:
                result.ShowWarning = true;
                result.Warning     = new LicenseWarning(LicenseWarningType.PaymentStatusUnknown, result.DaysUntilExpiration);
                break;
            default:
                if (result.IsInGracePeriod) {
                    result.ShowWarning = true;
                    result.Warning     = new LicenseWarning(LicenseWarningType.AccountExpiresIn, result.DaysUntilExpiration);
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