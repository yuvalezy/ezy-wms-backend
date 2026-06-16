using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers;

/// <summary>
/// System readiness for the lockdown gate. <c>status</c> is anonymous (and never
/// depends on SAP succeeding) so the frontend can decide whether to lock the UI;
/// <c>recheck</c> re-evaluates readiness after a superuser fixes the configuration.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemController(ISystemStatusService status, IUserService userService) : ControllerBase {

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult Status() {
        var s = status.Current;
        return Ok(new {
            ready         = s.Ready,
            sboConfigured = s.SboConfigured,
            detail        = s.Detail,
            checkedAtUtc  = s.CheckedAtUtc,
            version       = s.Version
        });
    }

    [HttpPost("recheck")]
    [Authorize]
    public async Task<IActionResult> Recheck() {
        if (!await IsUserSuperUser()) {
            return StatusCode(403, new {
                error = "forbidden",
                error_description = "Solo los super usuarios pueden re-verificar el sistema."
            });
        }
        var s = await status.RefreshAsync();
        return Ok(new {
            ready         = s.Ready,
            sboConfigured = s.SboConfigured,
            detail        = s.Detail,
            checkedAtUtc  = s.CheckedAtUtc,
            version       = s.Version
        });
    }

    private async Task<bool> IsUserSuperUser() {
        string? claim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId)) {
            return false;
        }
        var user = await userService.GetUserAsync(userId);
        return user?.SuperUser ?? false;
    }
}
