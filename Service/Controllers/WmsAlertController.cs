using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.DTOs.Alerts;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service.Controllers;

/// <summary>
/// WMS Alert Controller - Manages internal WMS notification alerts for users
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WmsAlertController(IWmsAlertService wmsAlertService, ILogger<WmsAlertController> logger) : ControllerBase {
    private Guid? GetCurrentUserId() {
        string? userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets alerts for the current user
    /// </summary>
    /// <param name="request">Optional filters for alerts (unread only, limit)</param>
    /// <returns>List of alerts for the current user</returns>
    /// <response code="200">Returns the list of alerts</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAlerts([FromQuery] WmsAlertRequest? request) {
        try {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) {
                return Unauthorized(new { error = "invalid_token", error_description = "User ID not found in token." });
            }

            request ??= new WmsAlertRequest();
            var alerts = await wmsAlertService.GetUserAlertsAsync(userId.Value, request.UnreadOnly ?? false, request.Limit);
            return Ok(alerts);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving alerts");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while retrieving alerts." });
        }
    }

    /// <summary>
    /// Gets the count of unread alerts for the current user
    /// </summary>
    /// <returns>The number of unread alerts</returns>
    /// <response code="200">Returns the unread count</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUnreadCount() {
        try {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) {
                return Unauthorized(new { error = "invalid_token", error_description = "User ID not found in token." });
            }

            var count = await wmsAlertService.GetUnreadCountAsync(userId.Value);
            return Ok(new { count });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving unread alert count");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while retrieving unread count." });
        }
    }

    /// <summary>
    /// Marks a specific alert as read
    /// </summary>
    /// <param name="id">The unique identifier of the alert</param>
    /// <returns>Success message if marked as read</returns>
    /// <response code="200">Returns success message</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="404">If the alert is not found or doesn't belong to the user</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("{id}/read")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkAsRead(Guid id) {
        try {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) {
                return Unauthorized(new { error = "invalid_token", error_description = "User ID not found in token." });
            }

            await wmsAlertService.MarkAsReadAsync(id, userId.Value);
            return Ok(new { message = "Alert marked as read." });
        }
        catch (KeyNotFoundException) {
            return NotFound(new { error = "alert_not_found", error_description = "Alert not found or does not belong to you." });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error marking alert as read {AlertId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while marking the alert as read." });
        }
    }

    /// <summary>
    /// Marks all alerts as read for the current user
    /// </summary>
    /// <returns>Success message if all alerts marked as read</returns>
    /// <response code="200">Returns success message</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("readAll")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkAllAsRead() {
        try {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) {
                return Unauthorized(new { error = "invalid_token", error_description = "User ID not found in token." });
            }

            await wmsAlertService.MarkAllAsReadAsync(userId.Value);
            return Ok(new { message = "All alerts marked as read." });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error marking all alerts as read");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while marking all alerts as read." });
        }
    }
}
