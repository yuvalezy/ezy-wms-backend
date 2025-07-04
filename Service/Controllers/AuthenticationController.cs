using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Core;
using Core.DTOs;
using Core.DTOs.General;
using Core.DTOs.Settings;
using Core.Exceptions;
using Core.Interfaces;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service.Controllers;

/// <summary>
/// Authentication Controller - Manages user authentication, session management, and password operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthenticationController(
    IAuthenticationService            authenticationService,
    ILogger<AuthenticationController> logger,
    ISessionManager                   sessionManager,
    IExternalSystemAdapter            externalSystemAdapter,
    ILicenseValidationService         licenseValidationService) : ControllerBase {
    /// <summary>
    /// Gets company information (no authentication required)
    /// </summary>
    /// <returns>The company name from the external system</returns>
    /// <response code="200">Returns the company name</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("CompanyName")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CompanyInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompanyInfoResponse>> GetCompanyInfo() {
        try {
            string? companyName = await sessionManager.GetStringAsync("CompanyName");
            if (string.IsNullOrEmpty(companyName)) {
                companyName = await externalSystemAdapter.GetCompanyNameAsync();
                await sessionManager.SetValueAsync("CompanyName", companyName ?? string.Empty, TimeSpan.FromDays(1));
            }

            var response = new CompanyInfoResponse{
                CompanyName     = companyName,
                ServerTime      = DateTime.UtcNow,
                LicenseWarnings = new List<string>()
            };

            // Add license warnings if past due by 3+ days
            try {
                var licenseValidation = await licenseValidationService.GetLicenseValidationResultAsync();
                if (licenseValidation is { ShowWarning: true, DaysUntilExpiration: <= -3 }) {
                    response.LicenseWarnings.Add(licenseValidation.WarningMessage ?? "License issue detected");
                }
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Error getting license warnings for company info");
            }

            return Ok(response);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error getting company name");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Authenticates a user with password-only login
    /// </summary>
    /// <param name="request">The login request containing password</param>
    /// <returns>Session information with JWT token and user details</returns>
    /// <response code="200">Returns session info with token and user details</response>
    /// <response code="400">If warehouse selection is required</response>
    /// <response code="401">If the password is invalid or account is disabled</response>
    /// <response code="500">If a server error occurs</response>
    /// <remarks>
    /// This endpoint uses password-only authentication. The system automatically identifies the user.
    /// Sets an HTTP-only cookie for session management and returns a JWT token for API access.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) {
        try {
            var sessionInfo = await authenticationService.LoginAsync(request);

            if (sessionInfo == null) {
                return Unauthorized(new { error = "invalid_grant", error_description = "Invalid password or account disabled." });
            }

            // Set HTTP-only cookie
            Response.Cookies.Append(Const.SessionCookieName, sessionInfo.Token, new CookieOptions {
                HttpOnly = true,
                Secure   = false, // Set to true in production
                SameSite = SameSiteMode.Lax,
                Expires  = sessionInfo.ExpiresAt
            });

            // Create response with license warnings
            var response = new {
                Token           = sessionInfo.Token,
                IsSuccess       = true,
                SessionInfo     = sessionInfo,
                LicenseWarnings = await GetLicenseWarningsAsync()
            };

            return Ok(response);
        }
        catch (WarehouseSelectionRequiredException ex) {
            logger.LogInformation("Warehouse selection required for login");
            return BadRequest(new {
                error             = "WAREHOUSE_SELECTION_REQUIRED",
                error_description = ex.Message,
                data = new {
                    warehouses = ex.AvailableWarehouses.Select(w => new { id = w.Id, name = w.Name })
                }
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during login");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred during authentication." });
        }
    }

    /// <summary>
    /// Changes the password for the authenticated user
    /// </summary>
    /// <param name="request">The password change request containing current and new passwords</param>
    /// <returns>Success message if password change was successful</returns>
    /// <response code="200">Returns success message</response>
    /// <response code="400">If the current password is incorrect</response>
    /// <response code="401">If the user is not authenticated or token is invalid</response>
    /// <response code="500">If a server error occurs</response>
    /// <remarks>
    /// Requires authentication. The user ID is extracted from the JWT token.
    /// Both current password verification and new password validation are performed.
    /// </remarks>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request) {
        try {
            // Get user ID from JWT claims
            string? userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
                return Unauthorized(new { error = "invalid_token", error_description = "User ID not found in token." });
            }

            bool success = await authenticationService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

            if (!success) {
                return BadRequest(new { error = "invalid_password", error_description = "Current password is incorrect or user not found." });
            }

            return Ok(new { message = "Password changed successfully." });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during password change");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while changing the password." });
        }
    }

    /// <summary>
    /// Logs out the authenticated user and clears session
    /// </summary>
    /// <returns>Success message if logout was successful</returns>
    /// <response code="200">Returns success message</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If a server error occurs</response>
    /// <remarks>
    /// Clears the session from the server-side session manager and removes the HTTP-only cookie.
    /// The JWT token becomes invalid after logout.
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout() {
        try {
            // Get the session token from the cookie
            string? sessionToken = Request.Cookies[Const.SessionCookieName];

            if (!string.IsNullOrEmpty(sessionToken)) {
                // Remove the session from the session manager
                await authenticationService.LogoutAsync(sessionToken);
            }

            // Clear the session cookie
            Response.Cookies.Delete(Const.SessionCookieName, new CookieOptions {
                HttpOnly = true,
                Secure   = false, // Set to true in production
                SameSite = SameSiteMode.Lax
            });

            return Ok(new { message = "Logged out successfully." });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred during logout." });
        }
    }

    /// <summary>
    /// Gets license status information
    /// </summary>
    /// <returns>License status details</returns>
    /// <response code="200">Returns license status</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("license-status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetLicenseStatus() {
        try {
            var validation = await licenseValidationService.GetLicenseValidationResultAsync();

            return Ok(new {
                IsValid             = validation.IsValid,
                AccountStatus       = validation.AccountStatus.ToString(),
                ExpirationDate      = validation.ExpirationDate,
                DaysUntilExpiration = validation.DaysUntilExpiration,
                IsInGracePeriod     = validation.IsInGracePeriod,
                WarningMessage      = validation.WarningMessage,
                ShowWarning         = validation.ShowWarning
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error getting license status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task<List<string>> GetLicenseWarningsAsync() {
        var warnings = new List<string>();

        try {
            var validation = await licenseValidationService.GetLicenseValidationResultAsync();

            if (validation.ShowWarning) {
                warnings.Add(validation.WarningMessage ?? "License issue detected");
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error getting license warnings");
        }

        return warnings;
    }
}