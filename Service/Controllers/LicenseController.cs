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
    IAccountStatusService      accountStatusService,
    ILogger<LicenseController> logger) : ControllerBase {
    [HttpGet("status")]
    public async Task<ActionResult<LicenseStatusResponse>> GetStatus() {
        try {
            var validation = await licenseValidationService.GetLicenseValidationResultAsync();

            return Ok(new LicenseStatusResponse {
                IsValid             = validation.IsValid,
                AccountStatus       = await accountStatusService.GetCurrentAccountStatusAsync(),
                ExpirationDate      = validation.ExpirationDate,
                DaysUntilExpiration = validation.DaysUntilExpiration,
                IsInGracePeriod     = validation.IsInGracePeriod,
                Warning             = validation.Warning,
                ShowWarning         = validation.ShowWarning
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
            int pendingCount   = await cloudService.GetPendingEventCountAsync();
            bool cloudAvailable = await cloudService.IsCloudAvailableAsync();

            return Ok(new QueueStatusResponse {
                PendingEventCount     = pendingCount,
                CloudServiceAvailable = cloudAvailable,
                LastChecked           = DateTime.UtcNow
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
}