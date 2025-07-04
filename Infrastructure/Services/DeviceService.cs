using Core.Entities;
using Core.Enums;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly SystemDbContext _context;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(SystemDbContext context, ILogger<DeviceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Device> RegisterDeviceAsync(string deviceUuid, string deviceName, SessionInfo sessionInfo)
        {
            // Check if device already exists
            var existingDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceUuid == deviceUuid);

            if (existingDevice != null)
            {
                throw new InvalidOperationException("Device already registered");
            }

            var device = new Device
            {
                DeviceUuid = deviceUuid,
                DeviceName = deviceName,
                RegistrationDate = DateTime.UtcNow,
                Status = DeviceStatus.Active,
                StatusNotes = "Initial registration",
                LastActiveDate = DateTime.UtcNow,
                CreatedByUserId = sessionInfo.Guid
            };

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            // Log audit record
            await LogDeviceStatusChangeAsync(device.Id, DeviceStatus.Active, DeviceStatus.Active, 
                "Device registered", sessionInfo);

            _logger.LogInformation("Device {DeviceUuid} registered by user {UserId}", deviceUuid, sessionInfo.Guid);
            return device;
        }

        public async Task<Device> GetDeviceAsync(string deviceUuid)
        {
            return await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceUuid == deviceUuid);
        }

        public async Task<List<Device>> GetAllDevicesAsync()
        {
            return await _context.Devices
                .Where(d => !d.Deleted)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<Device> UpdateDeviceStatusAsync(string deviceUuid, DeviceStatus status, string reason, SessionInfo sessionInfo)
        {
            var device = await GetDeviceAsync(deviceUuid);
            if (device == null)
            {
                throw new InvalidOperationException("Device not found");
            }

            var previousStatus = device.Status;
            device.Status = status;
            device.StatusNotes = reason;
            device.UpdatedAt = DateTime.UtcNow;
            device.UpdatedByUserId = sessionInfo?.Guid;

            if (status == DeviceStatus.Active)
            {
                device.LastActiveDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Log audit record
            await LogDeviceStatusChangeAsync(device.Id, previousStatus, status, reason, sessionInfo);

            _logger.LogInformation("Device {DeviceUuid} status changed from {PreviousStatus} to {NewStatus} by user {UserId}", 
                deviceUuid, previousStatus, status, sessionInfo?.Guid);

            return device;
        }

        public async Task<Device> UpdateDeviceNameAsync(string deviceUuid, string newName, SessionInfo sessionInfo)
        {
            var device = await GetDeviceAsync(deviceUuid);
            if (device == null)
            {
                throw new InvalidOperationException("Device not found");
            }

            device.DeviceName = newName;
            device.UpdatedAt = DateTime.UtcNow;
            device.UpdatedByUserId = sessionInfo.Guid;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Device {DeviceUuid} name updated to {NewName} by user {UserId}", 
                deviceUuid, newName, sessionInfo.Guid);

            return device;
        }

        public async Task<List<DeviceAudit>> GetDeviceAuditHistoryAsync(string deviceUuid)
        {
            var device = await GetDeviceAsync(deviceUuid);
            if (device == null)
            {
                return new List<DeviceAudit>();
            }

            return await _context.DeviceAudits
                .Where(a => a.DeviceId == device.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsDeviceActiveAsync(string deviceUuid)
        {
            var device = await GetDeviceAsync(deviceUuid);
            return device?.Status == DeviceStatus.Active;
        }

        private async Task LogDeviceStatusChangeAsync(Guid deviceId, DeviceStatus previousStatus, 
            DeviceStatus newStatus, string reason, SessionInfo sessionInfo)
        {
            var audit = new DeviceAudit
            {
                DeviceId = deviceId,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                Reason = reason,
                CreatedByUserId = sessionInfo?.Guid
            };

            _context.DeviceAudits.Add(audit);
            await _context.SaveChangesAsync();
        }
    }
}