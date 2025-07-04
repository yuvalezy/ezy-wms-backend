using Core.Entities;
using Core.Enums;
using Core.Models;

namespace Core.Services;

public interface IDeviceService {
    Task<Device>            RegisterDeviceAsync(string deviceUuid, string deviceName, SessionInfo sessionInfo);
    Task<Device?>           GetDeviceAsync(string      deviceUuid);
    Task<bool>           ValidateDeviceNameAvailable(string     name);
    Task<List<Device>>      GetAllDevicesAsync();
    Task<Device>            UpdateDeviceStatusAsync(string    deviceUuid, DeviceStatus status,  string      reason, SessionInfo sessionInfo);
    Task<Device>            UpdateDeviceNameAsync(string      deviceUuid, string       newName, SessionInfo sessionInfo);
    Task<List<DeviceAudit>> GetDeviceAuditHistoryAsync(string deviceUuid);
    Task<bool>              IsDeviceActiveAsync(string        deviceUuid);
}