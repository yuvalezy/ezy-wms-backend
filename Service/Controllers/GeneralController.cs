using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs;
using Core.DTOs.General;
using Core.DTOs.Items;
using Core.DTOs.Settings;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Middlewares;

namespace Service.Controllers;

/// <summary>
/// General Controller - Provides general system information, warehouses, user data, and item/barcode scanning utilities
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeneralController(IPublicService publicService, ISettings settings) : ControllerBase {
    /// <summary>
    /// Gets a list of available warehouses with optional filtering
    /// </summary>
    /// <param name="filter">Optional array of warehouse codes to filter by</param>
    /// <returns>A list of warehouses available to the authenticated user</returns>
    /// <response code="200">Returns the list of warehouses</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("Warehouses")]
    [ProducesResponseType(typeof(IEnumerable<ExternalValue<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ExternalValue<string>>>> GetWarehouses([FromQuery] string[]? filter = null) {
        var warehouses = await publicService.GetWarehousesAsync(filter);
        return Ok(warehouses);
    }

    /// <summary>
    /// Gets home dashboard information for the authenticated user
    /// </summary>
    /// <returns>Dashboard statistics and information for the user's warehouse</returns>
    /// <response code="200">Returns the home dashboard information</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("HomeInfo")]
    [ProducesResponseType(typeof(HomeInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HomeInfoResponse>> GetHomeInfo() {
        var response = await publicService.GetHomeInfoAsync(HttpContext.GetSession().Warehouse);
        return Ok(response);
    }

    /// <summary>
    /// Gets user information for the authenticated user including device status
    /// </summary>
    /// <returns>User profile information, session details, and device status</returns>
    /// <response code="200">Returns the user information</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("UserInfo")]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserInfoResponse>> GetUserInfo() {
        var sessionInfo = HttpContext.GetSession();
        
        // Get device UUID from header if not in session
        if (string.IsNullOrEmpty(sessionInfo.DeviceUuid)) {
            var deviceUuid = HttpContext.Request.Headers["X-Device-UUID"].FirstOrDefault();
            if (!string.IsNullOrEmpty(deviceUuid)) {
                sessionInfo.DeviceUuid = deviceUuid;
            }
        }
        
        return await publicService.GetUserInfoAsync(sessionInfo);
    }

    /// <summary>
    /// Gets a list of vendors from the external system (goods receipt roles only)
    /// </summary>
    /// <returns>A list of vendor information</returns>
    /// <response code="200">Returns the list of vendors</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks required goods receipt permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("Vendors")]
    [RequireAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor, RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<ExternalValue<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ExternalValue<string>>>> GetVendors() {
        var response = await publicService.GetVendorsAsync();
        return Ok(response);
    }

    /// <summary>
    /// Gets the configured package metadata field definitions
    /// </summary>
    /// <returns>Array of metadata field definitions</returns>
    /// <response code="200">Returns the metadata field definitions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("package-metadata-definitions")]
    [ProducesResponseType(typeof(MetadataDefinition[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<MetadataDefinition[]> GetMetadataDefinitions() {
        return Ok(settings.Package.MetadataDefinition);
    }

    /// <summary>
    /// Gets the configured item metadata field definitions
    /// </summary>
    /// <returns>Array of item metadata field definitions</returns>
    /// <response code="200">Returns the item metadata field definitions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("item-metadata-definitions")]
    [ProducesResponseType(typeof(MetadataDefinition[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<MetadataDefinition[]> GetItemMetadataDefinitions() {
        return Ok(settings.Item.MetadataDefinition);
    }

}