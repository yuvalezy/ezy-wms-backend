using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.DTOs;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireSuperUser]
public class UserController(IUserService userService, IExternalSystemAdapter externalSystemAdapter, ILogger<UserController> logger) : ControllerBase {
    private Guid? GetCurrentUserId() {
        string? userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
    [HttpGet]
    public async Task<IActionResult> GetUsers() {
        try {
            var users = await userService.GetUsersAsync();
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
            var user = await userService.GetUserAsync(id);
            
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
            var newUser = await userService.CreateUserAsync(request);
            return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, newUser);
        }
        catch (InvalidOperationException ex) {
            return BadRequest(new { error = "invalid_request", error_description = ex.Message });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while creating the user." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request) {
        try {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) {
                return Unauthorized(new { error = "invalid_token", error_description = "User ID not found in token." });
            }
            
            bool success = await userService.UpdateUserAsync(id, request, currentUserId.Value);
            
            if (!success) {
                return NotFound(new { error = "user_not_found", error_description = "User not found." });
            }
            
            return Ok(new { message = "User updated successfully." });
        }
        catch (InvalidOperationException ex) {
            return BadRequest(new { error = "invalid_operation", error_description = ex.Message });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while updating the user." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id) {
        try {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) {
                return Unauthorized(new { error = "invalid_token", error_description = "User ID not found in token." });
            }
            
            bool success = await userService.DeleteUserAsync(id, currentUserId.Value);
            
            if (!success) {
                return NotFound(new { error = "user_not_found", error_description = "User not found." });
            }
            
            return Ok(new { message = "User deleted successfully." });
        }
        catch (InvalidOperationException ex) {
            return BadRequest(new { error = "invalid_operation", error_description = ex.Message });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while deleting the user." });
        }
    }

    [HttpPost("{id}/disable")]
    public async Task<IActionResult> DisableUser(Guid id) {
        try {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) {
                return Unauthorized(new { error = "invalid_token", error_description = "User ID not found in token." });
            }
            
            bool success = await userService.DisableUserAsync(id, currentUserId.Value);
            
            if (!success) {
                return NotFound(new { error = "user_not_found", error_description = "User not found." });
            }
            
            return Ok(new { message = "User disabled successfully." });
        }
        catch (InvalidOperationException ex) {
            return BadRequest(new { error = "invalid_operation", error_description = ex.Message });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error disabling user {UserId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while disabling the user." });
        }
    }

    [HttpPost("{id}/enable")]
    public async Task<IActionResult> EnableUser(Guid id) {
        try {
            bool success = await userService.EnableUserAsync(id);
            
            if (!success) {
                return NotFound(new { error = "user_not_found", error_description = "User not found." });
            }
            
            return Ok(new { message = "User enabled successfully." });
        }
        catch (InvalidOperationException ex) {
            return BadRequest(new { error = "invalid_operation", error_description = ex.Message });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error enabling user {UserId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while enabling the user." });
        }
    }

    [HttpGet("external")]
    public async Task<IActionResult> GetExternalUsers() {
        try {
            var externalUsers = await externalSystemAdapter.GetUsersAsync();
            return Ok(externalUsers);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving external users");
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while retrieving external users." });
        }
    }

    [HttpGet("external/{id}")]
    public async Task<IActionResult> GetExternalUser(string id) {
        try {
            var externalUser = await externalSystemAdapter.GetUserInfoAsync(id);
            
            if (externalUser == null) {
                return NotFound(new { error = "external_user_not_found", error_description = "External user not found." });
            }

            return Ok(externalUser);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving external user {ExternalUserId}", id);
            return StatusCode(500, new { error = "server_error", error_description = "An error occurred while retrieving the external user." });
        }
    }
}