using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs.Package;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Service.Middlewares;

namespace Service.Controllers;

/// <summary>
/// Package Controller - Manages package operations including creation, content management, location tracking, and validation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PackageController(
    IPackageService packageService,
    IPackageContentService contentService,
    IPackageValidationService validationService,
    IPackageLocationService locationService,
    IInventoryCountingsLineService countingService,
    IExternalSystemAdapter adapter,
    IExternalCommandService externalCommandService,
    ILogger<PackageController> logger,
    ISettings settings)
: ControllerBase
{
    /// <summary>
    /// Creates a new package
    /// </summary>
    /// <param name="request">The package creation request containing package details</param>
    /// <returns>The created package with its generated barcode and details</returns>
    /// <response code="200">Returns the created package</response>
    /// <response code="400">If the request is invalid or creation failed</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PackageDto>> CreatePackage([FromBody] CreatePackageRequest request)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();

            // Determine required role based on operation type
            var requiredRole = GetRequiredRoleForOperation(request.SourceOperationType ?? ObjectType.Package);

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole))
            {
                return Forbid();
            }

            var package = await packageService.CreatePackageAsync(sessionInfo, request);

            await externalCommandService.ExecuteCommandsAsync(CommandTriggerType.CreatePackage, ObjectType.Package, package.Id);

            return Ok(await package.ToDto(adapter, settings));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating package");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific package by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the package</param>
    /// <returns>The package details</returns>
    /// <response code="200">Returns the package details</response>
    /// <response code="404">If the package is not found or not accessible</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDto>> GetPackage(Guid id)
    {
        var sessionInfo = HttpContext.GetSession();
        var package = await packageService.GetPackageAsync(id);
        if (package == null || sessionInfo.Warehouse != package.WhsCode)
            return NotFound();

        return Ok(await package.ToDto(adapter, settings));
    }

    /// <summary>
    /// Gets a package by its barcode with optional additional data
    /// </summary>
    /// <param name="parameters">Object containing barcode and additional query parameters:
    /// - Barcode: The barcode of the package to retrieve
    /// - Contents: Include package contents in the response
    /// - History: Include transaction history in the response  
    /// - Details: Include detailed package information in the response
    /// - ObjectId: Optional reference object ID
    /// - ObjectType: Optional reference object type</param>
    /// <returns>The package details with optional additional data</returns>
    /// <response code="200">Returns the package details with requested optional data</response>
    /// <response code="404">If the package is not found or not accessible by the current user</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("barcode")]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDto>> GetPackageByBarcode([FromBody] PackageByBarcodeRequest parameters)
    {
        var package = await packageService.GetPackageByBarcodeAsync(parameters);
        var sessionInfo = HttpContext.GetSession();
        if (package == null || sessionInfo.Warehouse != package.WhsCode)
            return NotFound();

        if (parameters.ObjectType == ObjectType.InventoryCounting)
        {
            bool isValid = await countingService.ValidateScanPackage(package.Id, parameters.ObjectId!.Value, parameters.BinEntry);
            if (!isValid)
            {
                return BadRequest(new { error = "Package is already counted in another bin location" });
            }
        }

        return Ok(await package.ToDto(adapter, settings));
    }

    /// <summary>
    /// Gets all active packages in the user's warehouse
    /// </summary>
    /// <returns>A list of active packages</returns>
    /// <response code="200">Returns the list of active packages</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PackageDto>>> GetActivePackages()
    {
        var sessionInfo = HttpContext.GetSession();
        var packages = await packageService.GetActivePackagesAsync(sessionInfo.Warehouse);
        return Ok(packages.Select(async p => await p.ToDto(adapter, settings)));
    }

    /// <summary>
    /// Closes a package, finalizing its contents and triggering label printing if configured
    /// </summary>
    /// <param name="id">The unique identifier of the package to close</param>
    /// <returns>The closed package details</returns>
    /// <response code="200">Returns the closed package</response>
    /// <response code="400">If the package cannot be closed or request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{id:guid}/close")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PackageDto>> ClosePackage(Guid id)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(RoleType.PackageManagement) && !sessionInfo.Roles.Contains(RoleType.PackageManagementSupervisor))
            {
                return Forbid();
            }

            var package = await packageService.ClosePackageAsync(id, sessionInfo);

            if (settings.Package.Label.AutoPrint)
            {
                await TriggerLabelPrintingAsync(package);
            }

            await externalCommandService.ExecuteCommandsAsync(CommandTriggerType.ClosePackage, ObjectType.Package, package.Id);

            return Ok(await package.ToDto(adapter, settings));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error closing package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancels a package with a specified reason
    /// </summary>
    /// <param name="id">The unique identifier of the package to cancel</param>
    /// <param name="request">The cancellation request containing the reason</param>
    /// <returns>The cancelled package details</returns>
    /// <response code="200">Returns the cancelled package</response>
    /// <response code="400">If the package cannot be cancelled or request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{id}/cancel")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PackageDto>> CancelPackage(Guid id, [FromBody] CancelPackageRequest request)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();
            var package = await packageService.CancelPackageAsync(id, sessionInfo, request.Reason);
            return Ok(await package.ToDto(adapter, settings));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Locks a package to prevent modifications with a specified reason
    /// </summary>
    /// <param name="id">The unique identifier of the package to lock</param>
    /// <param name="request">The lock request containing the reason</param>
    /// <returns>The locked package details</returns>
    /// <response code="200">Returns the locked package</response>
    /// <response code="400">If the package cannot be locked or request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{id}/lock")]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PackageDto>> LockPackage(Guid id, [FromBody] LockPackageRequest request)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();
            var package = await packageService.LockPackageAsync(id, sessionInfo, request.Reason);
            return Ok(await package.ToDto(adapter, settings));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error locking package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Unlocks a package to allow modifications (supervisor only)
    /// </summary>
    /// <param name="id">The unique identifier of the package to unlock</param>
    /// <returns>The unlocked package details</returns>
    /// <response code="200">Returns the unlocked package</response>
    /// <response code="400">If the package cannot be unlocked or request is invalid</response>
    /// <response code="403">If the user lacks required supervisor permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{id}/unlock")]
    [RequireAnyRole(RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PackageDto>> UnlockPackage(Guid id)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();
            var package = await packageService.UnlockPackageAsync(id, sessionInfo);
            return Ok(await package.ToDto(adapter, settings));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unlocking package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Adds an item to a package
    /// </summary>
    /// <param name="id">The unique identifier of the package</param>
    /// <param name="request">The request containing item details to add</param>
    /// <returns>The package content details for the added item</returns>
    /// <response code="200">Returns the added package content</response>
    /// <response code="400">If the item cannot be added or request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{id}/contents")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(typeof(PackageContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PackageContentDto>> AddItemToPackage(Guid id, [FromBody] AddItemToPackageRequest request)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();

            // Determine required role based on operation type
            var requiredRole = GetRequiredRoleForOperation(request.SourceOperationType ?? ObjectType.Package);

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole))
            {
                return Forbid();
            }

            request.PackageId = id;
            var content = await contentService.AddItemToPackageAsync(request, sessionInfo.Warehouse, sessionInfo.Guid);
            return Ok(await content.ToDto(adapter));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding item to package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Removes an item from a package
    /// </summary>
    /// <param name="id">The unique identifier of the package</param>
    /// <param name="request">The request containing item details to remove</param>
    /// <returns>The package content details for the removed item</returns>
    /// <response code="200">Returns the removed package content</response>
    /// <response code="400">If the item cannot be removed or request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("{id}/contents")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(typeof(PackageContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PackageContentDto>> RemoveItemFromPackage(Guid id, [FromBody] RemoveItemFromPackageRequest request)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();

            // Determine required role based on operation type
            var requiredRole = GetRequiredRoleForOperation(request.SourceOperationType ?? ObjectType.Package);

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole))
            {
                return Forbid();
            }

            request.PackageId = id;
            var content = await contentService.RemoveItemFromPackageAsync(request, sessionInfo.Guid);
            return Ok(await content.ToDto(adapter));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing item from package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all items contained in a package
    /// </summary>
    /// <param name="id">The unique identifier of the package</param>
    /// <returns>A list of all items in the package</returns>
    /// <response code="200">Returns the package contents</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id}/contents")]
    [ProducesResponseType(typeof(IEnumerable<PackageContentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PackageContentDto>>> GetPackageContents(Guid id)
    {
        var contents = await contentService.GetPackageContentsAsync(id);
        return Ok(contents.Select(async c => await c.ToDto(adapter)));
    }

    /// <summary>
    /// Gets the transaction history for a package
    /// </summary>
    /// <param name="id">The unique identifier of the package</param>
    /// <returns>A list of all transactions performed on the package</returns>
    /// <response code="200">Returns the package transaction history</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id}/transactions")]
    [ProducesResponseType(typeof(IEnumerable<PackageTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PackageTransactionDto>>> GetPackageTransactions(Guid id)
    {
        var transactions = await contentService.GetPackageTransactionHistoryAsync(id);
        return Ok(transactions.Select(t => t.ToDto()));
    }

    /// <summary>
    /// Gets the location movement history for a package
    /// </summary>
    /// <param name="id">The unique identifier of the package</param>
    /// <returns>A list of all location movements for the package</returns>
    /// <response code="200">Returns the package movement history</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id}/movements")]
    [ProducesResponseType(typeof(IEnumerable<PackageLocationHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PackageLocationHistoryDto>>> GetPackageMovements(Guid id)
    {
        var movements = await locationService.GetPackageLocationHistoryAsync(id);
        return Ok(movements.Select(async m => await m.ToDto(adapter)));
    }

    /// <summary>
    /// Validates package consistency across the system
    /// </summary>
    /// <param name="whsCode">Optional warehouse code to limit validation scope</param>
    /// <returns>A list of detected inconsistencies</returns>
    /// <response code="200">Returns the list of inconsistencies found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("validate-consistency")]
    [ProducesResponseType(typeof(IEnumerable<PackageInconsistencyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PackageInconsistencyDto>>> ValidateConsistency([FromQuery] string? whsCode = null)
    {
        var inconsistencies = await validationService.DetectInconsistenciesAsync(whsCode);
        return Ok(inconsistencies.Select(async i => await i.ToDto(adapter)));
    }

    /// <summary>
    /// Generates a new unique barcode for package creation
    /// </summary>
    /// <returns>A new unique barcode</returns>
    /// <response code="200">Returns the generated barcode</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("generate-barcode")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> GenerateBarcode()
    {
        string barcode = await validationService.GeneratePackageBarcodeAsync();
        return Ok(new { barcode });
    }

    /// <summary>
    /// Moves a package to a new location
    /// </summary>
    /// <param name="id">The unique identifier of the package to move</param>
    /// <param name="request">The move request containing destination location details</param>
    /// <returns>The updated package details with new location</returns>
    /// <response code="200">Returns the moved package</response>
    /// <response code="400">If the package cannot be moved or request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{id}/move")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PackageDto>> MovePackage(Guid id, [FromBody] MovePackageRequest request)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();

            // Determine required role based on operation type
            var requiredRole = GetRequiredRoleForOperation(request.SourceOperationType ?? ObjectType.Package);

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole))
            {
                return Forbid();
            }

            request.PackageId = id;
            request.UserId = sessionInfo.Guid;
            var package = await locationService.MovePackageAsync(request);
            return Ok(await package.ToDto(adapter, settings));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error moving package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validates the consistency and integrity of a specific package
    /// </summary>
    /// <param name="id">The unique identifier of the package to validate</param>
    /// <returns>The validation results for the package</returns>
    /// <response code="200">Returns the package validation results</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id}/validate")]
    [ProducesResponseType(typeof(PackageValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PackageValidationResult>> ValidatePackage(Guid id)
    {
        var result = await validationService.ValidatePackageConsistencyAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Updates metadata for a specific package
    /// </summary>
    /// <param name="id">The unique identifier of the package</param>
    /// <param name="request">The metadata update request containing field values</param>
    /// <returns>The updated package with new metadata</returns>
    /// <response code="200">Returns the updated package</response>
    /// <response code="400">If metadata validation fails or request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="404">If the package is not found or not accessible</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPut("{id:guid}/metadata")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDto>> UpdatePackageMetadata(
        Guid id,
        [FromBody] UpdatePackageMetadataRequest request)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();

            var package = await packageService.UpdatePackageMetadataAsync(id, request, sessionInfo);
            return Ok(await package.ToDto(adapter, settings));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not accessible"))
        {
            return NotFound(new { error = "Package not found" }); // Don't leak warehouse info
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating package metadata for {PackageId}", id);
            return BadRequest(new { error = "An error occurred updating package metadata" });
        }
    }

    private async Task TriggerLabelPrintingAsync(Core.Entities.Package package)
    {
        logger.LogError("Triggering label print for package {Barcode}", package.Barcode);
        // TODO: Implement label printing integration
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets available manual commands for packages
    /// </summary>
    /// <param name="screenName">The screen name where commands will be displayed</param>
    /// <returns>List of available manual commands</returns>
    /// <response code="200">Returns the list of manual commands</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("manual-commands")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<object>>> GetManualCommands([FromQuery] string screenName = "PackageList")
    {
        var commands = await externalCommandService.GetManualCommandsAsync(ObjectType.Package, screenName);

        var result = commands.Select(c => new
        {
            c.Id,
            c.Name,
            c.Description,
            ButtonText = c.UIConfiguration?.ButtonText ?? "Execute Command",
            RequireConfirmation = c.UIConfiguration?.RequireConfirmation ?? true,
            ConfirmationMessage = c.UIConfiguration?.ConfirmationMessage,
            MaxBatchSize = c.UIConfiguration?.MaxBatchSize,
            AllowBatchExecution = c.AllowBatchExecution
        });

        return Ok(result);
    }

    /// <summary>
    /// Executes a manual command for a specific package
    /// </summary>
    /// <param name="id">The unique identifier of the package</param>
    /// <param name="commandId">The ID of the command to execute</param>
    /// <returns>Command execution result</returns>
    /// <response code="200">Command executed successfully</response>
    /// <response code="400">If the command execution failed</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{id:guid}/execute-command/{commandId}")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ExecuteManualCommand(Guid id, string commandId)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(RoleType.PackageManagement) && !sessionInfo.Roles.Contains(RoleType.PackageManagementSupervisor))
            {
                return Forbid();
            }

            await externalCommandService.ExecuteCommandAsync(commandId, id);

            return Ok(new { message = "Command executed successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing manual command {CommandId} for package {PackageId}", commandId, id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Executes a batch manual command for multiple packages
    /// </summary>
    /// <param name="request">The batch command request</param>
    /// <returns>Command execution result</returns>
    /// <response code="200">Command executed successfully</response>
    /// <response code="400">If the command execution failed</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("execute-batch-command")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ExecuteBatchManualCommand([FromBody] ExecuteBatchCommandRequest request)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(RoleType.PackageManagement) && !sessionInfo.Roles.Contains(RoleType.PackageManagementSupervisor))
            {
                return Forbid();
            }

            await externalCommandService.ExecuteBatchCommandAsync(request.CommandId, request.PackageIds);

            return Ok(new { message = $"Batch command executed successfully for {request.PackageIds.Length} packages" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing batch manual command {CommandId} for {PackageCount} packages", request.CommandId, request.PackageIds?.Length ?? 0);
            return BadRequest(new { error = ex.Message });
        }
    }

    private RoleType GetRequiredRoleForOperation(ObjectType operationType)
    {
        return operationType switch
        {
            ObjectType.GoodsReceipt => RoleType.GoodsReceipt,
            ObjectType.Transfer => RoleType.Transfer,
            ObjectType.Picking => RoleType.Picking,
            ObjectType.InventoryCounting => RoleType.Counting,
            ObjectType.Package => RoleType.PackageManagement,
            _ => RoleType.PackageManagement
        };
    }
}

/// <summary>
/// Request model for executing batch commands
/// </summary>
public class ExecuteBatchCommandRequest
{
    /// <summary>
    /// The ID of the command to execute
    /// </summary>
    public required string CommandId { get; set; }

    /// <summary>
    /// Array of package IDs to process
    /// </summary>
    public required Guid[] PackageIds { get; set; }
}