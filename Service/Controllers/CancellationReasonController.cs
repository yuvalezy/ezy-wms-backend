using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireSuperUser]
public class CancellationReasonController(ICancellationReasonService cancellationReasonService) : ControllerBase {
    [HttpPost]
    public async Task<ActionResult<CancellationReasonResponse>> Create([FromBody] CreateCancellationReasonRequest request) {
        var response = await cancellationReasonService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut]
    public async Task<ActionResult<CancellationReasonResponse>> Update([FromBody] UpdateCancellationReasonRequest request) {
        try {
            var response = await cancellationReasonService.UpdateAsync(request);
            return Ok(response);
        }
        catch (KeyNotFoundException ex) {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
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

    [HttpGet]
    [AllowAnonymous] // Allow non-superusers to read cancellation reasons
    [Authorize] // But still require authentication
    public async Task<ActionResult<IEnumerable<CancellationReasonResponse>>> GetAll([FromQuery] GetCancellationReasonsRequest request) {
        var reasons = await cancellationReasonService.GetAllAsync(request);
        return Ok(reasons);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous] // Allow non-superusers to read cancellation reasons
    [Authorize] // But still require authentication
    public async Task<ActionResult<CancellationReasonResponse>> GetById(Guid id) {
        var reason = await cancellationReasonService.GetByIdAsync(id);
        if (reason == null) {
            return NotFound($"Cancellation reason with ID {id} not found.");
        }
        return Ok(reason);
    }
}