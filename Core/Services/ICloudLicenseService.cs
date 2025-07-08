using Core.Enums;
using Core.Models;

namespace Core.Services;

public interface ICloudLicenseService {
    Task<CloudLicenseResponse>      SendDeviceEventAsync(CloudLicenseRequest request);
    Task<AccountValidationResponse> ValidateAccountAsync(AccountValidationRequest request);
    Task<bool>                      IsCloudAvailableAsync();
    Task                            QueueDeviceEventAsync(CloudLicenseEvent eventType, string deviceUuid, string deviceName = "");
    Task                            ProcessQueuedEventsAsync();
    Task<int>                       GetPendingEventCountAsync();
}