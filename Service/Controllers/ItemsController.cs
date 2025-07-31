using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs.Items;
using Core.DTOs.Settings;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Middlewares;

namespace Service.Controllers;

/// <summary>
/// Item Controller - Provides item/barcode/bin location utilities and metadata management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemsController(
    IPublicService publicService,
    IItemService itemService) : ControllerBase {
    /// <summary>
    /// Scans and validates a bin location barcode
    /// </summary>
    /// <param name="bin">The bin location barcode or identifier to scan</param>
    /// <returns>Bin location information if found</returns>
    /// <response code="200">Returns the bin location details</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks required warehouse operation permissions</response>
    /// <response code="404">If the bin location is not found</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("ScanBinLocation")]
    [RequireAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptConfirmation, RoleType.Counting)]
    [ProducesResponseType(typeof(BinLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BinLocationResponse?>> ScanBinLocation([FromQuery] string bin) {
        var response = await publicService.ScanBinLocationAsync(bin);

        return response != null ? Ok(response) : NotFound();
    }

    /// <summary>
    /// Scans and retrieves item information by barcode
    /// </summary>
    /// <param name="scanCode">The item barcode or identifier to scan</param>
    /// <param name="item">Whether to search by item code instead of barcode</param>
    /// <returns>Item information matching the scanned code</returns>
    /// <response code="200">Returns the item information</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks required warehouse operation permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("ItemByBarCode")]
    [RequireAnyRole(RoleType.GoodsReceipt, RoleType.GoodsReceiptConfirmation, RoleType.TransferRequest)]
    [ProducesResponseType(typeof(IEnumerable<ItemInfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ItemInfoResponse>>> ScanItemBarCode([FromQuery] string scanCode, [FromQuery] bool item = false) {
        return Ok(await publicService.ScanItemBarCodeAsync(scanCode, item));
    }

    /// <summary>
    /// Validates item and barcode combinations (supervisor roles only)
    /// </summary>
    /// <param name="request">The request containing item code and barcode to validate</param>
    /// <returns>Item validation results</returns>
    /// <response code="200">Returns the item validation results</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("ItemCheck")]
    [RequireAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor, RoleType.PickingSupervisor,
        RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<ItemCheckResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ItemCheckResponse>>> ItemCheck([FromBody] ItemBarCodeRequest request) {
        return Ok(await publicService.ItemCheckAsync(request.ItemCode, request.Barcode));
    }

    /// <summary>
    /// Checks bin contents and inventory (supervisor roles only)
    /// </summary>
    /// <param name="binEntry">The bin entry ID to check contents for</param>
    /// <returns>Bin content information and inventory details</returns>
    /// <response code="200">Returns the bin content information</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("BinCheck")]
    [RequireAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor, RoleType.PickingSupervisor,
        RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<BinContentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BinContentResponse>>> BinCheck([FromQuery] int binEntry) {
        return Ok(await publicService.BinCheckAsync(binEntry));
    }

    /// <summary>
    /// Gets item stock information across bins in the current warehouse
    /// </summary>
    /// <param name="request">The request containing item code to check stock for</param>
    /// <returns>Item stock information across all bins</returns>
    /// <response code="200">Returns the item stock information</response>
    /// <response code="400">If the item code is missing or invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("ItemStock")]
    [RequireAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor,
        RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor, RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<ItemStockResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ItemStockResponse>>> ItemStock([FromBody] ItemBarCodeRequest request) {
        if (string.IsNullOrWhiteSpace(request.ItemCode)) {
            return BadRequest("Item code is required.");
        }

        string warehouse = HttpContext.GetSession().Warehouse;
        return Ok(await publicService.ItemStockAsync(request.ItemCode, warehouse));
    }
    
    /// <summary>
    /// Gets item stock information across bins in the current warehouse
    /// </summary>
    /// <param name="request">The request containing item code to check stock for</param>
    /// <returns>Item stock information across all bins</returns>
    /// <response code="200">Returns the item stock information</response>
    /// <response code="400">If the item code is missing or invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("ItemBinStock")]
    [RequireAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor,
        RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor, RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(typeof(IEnumerable<ItemBinStockResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ItemBinStockResponse>>> ItemBinStock([FromBody] ItemBarCodeRequest request) {
        if (string.IsNullOrWhiteSpace(request.ItemCode)) {
            return BadRequest("Item code is required.");
        }

        string warehouse = HttpContext.GetSession().Warehouse;
        return Ok(await publicService.ItemBinStockAsync(request.ItemCode, warehouse));
    }

    /// <summary>
    /// Updates or creates item barcode associations (supervisor roles only)
    /// </summary>
    /// <param name="request">The request containing item and barcode information to update</param>
    /// <returns>Response indicating success or failure of the barcode update</returns>
    /// <response code="200">Returns the barcode update response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("UpdateItemBarCode")]
    [RequireAnyRole(RoleType.GoodsReceiptSupervisor, RoleType.CountingSupervisor, RoleType.TransferSupervisor, RoleType.GoodsReceiptConfirmationSupervisor)]
    [ProducesResponseType(typeof(UpdateItemBarCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<UpdateItemBarCodeResponse> UpdateItemBarCode([FromBody] UpdateBarCodeRequest request) {
        return await publicService.UpdateItemBarCode(HttpContext.GetSession().UserId, request);
    }

    /// <summary>
    /// Updates metadata for a specific item
    /// </summary>
    /// <param name="itemCode">The item code to update metadata for</param>
    /// <param name="request">The metadata update request containing field values</param>
    /// <returns>The updated item metadata</returns>
    /// <response code="200">Returns the updated item metadata</response>
    /// <response code="400">If metadata validation fails or request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="404">If the item is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPut("{itemCode}/metadata")]
    [RequireAnyRole(RoleType.ItemManagement, RoleType.ItemManagementSupervisor)]
    [ProducesResponseType(typeof(ItemMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemMetadataResponse>> UpdateItemMetadata(
        string itemCode,
        [FromBody] UpdateItemMetadataRequest request) {
        
        var sessionInfo = HttpContext.GetSession();
        
        var updatedItem = await itemService.UpdateItemMetadataAsync(
            itemCode, request, sessionInfo);
            
        return Ok(updatedItem);
    }
    
    /// <summary>
    /// Retrieves metadata for a specific item
    /// </summary>
    /// <param name="itemCode">The item code to retrieve metadata for</param>
    /// <returns>The item metadata</returns>
    /// <response code="200">Returns the item metadata</response>
    /// <response code="404">If the item is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{itemCode}/metadata")]
    [ProducesResponseType(typeof(ItemMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemMetadataResponse>> GetItemMetadata(string itemCode) {
        var itemMetadata = await itemService.GetItemMetadataAsync(itemCode);
        
        if (itemMetadata == null) {
            throw new System.Collections.Generic.KeyNotFoundException($"Item '{itemCode}' not found");
        }
        
        return Ok(itemMetadata);
    }
}