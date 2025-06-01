using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Core;
using Core.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service.API.Authentication;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController(IAuthenticationService authenticationService, ILogger<AuthenticationController> logger) : ControllerBase {
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) {
        try {
            var sessionInfo = await authenticationService.LoginAsync(request.Password);

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
}