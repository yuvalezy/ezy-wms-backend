using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Enums;
using Core.Models;
using Core.Models.Settings;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Service.Services;

public class CloudSyncBackgroundService(
    IServiceScopeFactory                 scopeFactory,
    ILogger<CloudSyncBackgroundService>  logger,
    IOptions<CloudSyncBackgroundOptions> options) : BackgroundService {
    private readonly CloudSyncBackgroundOptions options        = options.Value;
    private readonly SemaphoreSlim              syncSemaphore  = new(1, 1);
    private          DateTime                   lastSync       = DateTime.MinValue;
    private          DateTime                   lastValidation = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!options.Enabled) {
            logger.LogInformation("CloudSyncBackgroundService is disabled");
            return;
        }

        logger.LogInformation("CloudSyncBackgroundService started with sync interval of {SyncIntervalMinutes} minutes and validation interval of {ValidationIntervalHours} hours",
            options.SyncIntervalMinutes, options.ValidationIntervalHours);

        // Run validation immediately on startup
        await TriggerValidation();

        while (!stoppingToken.IsCancellationRequested) {
            try {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Check every minute

                if (stoppingToken.IsCancellationRequested)
                    break;

                var now = DateTime.UtcNow;

                // Check if sync is due
                if (now - lastSync >= TimeSpan.FromMinutes(options.SyncIntervalMinutes)) {
                    await TriggerSync();
                }

                // Check if validation is due
                if (now - lastValidation >= TimeSpan.FromHours(options.ValidationIntervalHours)) {
                    await TriggerValidation();
                }
            }
            catch (OperationCanceledException) {
                // Expected when cancellation is requested
            }
            catch (Exception ex) {
                logger.LogError(ex, "Error occurred during cloud sync background service");
            }
        }
    }

    private async Task TriggerSync() {
        // Ensure only one sync runs at a time
        if (!await syncSemaphore.WaitAsync(0)) {
            logger.LogInformation("Cloud sync already in progress, skipping");
            return;
        }

        try {
            lastSync = DateTime.UtcNow;
            logger.LogInformation("Starting cloud sync queue processing");

            using var scope        = scopeFactory.CreateScope();
            var       cloudService = scope.ServiceProvider.GetRequiredService<ICloudLicenseService>();

            await cloudService.ProcessQueuedEventsAsync();

            logger.LogInformation("Cloud sync queue processing completed");
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during cloud sync queue processing");
        }
        finally {
            syncSemaphore.Release();
        }
    }

    private async Task TriggerValidation() {
        // Ensure only one validation runs at a time
        if (!await syncSemaphore.WaitAsync(0)) {
            logger.LogInformation("Cloud validation already in progress, skipping");
            return;
        }

        try {
            lastValidation = DateTime.UtcNow;
            logger.LogInformation("Starting daily account validation");

            using var scope                = scopeFactory.CreateScope();
            var       cloudService         = scope.ServiceProvider.GetRequiredService<ICloudLicenseService>();
            var       deviceService        = scope.ServiceProvider.GetRequiredService<IDeviceService>();
            var       accountStatusService = scope.ServiceProvider.GetRequiredService<IAccountStatusService>();
            var       licenseCacheService  = scope.ServiceProvider.GetRequiredService<ILicenseCacheService>();

            // Get all active devices
            var devices       = await deviceService.GetAllDevicesAsync();
            var activeDevices = devices.Where(d => d.Status == DeviceStatus.Active).ToList();

            var validationRequest = new AccountValidationRequest {
                ActiveDeviceUuids       = activeDevices.Select(d => d.DeviceUuid).ToList(),
                LastValidationTimestamp = await licenseCacheService.GetLastValidationTimestampAsync() ?? DateTime.MinValue
            };

            var response = await cloudService.ValidateAccountAsync(validationRequest);

            if (response.Success && response.LicenseData != null) {
                // Update account status
                await accountStatusService.UpdateAccountStatusAsync(response.LicenseData.AccountStatus,
                    "Daily validation from cloud service");

                // Update license cache
                var cacheData = new LicenseCacheData {
                    AccountStatus           = response.LicenseData.AccountStatus,
                    ExpirationDate          = response.LicenseData.ExpirationDate,
                    PaymentCycleDate        = response.LicenseData.PaymentCycleDate,
                    DemoExpirationDate      = response.LicenseData.DemoExpirationDate,
                    InactiveReason          = response.LicenseData.InactiveReason,
                    LastValidationTimestamp = DateTime.UtcNow,
                    ActiveDeviceCount       = response.LicenseData.ActiveDeviceCount,
                    MaxAllowedDevices       = response.LicenseData.MaxAllowedDevices,
                    AdditionalData          = response.LicenseData.AdditionalData
                };

                await licenseCacheService.UpdateLicenseCacheAsync(cacheData);

                // Deactivate devices if requested
                if (response.DevicesToDeactivate.Count > 0) {
                    foreach (var deviceUuid in response.DevicesToDeactivate) {
                        // System action - pass null for sessionInfo since this is automated
                        await deviceService.UpdateDeviceStatusAsync(deviceUuid, DeviceStatus.Disabled,
                            "Deactivated by cloud service", null);
                    }
                }

                logger.LogInformation("Daily validation completed successfully");
            }
            else {
                logger.LogWarning("Daily validation failed: {Message}", response.Message);

                // Check if we should transition to PaymentDueUnknown
                var accountStatus = await accountStatusService.GetCurrentAccountStatusAsync();
                if (accountStatus.Status == AccountState.PaymentDue) {
                    await accountStatusService.UpdateAccountStatusAsync(AccountState.PaymentDueUnknown,
                        "Cloud service unreachable during validation");
                }
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during daily validation");
        }
        finally {
            syncSemaphore.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        logger.LogInformation("CloudSyncBackgroundService is stopping");
        await base.StopAsync(cancellationToken);
    }
}