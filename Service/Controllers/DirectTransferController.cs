using System.Threading.Tasks;
using Core.DTOs.DirectTransfer;
using Core.Enums;
using Core.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Middlewares;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DirectTransferController(IDirectTransferService directTransferService) : ControllerBase {

    [HttpPost("execute")]
    [RequireRolePermission(RoleType.DirectTransfer)]
    [ProducesResponseType(typeof(DirectTransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DirectTransferResponse>> Execute([FromBody] DirectTransferRequest request) {
        var sessionInfo = HttpContext.GetSession();

        if (!sessionInfo.EnableBinLocations) {
            return BadRequest(new DirectTransferResponse {
                Success = false,
                ErrorMessage = "Direct transfer requires bin location management to be enabled"
            });
        }

        var result = await directTransferService.ExecuteAsync(request, sessionInfo);
        return Ok(result);
    }
}
