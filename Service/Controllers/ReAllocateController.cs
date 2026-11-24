using System.Threading.Tasks;
using Core.DTOs.ReAllocate;
using Core.Enums;
using Core.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Middlewares;

namespace Service.Controllers;

/// <summary>
/// Re-Allocate: move an item's available bin stock into 1–3 target bins as one SAP stock transfer.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireRolePermission(RoleType.ReAllocate)]
public class ReAllocateController(IReAllocateService reAllocateService) : ControllerBase {
    private const string BinsRequired = "Re-allocate requires bin location management to be enabled";

    /// <summary>Bins holding the item in the session warehouse with on-hand, WMS-reserved and available quantities.</summary>
    [HttpGet("sources/{itemCode}")]
    [ProducesResponseType(typeof(ReAllocateSourcesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ReAllocateSourcesResponse>> Sources(string itemCode) {
        var sessionInfo = HttpContext.GetSession();
        if (!sessionInfo.EnableBinLocations) {
            return BadRequest(BinsRequired);
        }

        return Ok(await reAllocateService.GetSourcesAsync(itemCode, sessionInfo));
    }

    /// <summary>Re-validates availability and posts the whole re-allocation as a single SAP stock transfer.</summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ReAllocateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ReAllocateResponse>> Execute([FromBody] ReAllocateRequest request) {
        var sessionInfo = HttpContext.GetSession();
        if (!sessionInfo.EnableBinLocations) {
            return BadRequest(new ReAllocateResponse { Success = false, ErrorMessage = BinsRequired });
        }

        return Ok(await reAllocateService.ExecuteAsync(request, sessionInfo));
    }
}
