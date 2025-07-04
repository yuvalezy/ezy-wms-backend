using System;
using System.Threading.Tasks;
using Core.DTOs.License;
using Core.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenseController(
    ILicenseValidationService  licenseValidationService,
    ICloudLicenseService       cloudService,
    IDeviceService             deviceService,
    ILogger<LicenseController> logger) : ControllerBase {

    [HttpGet("status")]
    public async Task<ActionResult<LicenseStatusResponse>> GetStatus() {
        try {
            var validation = await licenseValidationService.GetLicenseValidationResultAsync();
            
            return Ok(new LicenseStatusResponse {
                IsValid = validation.IsValid,
                AccountStatus = validation.AccountStatus.ToString(),
                ExpirationDate = validation.ExpirationDate,
                DaysUntilExpiration = validation.DaysUntilExpiration,
                IsInGracePeriod = validation.IsInGracePeriod,
                WarningMessage = validation.WarningMessage,
                ShowWarning = validation.ShowWarning
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error getting license status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("queue-status")]
    [RequireSuperUser]
    public async Task<ActionResult<QueueStatusResponse>> GetQueueStatus() {
        try {
            var pendingCount = await cloudService.GetPendingEventCountAsync();
            var cloudAvailable = await cloudService.IsCloudAvailableAsync();
            
            return Ok(new QueueStatusResponse {
                PendingEventCount = pendingCount,
                CloudServiceAvailable = cloudAvailable,
                LastChecked = DateTime.UtcNow
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error getting queue status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("force-sync")]
    [RequireSuperUser]
    public async Task<ActionResult> ForceSync() {
        try {
            await cloudService.ProcessQueuedEventsAsync();
            return Ok(new { message = "Sync initiated successfully" });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error forcing sync");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("validate-device")]
    public async Task<ActionResult<DeviceValidationResponse>> ValidateDevice([FromBody] DeviceValidationRequest request) {
        try {
            var isValid = await licenseValidationService.ValidateDeviceAccessAsync(request.DeviceUuid);
            var device = await deviceService.GetDeviceAsync(request.DeviceUuid);
            
            return Ok(new DeviceValidationResponse {
                IsValid = isValid,
                DeviceStatus = device?.Status.ToString(),
                ValidationTimestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error validating device");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}