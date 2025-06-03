using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs;
using Core.Interfaces;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireSuperUser]
public class AuthorizationGroupController(IAuthorizationGroupService authorizationGroupService) : ControllerBase {
    [HttpPost, RequireSuperUser]
    public async Task<ActionResult<AuthorizationGroupResponse>> Create([FromBody] CreateAuthorizationGroupRequest request) {
        try {
            var response = await authorizationGroupService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (InvalidOperationException ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut, RequireSuperUser]
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

    [HttpDelete("{id:guid}"), RequireSuperUser]
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuthorizationGroupResponse>> GetById(Guid id) {
        var authGroup = await authorizationGroupService.GetByIdAsync(id);
        if (authGroup == null) {
            return NotFound($"Authorization group with ID {id} not found.");
        }
        return Ok(authGroup);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuthorizationGroupResponse>>> GetAll() {
        var authGroups = await authorizationGroupService.GetAllAsync();
        return Ok(authGroups);
    }
}