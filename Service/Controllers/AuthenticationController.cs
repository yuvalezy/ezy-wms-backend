using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Core;
using Core.DTOs;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController(
    IAuthenticationService            authenticationService,
    ILogger<AuthenticationController> logger,
    ISessionManager                   sessionManager,
    IExternalSystemAdapter            externalSystemAdapter) : ControllerBase {
    [HttpGet("CompanyName"), AllowAnonymous]
    public async Task<ActionResult<string>> GetCompanyInfo() {
        string? companyName = await sessionManager.GetStringAsync("CompanyName");
        if (!string.IsNullOrEmpty(companyName)) {
            return Ok(companyName);
        }

        companyName = await externalSystemAdapter.GetCompanyNameAsync();
        await sessionManager.SetValueAsync("CompanyName", companyName ?? string.Empty, TimeSpan.FromDays(1));
        return Ok(companyName);
    }

    [AllowAnonymous]
    [HttpPost("login")]
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

            return Ok(sessionInfo);
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

    [Authorize]
    [HttpPost("change-password")]
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

    [Authorize]
    [HttpPost("logout")]
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
}