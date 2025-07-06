using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs.Device;
using Core.Enums;
using Core.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Service.Middlewares;

namespace Service.Controllers;

/// <summary>
/// Device Controller - Manages device registration and status for licensing system
/// </summary>
[ApiController]
[RequireSuperUser]
[Route("api/[controller]")]
[Authorize]
public class DeviceController(IDeviceService deviceService, ILogger<DeviceController> logger) : ControllerBase {
    /// <summary>
    /// Registers a new device in the system
    /// </summary>
    /// <param name="request">The device registration request</param>
    /// <returns>The registered device information</returns>
    /// <response code="200">Returns the registered device</response>
    /// <response code="400">If the device is already registered or request is invalid</response>
    /// <response code="403">If the user is not a superuser</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceResponse>> RegisterDevice([FromBody] RegisterDeviceRequest request) {
        try {
            var sessionInfo = HttpContext.GetSession();
            var device = await deviceService.RegisterDeviceAsync(
                request.DeviceUuid, request.DeviceName, sessionInfo);

            return Ok(new DeviceResponse {
                Id               = device.Id,
                DeviceUuid       = device.DeviceUuid,
                DeviceName       = device.DeviceName,
                Status           = device.Status.ToString(),
                RegistrationDate = device.RegistrationDate,
                StatusNotes      = device.StatusNotes,
                LastActiveDate   = device.LastActiveDate
            });
        }
        catch (InvalidOperationException ex) {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error registering device {DeviceUuid}", request.DeviceUuid);
            return BadRequest(new { error = "Failed to register device" });
        }
    }

    /// <summary>
    /// Gets all registered devices
    /// </summary>
    /// <returns>List of all devices</returns>
    /// <response code="200">Returns the list of devices</response>
    /// <response code="403">If the user is not a superuser</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<DeviceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<DeviceResponse>>> GetAllDevices() {
        try {
            var devices = await deviceService.GetAllDevicesAsync();
            var response = devices.Select(d => new DeviceResponse {
                Id               = d.Id,
                DeviceUuid       = d.DeviceUuid,
                DeviceName       = d.DeviceName,
                Status           = d.Status.ToString(),
                RegistrationDate = d.RegistrationDate,
                StatusNotes      = d.StatusNotes,
                LastActiveDate   = d.LastActiveDate
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error getting all devices");
            return BadRequest(new { error = "Failed to get devices" });
        }
    }

    /// <summary>
    /// Gets a specific device by UUID
    /// </summary>
    /// <param name="deviceUuid">The device UUID</param>
    /// <returns>The device information</returns>
    /// <response code="200">Returns the device</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="403">If the user is not a superuser</response>
    [HttpGet("{deviceUuid}")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceResponse>> GetDevice(string deviceUuid) {
        try {
            var device = await deviceService.GetDeviceAsync(deviceUuid);
            if (device == null) {
                return NotFound(new { error = "Device not found" });
            }

            return Ok(new DeviceResponse {
                Id               = device.Id,
                DeviceUuid       = device.DeviceUuid,
                DeviceName       = device.DeviceName,
                Status           = device.Status.ToString(),
                RegistrationDate = device.RegistrationDate,
                StatusNotes      = device.StatusNotes,
                LastActiveDate   = device.LastActiveDate
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error getting device {DeviceUuid}", deviceUuid);
            return BadRequest(new { error = "Failed to get device" });
        }
    }

    /// <summary>
    /// Updates the status of a device
    /// </summary>
    /// <param name="deviceUuid">The device UUID</param>
    /// <param name="request">The status update request</param>
    /// <returns>The updated device information</returns>
    /// <response code="200">Returns the updated device</response>
    /// <response code="400">If the status is invalid</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="403">If the user is not a superuser</response>
    [HttpPut("{deviceUuid}/status")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceResponse>> UpdateDeviceStatus(
        string deviceUuid, [FromBody] UpdateDeviceStatusRequest request) {
        try {
            var sessionInfo = HttpContext.GetSession();

            if (!Enum.TryParse<DeviceStatus>(request.Status, out var status)) {
                return BadRequest(new { error = "Invalid device status" });
            }

            var device = await deviceService.UpdateDeviceStatusAsync(
                deviceUuid, status, request.Reason, sessionInfo);

            return Ok(new DeviceResponse {
                Id               = device.Id,
                DeviceUuid       = device.DeviceUuid,
                DeviceName       = device.DeviceName,
                Status           = device.Status.ToString(),
                RegistrationDate = device.RegistrationDate,
                StatusNotes      = device.StatusNotes,
                LastActiveDate   = device.LastActiveDate
            });
        }
        catch (InvalidOperationException ex) {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error updating device status for {DeviceUuid}", deviceUuid);
            return BadRequest(new { error = "Failed to update device status" });
        }
    }

    /// <summary>
    /// Updates the name of a device
    /// </summary>
    /// <param name="deviceUuid">The device UUID</param>
    /// <param name="request">The name update request</param>
    /// <returns>The updated device information</returns>
    /// <response code="200">Returns the updated device</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="403">If the user is not a superuser</response>
    [HttpPut("{deviceUuid}/name")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceResponse>> UpdateDeviceName(
        string deviceUuid, [FromBody] UpdateDeviceNameRequest request) {
        try {
            var sessionInfo = HttpContext.GetSession();
            var device = await deviceService.UpdateDeviceNameAsync(
                deviceUuid, request.DeviceName, sessionInfo);

            return Ok(new DeviceResponse {
                Id               = device.Id,
                DeviceUuid       = device.DeviceUuid,
                DeviceName       = device.DeviceName,
                Status           = device.Status.ToString(),
                RegistrationDate = device.RegistrationDate,
                StatusNotes      = device.StatusNotes,
                LastActiveDate   = device.LastActiveDate
            });
        }
        catch (InvalidOperationException ex) {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error updating device name for {DeviceUuid}", deviceUuid);
            return BadRequest(new { error = "Failed to update device name" });
        }
    }

    /// <summary>
    /// Gets the audit history for a device
    /// </summary>
    /// <param name="deviceUuid">The device UUID</param>
    /// <returns>List of audit records for the device</returns>
    /// <response code="200">Returns the audit history</response>
    /// <response code="403">If the user is not a superuser</response>
    [HttpGet("{deviceUuid}/audit")]
    [ProducesResponseType(typeof(List<DeviceAuditResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<DeviceAuditResponse>>> GetDeviceAuditHistory(string deviceUuid) {
        try {
            var auditHistory = await deviceService.GetDeviceAuditHistoryAsync(deviceUuid);
            var response = auditHistory.Select(a => new DeviceAuditResponse {
                PreviousStatus = a.PreviousStatus.ToString(),
                NewStatus      = a.NewStatus.ToString(),
                Reason         = a.Reason,
                ChangedAt      = a.CreatedAt,
                ChangedByUser  = a.CreatedByUserId?.ToString()
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error getting audit history for device {DeviceUuid}", deviceUuid);
            return BadRequest(new { error = "Failed to get audit history" });
        }
    }
}