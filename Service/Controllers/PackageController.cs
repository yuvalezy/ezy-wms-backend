using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs.Package;
using Core.Enums;
using Core.Extensions;
using Core.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Service.Middlewares;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PackageController(IPackageService packageService, ILogger<PackageController> logger, IConfiguration configuration)
    : ControllerBase {
    [HttpPost]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    public async Task<ActionResult<PackageDto>> CreatePackage([FromBody] CreatePackageRequest request) {
        try {
            var sessionInfo = HttpContext.GetSession();

            // Determine required role based on operation type
            var requiredRole = GetRequiredRoleForOperation(request.SourceOperationType ?? ObjectType.Package);

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole)) {
                return Forbid();
            }

            var package = await packageService.CreatePackageAsync(sessionInfo, request);
            return Ok(package.ToDto());
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error creating package");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PackageDto>> GetPackage(Guid id) {
        var sessionInfo = HttpContext.GetSession();
        var package     = await packageService.GetPackageAsync(id);
        if (package == null || sessionInfo.Warehouse != package.WhsCode)
            return NotFound();

        return Ok(package.ToDto());
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<PackageDto>> GetPackageByBarcode(string barcode) {
        var package     = await packageService.GetPackageByBarcodeAsync(barcode);
        var sessionInfo = HttpContext.GetSession();
        if (package == null || sessionInfo.Warehouse != package.WhsCode)
            return NotFound();

        return Ok(package.ToDto());
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PackageDto>>> GetActivePackages() {
        var sessionInfo = HttpContext.GetSession();
        var packages    = await packageService.GetActivePackagesAsync(sessionInfo.Warehouse);
        return Ok(packages.Select(p => p.ToDto()));
    }

    [HttpPost("{id:guid}/close")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    public async Task<ActionResult<PackageDto>> ClosePackage(Guid id) {
        try {
            var sessionInfo = HttpContext.GetSession();

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(RoleType.PackageManagement) && !sessionInfo.Roles.Contains(RoleType.PackageManagementSupervisor)) {
                return Forbid();
            }

            var package = await packageService.ClosePackageAsync(id, sessionInfo);

            if (IsLabelPrintingEnabled()) {
                await TriggerLabelPrintingAsync(package);
            }

            return Ok(package.ToDto());
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error closing package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    public async Task<ActionResult<PackageDto>> CancelPackage(Guid id, [FromBody] CancelPackageRequest request) {
        try {
            var sessionInfo = HttpContext.GetSession();
            var package     = await packageService.CancelPackageAsync(id, sessionInfo, request.Reason);
            return Ok(package.ToDto());
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error cancelling package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/lock")]
    public async Task<ActionResult<PackageDto>> LockPackage(Guid id, [FromBody] LockPackageRequest request) {
        try {
            var sessionInfo = HttpContext.GetSession();
            var package     = await packageService.LockPackageAsync(id, sessionInfo, request.Reason);
            return Ok(package.ToDto());
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error locking package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/unlock")]
    [RequireAnyRole(RoleType.PackageManagementSupervisor)]
    public async Task<ActionResult<PackageDto>> UnlockPackage(Guid id) {
        try {
            var sessionInfo = HttpContext.GetSession();
            var package     = await packageService.UnlockPackageAsync(id, sessionInfo);
            return Ok(package.ToDto());
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error unlocking package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/contents")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    public async Task<ActionResult<PackageContentDto>> AddItemToPackage(Guid id, [FromBody] AddItemToPackageRequest request) {
        try {
            var sessionInfo = HttpContext.GetSession();

            // Determine required role based on operation type
            var requiredRole = GetRequiredRoleForOperation(request.SourceOperationType ?? ObjectType.Package);

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole)) {
                return Forbid();
            }

            request.PackageId = id;
            var content = await packageService.AddItemToPackageAsync(request, sessionInfo);
            return Ok(content.ToDto());
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error adding item to package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}/contents")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    public async Task<ActionResult<PackageContentDto>> RemoveItemFromPackage(Guid id, [FromBody] RemoveItemFromPackageRequest request) {
        try {
            var sessionInfo = HttpContext.GetSession();

            // Determine required role based on operation type
            var requiredRole = GetRequiredRoleForOperation(request.SourceOperationType ?? ObjectType.Package);

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole)) {
                return Forbid();
            }

            request.PackageId = id;
            var content = await packageService.RemoveItemFromPackageAsync(request, sessionInfo);
            return Ok(content.ToDto());
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error removing item from package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/contents")]
    public async Task<ActionResult<IEnumerable<PackageContentDto>>> GetPackageContents(Guid id) {
        var sessionInfo = HttpContext.GetSession();
        var contents    = await packageService.GetPackageContentsAsync(id);
        return Ok(contents.Select(c => c.ToDto()));
    }

    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<IEnumerable<PackageTransactionDto>>> GetPackageTransactions(Guid id) {
        var transactions = await packageService.GetPackageTransactionHistoryAsync(id);
        return Ok(transactions.Select(t => t.ToDto()));
    }

    [HttpGet("{id}/movements")]
    public async Task<ActionResult<IEnumerable<PackageLocationHistoryDto>>> GetPackageMovements(Guid id) {
        var movements = await packageService.GetPackageLocationHistoryAsync(id);
        return Ok(movements.Select(m => m.ToDto()));
    }

    [HttpPost("validate-consistency")]
    public async Task<ActionResult<IEnumerable<PackageInconsistencyDto>>> ValidateConsistency([FromQuery] string? whsCode = null) {
        var inconsistencies = await packageService.DetectInconsistenciesAsync(whsCode);
        return Ok(inconsistencies.Select(i => i.ToDto()));
    }

    [HttpPost("generate-barcode")]
    public async Task<ActionResult<object>> GenerateBarcode() {
        string barcode = await packageService.GeneratePackageBarcodeAsync();
        return Ok(new { barcode });
    }

    [HttpPost("{id}/move")]
    [RequireAnyRole(RoleType.PackageManagement, RoleType.PackageManagementSupervisor)]
    public async Task<ActionResult<PackageDto>> MovePackage(Guid id, [FromBody] MovePackageRequest request) {
        try {
            var sessionInfo = HttpContext.GetSession();

            // Determine required role based on operation type
            var requiredRole = GetRequiredRoleForOperation(request.SourceOperationType ?? ObjectType.Package);

            if (!sessionInfo.SuperUser && !sessionInfo.Roles.Contains(requiredRole)) {
                return Forbid();
            }

            request.PackageId = id;
            request.UserId    = sessionInfo.Guid;
            var package = await packageService.MovePackageAsync(request);
            return Ok(package.ToDto());
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error moving package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/validate")]
    public async Task<ActionResult<PackageValidationResult>> ValidatePackage(Guid id) {
        var result = await packageService.ValidatePackageConsistencyAsync(id);
        return Ok(result);
    }

    private bool IsLabelPrintingEnabled() {
        return configuration.GetValue("Package:Label:AutoPrint", false);
    }

    private async Task TriggerLabelPrintingAsync(Core.Entities.Package package) {
        logger.LogInformation("Triggering label print for package {Barcode}", package.Barcode);
        // TODO: Implement label printing integration
        await Task.CompletedTask;
    }

    private RoleType GetRequiredRoleForOperation(ObjectType operationType) {
        return operationType switch {
            ObjectType.GoodsReceipt      => RoleType.GoodsReceipt,
            ObjectType.Transfer          => RoleType.Transfer,
            ObjectType.Picking           => RoleType.Picking,
            ObjectType.InventoryCounting => RoleType.Counting,
            ObjectType.Package           => RoleType.PackageManagement,
            _                            => RoleType.PackageManagement
        };
    }
}