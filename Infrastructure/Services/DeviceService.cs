using Core.Entities;
using Core.Enums;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DeviceService(SystemDbContext context, ICloudLicenseService cloudService, ILogger<DeviceService> logger) : IDeviceService {
    public async Task<Device> RegisterDeviceAsync(string deviceUuid, string deviceName, SessionInfo sessionInfo) {
        // Check if device already exists
        var existingDevice = await context.Devices
            .FirstOrDefaultAsync(d => d.DeviceUuid == deviceUuid);

        if (existingDevice != null) {
            throw new InvalidOperationException("Device already registered");
        }

        var device = new Device {
            DeviceUuid       = deviceUuid,
            DeviceName       = deviceName,
            RegistrationDate = DateTime.UtcNow,
            Status           = DeviceStatus.Active,
            StatusNotes      = "Initial registration",
            LastActiveDate   = DateTime.UtcNow,
            CreatedByUserId  = sessionInfo.Guid
        };

        context.Devices.Add(device);
        await context.SaveChangesAsync();

        // Log audit record
        await LogDeviceStatusChangeAsync(device.Id, DeviceStatus.Active, DeviceStatus.Active,
            "Device registered", sessionInfo);

        // Queue cloud event
        await cloudService.QueueDeviceEventAsync("register", deviceUuid, deviceName);

        logger.LogInformation("Device {DeviceUuid} registered by user {UserId}", deviceUuid, sessionInfo.Guid);
        return device;
    }

    public async Task<Device?> GetDeviceAsync(string deviceUuid) {
        return await context.Devices
            .FirstOrDefaultAsync(d => d.DeviceUuid == deviceUuid);
    }

    public Task<bool> ValidateDeviceNameAvailable(string name) {
        return context.Devices
            .Where(d => d.DeviceName.ToLower() == name.ToLower())
            .AnyAsync();
    }

    public async Task<List<Device>> GetAllDevicesAsync() {
        return await context.Devices
            .Where(d => !d.Deleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Device> UpdateDeviceStatusAsync(string deviceUuid, DeviceStatus status, string reason, SessionInfo sessionInfo) {
        var device = await GetDeviceAsync(deviceUuid);
        if (device == null) {
            throw new InvalidOperationException("Device not found");
        }

        var previousStatus = device.Status;
        device.Status          = status;
        device.StatusNotes     = reason;
        device.UpdatedAt       = DateTime.UtcNow;
        device.UpdatedByUserId = sessionInfo?.Guid;

        if (status == DeviceStatus.Active) {
            device.LastActiveDate = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        // Log audit record
        await LogDeviceStatusChangeAsync(device.Id, previousStatus, status, reason, sessionInfo);

        // Queue cloud event
        string eventType = status switch {
            DeviceStatus.Active => "activate",
            DeviceStatus.Inactive => "deactivate",
            DeviceStatus.Disabled => "disable",
            _ => "update"
        };
        await cloudService.QueueDeviceEventAsync(eventType, deviceUuid);

        logger.LogInformation("Device {DeviceUuid} status changed from {PreviousStatus} to {NewStatus} by user {UserId}",
            deviceUuid, previousStatus, status, sessionInfo?.Guid);

        return device;
    }

    public async Task<Device> UpdateDeviceNameAsync(string deviceUuid, string newName, SessionInfo sessionInfo) {
        var device = await GetDeviceAsync(deviceUuid);
        if (device == null) {
            throw new InvalidOperationException("Device not found");
        }

        device.DeviceName      = newName;
        device.UpdatedAt       = DateTime.UtcNow;
        device.UpdatedByUserId = sessionInfo.Guid;

        await context.SaveChangesAsync();

        logger.LogInformation("Device {DeviceUuid} name updated to {NewName} by user {UserId}",
            deviceUuid, newName, sessionInfo.Guid);

        return device;
    }

    public async Task<List<DeviceAudit>> GetDeviceAuditHistoryAsync(string deviceUuid) {
        var device = await GetDeviceAsync(deviceUuid);
        if (device == null) {
            return new List<DeviceAudit>();
        }

        return await context.DeviceAudits
            .Where(a => a.DeviceId == device.Id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsDeviceActiveAsync(string deviceUuid) {
        var device = await GetDeviceAsync(deviceUuid);
        return device?.Status == DeviceStatus.Active;
    }

    private async Task LogDeviceStatusChangeAsync(Guid deviceId,  DeviceStatus previousStatus,
        DeviceStatus                                   newStatus, string       reason, SessionInfo sessionInfo) {
        var audit = new DeviceAudit {
            DeviceId        = deviceId,
            PreviousStatus  = previousStatus,
            NewStatus       = newStatus,
            Reason          = reason,
            CreatedByUserId = sessionInfo?.Guid
        };

        context.DeviceAudits.Add(audit);
        await context.SaveChangesAsync();
    }
}