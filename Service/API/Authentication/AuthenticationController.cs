using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Utils;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Service.API.Authentication;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController(IJwtAuthenticationService jwtService, ILogger<AuthenticationController> logger, SystemDbContext dbContext) : ControllerBase {
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

            return Ok(sessionInfo);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during login");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred during authentication." });
        }
    }
}