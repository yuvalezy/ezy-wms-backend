using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Core;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Utils;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace Service.API.Authentication;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController(ISessionManager sessionManager, IJwtAuthenticationService jwtService, ILogger<AuthenticationController> logger, SystemDbContext dbContext) : ControllerBase {
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) {
        try {
            // Find all users and check password
            var users = await dbContext.Users
                .Include(u => u.AuthorizationGroup)
                .ToListAsync();

            var authenticatedUser = users.FirstOrDefault(user => PasswordUtils.VerifyPassword(request.Password, user.Password));

            if (authenticatedUser == null) {
                return Unauthorized(new { error = "invalid_grant", error_description = "Invalid password." });
            }

            // Generate token
            var expiresAt = DateTime.UtcNow.Date.AddDays(1); // Expires at midnight
            string token     = jwtService.GenerateToken(authenticatedUser, expiresAt);

            var authorizations = authenticatedUser.AuthorizationGroup?.Authorizations ?? new List<Authorization>();

            var sessionInfo = new SessionInfo {
                UserId         = authenticatedUser.Id.ToString(),
                SuperUser      = authenticatedUser.SuperUser,
                Authorizations = authorizations,
                Token          = token,
                ExpiresAt      = expiresAt
            };
            
            await sessionManager.SetValueAsync(token, sessionInfo.ToJson(), TimeSpan.FromDays(1));
            
            Response.Cookies.Append(Const.SessionCookieName, token, new CookieOptions {
                HttpOnly = true,
                Secure   = false,
                SameSite = SameSiteMode.Lax,
                Expires  = expiresAt
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
            var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
                return Unauthorized(new { error = "invalid_token", error_description = "User ID not found in token." });
            }
            
            // Get user from database
            var user = await dbContext.Users.FindAsync(userId);
            if (user == null) {
                return NotFound(new { error = "user_not_found", error_description = "User not found." });
            }
            
            // Verify current password
            if (!PasswordUtils.VerifyPassword(request.CurrentPassword, user.Password)) {
                return BadRequest(new { error = "invalid_password", error_description = "Current password is incorrect." });
            }
            
            // Update password
            user.Password = PasswordUtils.HashPasswordWithSalt(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            
            await dbContext.SaveChangesAsync();
            
            return Ok(new { message = "Password changed successfully." });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during password change");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while changing the password." });
        }
    }
}