using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs.Settings;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExternalSystemAlertController(IExternalSystemAlertService alertService) : ControllerBase {

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExternalSystemAlertResponse>>> GetAlerts() {
        var alerts = await alertService.GetAlertsAsync();
        var response = alerts.Select(a => new ExternalSystemAlertResponse {
            Id = a.Id,
            ObjectType = a.ObjectType,
            ExternalUserId = a.ExternalUserId,
            Enabled = a.Enabled,
            CreatedAt = a.CreatedAt,
            CreatedByUserId = a.CreatedByUserId,
            UpdatedAt = a.UpdatedAt,
            UpdatedByUserId = a.UpdatedByUserId
        });
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ExternalSystemAlertResponse>> CreateAlert([FromBody] ExternalSystemAlertRequest request) {
        var userId = Guid.Parse(User.FindFirst("EmployeeID")!.Value);

        var alert = new ExternalSystemAlert {
            ObjectType = request.ObjectType,
            ExternalUserId = request.ExternalUserId,
            Enabled = request.Enabled
        };

        try {
            var created = await alertService.CreateAlertAsync(alert, userId);
            var response = new ExternalSystemAlertResponse {
                Id = created.Id,
                ObjectType = created.ObjectType,
                ExternalUserId = created.ExternalUserId,
                Enabled = created.Enabled,
                CreatedAt = created.CreatedAt,
                CreatedByUserId = created.CreatedByUserId,
                UpdatedAt = created.UpdatedAt,
                UpdatedByUserId = created.UpdatedByUserId
            };
            return CreatedAtAction(nameof(GetAlerts), new { id = created.Id }, response);
        }
        catch (Exception ex) {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExternalSystemAlertResponse>> UpdateAlert(Guid id, [FromBody] ExternalSystemAlertUpdateRequest request) {
        var userId = Guid.Parse(User.FindFirst("EmployeeID")!.Value);

        try {
            var updated = await alertService.UpdateAlertAsync(id, request.Enabled, userId);
            var response = new ExternalSystemAlertResponse {
                Id = updated.Id,
                ObjectType = updated.ObjectType,
                ExternalUserId = updated.ExternalUserId,
                Enabled = updated.Enabled,
                CreatedAt = updated.CreatedAt,
                CreatedByUserId = updated.CreatedByUserId,
                UpdatedAt = updated.UpdatedAt,
                UpdatedByUserId = updated.UpdatedByUserId
            };
            return Ok(response);
        }
        catch (KeyNotFoundException) {
            return NotFound(new { error = $"Alert with ID {id} not found" });
        }
        catch (Exception ex) {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAlert(Guid id) {
        try {
            await alertService.DeleteAlertAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException) {
            return NotFound(new { error = $"Alert with ID {id} not found" });
        }
        catch (Exception ex) {
            return BadRequest(new { error = ex.Message });
        }
    }
}
