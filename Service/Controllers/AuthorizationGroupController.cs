using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs.Settings;
using Core.Interfaces;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers;

/// <summary>
/// Authorization Group Controller - Manages authorization groups and role assignments (super user only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireSuperUser]
public class AuthorizationGroupController(IAuthorizationGroupService authorizationGroupService) : ControllerBase {
    /// <summary>
    /// Creates a new authorization group (super user only)
    /// </summary>
    /// <param name="request">The authorization group creation request</param>
    /// <returns>The created authorization group details</returns>
    /// <response code="201">Returns the created authorization group</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost]
    [RequireSuperUser]
    [ProducesResponseType(typeof(AuthorizationGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthorizationGroupResponse>> Create([FromBody] CreateAuthorizationGroupRequest request) {
        try {
            var response = await authorizationGroupService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (InvalidOperationException ex) {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing authorization group (super user only)
    /// </summary>
    /// <param name="request">The authorization group update request</param>
    /// <returns>The updated authorization group details</returns>
    /// <response code="200">Returns the updated authorization group</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="404">If the authorization group is not found</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPut]
    [RequireSuperUser]
    [ProducesResponseType(typeof(AuthorizationGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthorizationGroupResponse>> Update([FromBody] UpdateAuthorizationGroupRequest request) {
        try {
            var response = await authorizationGroupService.UpdateAsync(request);
            return Ok(response);
        }
        catch (KeyNotFoundException ex) {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex) {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes an authorization group (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the authorization group to delete</param>
    /// <returns>No content if deletion was successful</returns>
    /// <response code="204">Authorization group deleted successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="404">If the authorization group is not found</response>
    /// <response code="500">If a server error occurs</response>
    [HttpDelete("{id:guid}")]
    [RequireSuperUser]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id) {
        try {
            var result = await authorizationGroupService.DeleteAsync(id);
            if (!result) {
                return NotFound($"Authorization group with ID {id} not found.");
            }
            return NoContent();
        }
        catch (InvalidOperationException ex) {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets a specific authorization group by its ID (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the authorization group</param>
    /// <returns>The authorization group details</returns>
    /// <response code="200">Returns the authorization group details</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="404">If the authorization group is not found</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuthorizationGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthorizationGroupResponse>> GetById(Guid id) {
        var authGroup = await authorizationGroupService.GetByIdAsync(id);
        if (authGroup == null) {
            return NotFound($"Authorization group with ID {id} not found.");
        }
        return Ok(authGroup);
    }

    /// <summary>
    /// Gets all authorization groups in the system (super user only)
    /// </summary>
    /// <returns>A list of all authorization groups</returns>
    /// <response code="200">Returns the list of authorization groups</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuthorizationGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AuthorizationGroupResponse>>> GetAll() {
        var authGroups = await authorizationGroupService.GetAllAsync();
        return Ok(authGroups);
    }
}