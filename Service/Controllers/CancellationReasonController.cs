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
/// Cancellation Reason Controller - Manages cancellation reasons for various operations (super user only for modifications)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireSuperUser]
public class CancellationReasonController(ICancellationReasonService cancellationReasonService) : ControllerBase {
    /// <summary>
    /// Creates a new cancellation reason (super user only)
    /// </summary>
    /// <param name="request">The cancellation reason creation request</param>
    /// <returns>The created cancellation reason details</returns>
    /// <response code="201">Returns the created cancellation reason</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost]
    [ProducesResponseType(typeof(CancellationReasonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CancellationReasonResponse>> Create([FromBody] CreateCancellationReasonRequest request) {
        var response = await cancellationReasonService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Updates an existing cancellation reason (super user only)
    /// </summary>
    /// <param name="request">The cancellation reason update request</param>
    /// <returns>The updated cancellation reason details</returns>
    /// <response code="200">Returns the updated cancellation reason</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="404">If the cancellation reason is not found</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPut]
    [ProducesResponseType(typeof(CancellationReasonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CancellationReasonResponse>> Update([FromBody] UpdateCancellationReasonRequest request) {
        try {
            var response = await cancellationReasonService.UpdateAsync(request);
            return Ok(response);
        }
        catch (KeyNotFoundException ex) {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a cancellation reason (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the cancellation reason to delete</param>
    /// <returns>No content if deletion was successful</returns>
    /// <response code="204">Cancellation reason deleted successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="404">If the cancellation reason is not found</response>
    /// <response code="500">If a server error occurs</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id) {
        try {
            var result = await cancellationReasonService.DeleteAsync(id);
            if (!result) {
                return NotFound($"Cancellation reason with ID {id} not found.");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex) {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets all cancellation reasons with optional filtering (authenticated users only)
    /// </summary>
    /// <param name="request">The request containing filter criteria</param>
    /// <returns>A list of cancellation reasons matching the filter criteria</returns>
    /// <response code="200">Returns the list of cancellation reasons</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If a server error occurs</response>
    /// <remarks>
    /// This endpoint allows all authenticated users to read cancellation reasons, not just super users.
    /// </remarks>
    [HttpGet]
    [AllowAnonymous] // Allow non-superusers to read cancellation reasons
    [Authorize]      // But still require authentication
    [ProducesResponseType(typeof(IEnumerable<CancellationReasonResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CancellationReasonResponse>>> GetAll([FromQuery] GetCancellationReasonsRequest request) {
        var reasons = await cancellationReasonService.GetAllAsync(request);
        return Ok(reasons);
    }

    /// <summary>
    /// Gets a specific cancellation reason by its ID (authenticated users only)
    /// </summary>
    /// <param name="id">The unique identifier of the cancellation reason</param>
    /// <returns>The cancellation reason details</returns>
    /// <response code="200">Returns the cancellation reason details</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="404">If the cancellation reason is not found</response>
    /// <response code="500">If a server error occurs</response>
    /// <remarks>
    /// This endpoint allows all authenticated users to read cancellation reasons, not just super users.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [AllowAnonymous] // Allow non-superusers to read cancellation reasons
    [Authorize]      // But still require authentication
    [ProducesResponseType(typeof(CancellationReasonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CancellationReasonResponse>> GetById(Guid id) {
        var reason = await cancellationReasonService.GetByIdAsync(id);
        if (reason == null) {
            return NotFound($"Cancellation reason with ID {id} not found.");
        }

        return Ok(reason);
    }
}