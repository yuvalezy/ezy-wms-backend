using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.DTOs.Settings;
using Core.Interfaces;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service.Controllers;

/// <summary>
/// User Controller - Manages user operations including CRUD operations, enabling/disabling users, and external user integration (super user only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireSuperUser]
public class UserController(IUserService userService, IExternalSystemAdapter externalSystemAdapter, ILogger<UserController> logger) : ControllerBase {
    private Guid? GetCurrentUserId() {
        string? userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
    /// <summary>
    /// Gets all users in the system (super user only)
    /// </summary>
    /// <returns>A list of all users</returns>
    /// <response code="200">Returns the list of users</response>
    /// <response code="500">If a server error occurs</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Gets a specific user by their ID (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <returns>The user details</returns>
    /// <response code="200">Returns the user details</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If a server error occurs</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    
    /// <summary>
    /// Creates a new user (super user only)
    /// </summary>
    /// <param name="request">The user creation request containing user details</param>
    /// <returns>The created user details</returns>
    /// <response code="201">Returns the created user</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If a server error occurs</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Updates an existing user (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the user to update</param>
    /// <param name="request">The user update request containing updated details</param>
    /// <returns>Success message if update was successful</returns>
    /// <response code="200">Returns success message</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If a server error occurs</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Deletes a user (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete</param>
    /// <returns>Success message if deletion was successful</returns>
    /// <response code="200">Returns success message</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If a server error occurs</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Disables a user account (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the user to disable</param>
    /// <returns>Success message if disabling was successful</returns>
    /// <response code="200">Returns success message</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If a server error occurs</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{id}/disable")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Enables a user account (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the user to enable</param>
    /// <returns>Success message if enabling was successful</returns>
    /// <response code="200">Returns success message</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If a server error occurs</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{id}/enable")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Gets all users from the external system (super user only)
    /// </summary>
    /// <returns>A list of external system users</returns>
    /// <response code="200">Returns the list of external users</response>
    /// <response code="500">If a server error occurs</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("external")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Gets a specific user from the external system by their ID (super user only)
    /// </summary>
    /// <param name="id">The external system user identifier</param>
    /// <returns>The external user details</returns>
    /// <response code="200">Returns the external user details</response>
    /// <response code="404">If the external user is not found</response>
    /// <response code="500">If a server error occurs</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("external/{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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