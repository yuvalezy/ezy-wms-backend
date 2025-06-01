using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Core;
using Core.Entities;
using Core.Models;
using Core.Utils;
using Infrastructure.Auth;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Service.API.Users;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireSuperUser]
public class UserController(SystemDbContext dbContext, ILogger<UserController> logger) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> GetUsers() {
        try {
            var users = await dbContext.Users
                .Include(u => u.AuthorizationGroup)
                .Select(u => new UserResponse {
                    Id                     = u.Id,
                    FullName               = u.FullName,
                    Email                  = u.Email,
                    Position               = u.Position,
                    SuperUser              = u.SuperUser,
                    Active                 = u.Active,
                    AuthorizationGroupId   = u.AuthorizationGroupId,
                    AuthorizationGroupName = u.AuthorizationGroup != null ? u.AuthorizationGroup.Name : null,
                    CreatedAt              = u.CreatedAt,
                    UpdatedAt              = u.UpdatedAt
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while retrieving users." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id) {
        try {
            var user = await dbContext.Users
                .Include(u => u.AuthorizationGroup)
                .Where(u => u.Id == id)
                .Select(u => new UserResponse {
                    Id                     = u.Id,
                    FullName               = u.FullName,
                    Email                  = u.Email,
                    Position               = u.Position,
                    SuperUser              = u.SuperUser,
                    Active                 = u.Active,
                    AuthorizationGroupId   = u.AuthorizationGroupId,
                    AuthorizationGroupName = u.AuthorizationGroup != null ? u.AuthorizationGroup.Name : null,
                    CreatedAt              = u.CreatedAt,
                    UpdatedAt              = u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null) {
                return NotFound(new { error = "user_not_found", error_description = "User not found." });
            }

            return Ok(user);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while retrieving the user." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request) {
        try {
            // Validate authorization group if provided
            if (request.AuthorizationGroupId.HasValue) {
                var groupExists = await dbContext.AuthorizationGroups.AnyAsync(g => g.Id == request.AuthorizationGroupId.Value);
                if (!groupExists) {
                    return BadRequest(new { error = "invalid_group", error_description = "Authorization group not found." });
                }
            }

            var newUser = new User {
                Id                   = Guid.NewGuid(),
                FullName             = request.FullName,
                Password             = PasswordUtils.HashPasswordWithSalt(request.Password),
                Email                = request.Email,
                Position             = request.Position,
                SuperUser            = request.SuperUser,
                Active               = true,
                AuthorizationGroupId = request.AuthorizationGroupId,
                CreatedAt            = DateTime.UtcNow,
                UpdatedAt            = DateTime.UtcNow
            };

            dbContext.Users.Add(newUser);
            await dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, new UserResponse {
                Id                     = newUser.Id,
                FullName               = newUser.FullName,
                Email                  = newUser.Email,
                Position               = newUser.Position,
                SuperUser              = newUser.SuperUser,
                Active                 = newUser.Active,
                AuthorizationGroupId   = newUser.AuthorizationGroupId,
                AuthorizationGroupName = null,
                CreatedAt              = newUser.CreatedAt,
                UpdatedAt              = newUser.UpdatedAt
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while creating the user." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request) {
        try {
            var user = await dbContext.Users.FindAsync(id);
            if (user == null) {
                return NotFound(new { error = "user_not_found", error_description = "User not found." });
            }

            // Get current user ID from claims
            var currentUserIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(currentUserIdClaim) && Guid.TryParse(currentUserIdClaim, out var currentUserId)) {
                // Prevent user from removing their own super user status
                if (currentUserId == id && request.SuperUser == false && user.SuperUser) {
                    return BadRequest(new { error = "invalid_operation", error_description = "Cannot remove your own super user status." });
                }
            }

            // Validate authorization group if provided
            if (request.AuthorizationGroupId.HasValue) {
                var groupExists = await dbContext.AuthorizationGroups.AnyAsync(g => g.Id == request.AuthorizationGroupId.Value);
                if (!groupExists) {
                    return BadRequest(new { error = "invalid_group", error_description = "Authorization group not found." });
                }
            }

            // Update fields only if provided
            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName;

            if (!string.IsNullOrWhiteSpace(request.Password))
                user.Password = PasswordUtils.HashPasswordWithSalt(request.Password);

            if (request.Email != null)
                user.Email = request.Email;

            if (request.Position != null)
                user.Position = request.Position;

            if (request.SuperUser.HasValue)
                user.SuperUser = request.SuperUser.Value;

            if (request.AuthorizationGroupId != null)
                user.AuthorizationGroupId = request.AuthorizationGroupId;

            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return Ok(new { message = "User updated successfully." });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while updating the user." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id) {
        try {
            // Prevent deletion of default system user
            if (id == Const.DefaultUserId) {
                return BadRequest(new { error = "invalid_operation", error_description = "Cannot delete the default system user." });
            }

            // Get current user ID from claims
            var currentUserIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(currentUserIdClaim) && Guid.TryParse(currentUserIdClaim, out var currentUserId)) {
                // Prevent user from deleting themselves
                if (currentUserId == id) {
                    return BadRequest(new { error = "invalid_operation", error_description = "Cannot delete your own user account." });
                }
            }

            var user = await dbContext.Users.FindAsync(id);
            if (user == null) {
                return NotFound(new { error = "user_not_found", error_description = "User not found." });
            }

            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully." });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while deleting the user." });
        }
    }

    [HttpPost("{id}/disable")]
    public async Task<IActionResult> DisableUser(Guid id) {
        try {
            // Get current user ID from claims
            var currentUserIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(currentUserIdClaim) && Guid.TryParse(currentUserIdClaim, out var currentUserId)) {
                // Prevent user from disabling themselves
                if (currentUserId == id) {
                    return BadRequest(new { error = "invalid_operation", error_description = "Cannot disable your own user account." });
                }
            }

            var user = await dbContext.Users.FindAsync(id);
            if (user == null) {
                return NotFound(new { error = "user_not_found", error_description = "User not found." });
            }

            if (!user.Active) {
                return BadRequest(new { error = "invalid_operation", error_description = "User is already disabled." });
            }

            user.Active    = false;
            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return Ok(new { message = "User disabled successfully." });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error disabling user {UserId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while disabling the user." });
        }
    }

    [HttpPost("{id}/enable")]
    public async Task<IActionResult> EnableUser(Guid id) {
        try {
            var user = await dbContext.Users.FindAsync(id);
            if (user == null) {
                return NotFound(new { error = "user_not_found", error_description = "User not found." });
            }

            if (user.Active) {
                return BadRequest(new { error = "invalid_operation", error_description = "User is already enabled." });
            }

            user.Active    = true;
            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return Ok(new { message = "User enabled successfully." });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error enabling user {UserId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while enabling the user." });
        }
    }
}