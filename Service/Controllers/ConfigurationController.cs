using System;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Adapters.Common.SBO.Repositories;
using Core.DTOs.Configuration;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service.Controllers;

/// <summary>
/// Superuser-only management of the database-backed configuration sections:
/// list, read (masked), validate, update, history, restore, export, import, and
/// migration status. Updates trigger a configuration reload.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfigurationController(
    IConfigurationManagementService service,
    IUserService userService,
    ISboConnectionTester sboTester,
    SboGeneralRepository sboGeneralRepository,
    ILogger<ConfigurationController> logger) : ControllerBase {

    [HttpGet]
    public async Task<IActionResult> List() {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        return Ok(await service.ListAsync());
    }

    [HttpGet("migration-status")]
    public async Task<IActionResult> MigrationStatus() {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        return Ok(await service.GetMigrationStatusAsync());
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAll() {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        return Ok(await service.ExportAsync(null));
    }

    [HttpGet("export/{section}")]
    public async Task<IActionResult> ExportSection(string section) {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        return Ok(await service.ExportAsync(section));
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ConfigImportRequest request, [FromQuery] bool dryRun = false) {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        return await Guarded(async () => Ok(await service.ImportAsync(request, dryRun, GetUserId())));
    }

    [HttpPost("import/{section}")]
    public async Task<IActionResult> ImportSection(
        string section, [FromBody] JsonNode? json, [FromQuery] bool dryRun = false) {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        var request = new ConfigImportRequest();
        request.Sections[section] = json;
        return await Guarded(async () => Ok(await service.ImportAsync(request, dryRun, GetUserId())));
    }

    [HttpGet("{section}")]
    public async Task<IActionResult> Get(string section) {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        var detail = await service.GetAsync(section);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("{section}/history")]
    public async Task<IActionResult> History(string section) {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        return Ok(await service.HistoryAsync(section));
    }

    [HttpPost("{section}/validate")]
    public async Task<IActionResult> Validate(string section, [FromBody] ConfigSectionUpdateRequest request) {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        return Ok(service.Validate(section, request.Json));
    }

    /// <summary>Tests a draft SBO connection without saving (masked secrets resolved from the stored values).</summary>
    [HttpPost("{section}/test-connection")]
    public async Task<IActionResult> TestConnection(string section, [FromBody] ConfigSectionUpdateRequest request) {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        if (!section.Equals("SboSettings", StringComparison.OrdinalIgnoreCase)) {
            return BadRequest(new {
                error = "unsupported",
                error_description = "Test connection is only available for SboSettings."
            });
        }
        return await Guarded(async () => {
            var sbo    = await service.ResolveSboSettingsDraftAsync(request.Json);
            var result = await sboTester.TestServiceLayerLoginAsync(sbo);
            return Ok(new { success = result.Success, message = result.Message });
        });
    }

    /// <summary>
    /// Validates the draft "Filters" SQL fragments against the external SAP database. Each present
    /// filter is spliced into the exact query shape it is used in at runtime, guarded so it returns
    /// no rows, and executed inside a rolled-back transaction — so SQL Server fully parses and binds
    /// it (catching syntax errors and unknown columns) without persisting anything.
    /// </summary>
    [HttpPost("Filters/validate-sql")]
    public async Task<IActionResult> ValidateFiltersSql([FromBody] ConfigSectionUpdateRequest request) {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        return await Guarded(async () => {
            // Filters has no secrets; read the draft fields directly so a partially-filled
            // PickPackOnly (Query without GroupBy, while editing) never throws on bind.
            string? vendors        = ReadFilterString(request.Json, "Vendors");
            var     pickPackNode   = FindProperty(request.Json, "PickPackOnly");
            string? pickPackQuery  = ReadFilterString(pickPackNode, "Query");
            string? pickPackGroup  = ReadFilterString(pickPackNode, "GroupBy");

            var response = new FiltersValidationResponse();

            if (!string.IsNullOrWhiteSpace(vendors)) {
                var (valid, message) = await sboGeneralRepository.ValidateVendorsFilterAsync(vendors);
                response.Vendors = new FilterValidationResult { Valid = valid, Message = message };
            }

            if (!string.IsNullOrWhiteSpace(pickPackQuery)) {
                if (string.IsNullOrWhiteSpace(pickPackGroup)) {
                    response.PickPackOnly = new FilterValidationResult {
                        Valid   = false,
                        Message = "PickPackOnly GroupBy is required when a Query is set."
                    };
                }
                else {
                    var (valid, message) = await sboGeneralRepository.ValidatePickPackOnlyFilterAsync(
                        new FilterQuery { Query = pickPackQuery, GroupBy = pickPackGroup });
                    response.PickPackOnly = new FilterValidationResult { Valid = valid, Message = message };
                }
            }

            return Ok(response);
        });
    }

    /// <summary>Case-insensitive lookup of a JSON object property (PascalCase or camelCase).</summary>
    private static JsonNode? FindProperty(JsonNode? node, string name) {
        if (node is not JsonObject obj) {
            return null;
        }
        foreach (var pair in obj) {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)) {
                return pair.Value;
            }
        }
        return null;
    }

    private static string? ReadFilterString(JsonNode? node, string name) =>
        FindProperty(node, name)?.GetValue<string?>();

    [HttpPut("{section}")]
    public async Task<IActionResult> Update(string section, [FromBody] ConfigSectionUpdateRequest request) {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        return await Guarded(async () =>
            Ok(await service.UpdateAsync(section, request.Json, request.ExpectedVersion, ConfigChangeType.Edit, GetUserId())));
    }

    [HttpPost("{section}/restore/{version:int}")]
    public async Task<IActionResult> Restore(string section, int version) {
        if (await Forbidden() is { } forbid) {
            return forbid;
        }
        return await Guarded(async () => Ok(await service.RestoreAsync(section, version, GetUserId())));
    }

    // --- helpers ---

    private async Task<IActionResult> Guarded(Func<Task<IActionResult>> action) {
        try {
            return await action();
        }
        catch (ConfigOperationException ex) {
            return StatusCode(ex.StatusCode, new { error = "configuration_error", error_description = ex.Message, errors = ex.Errors });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Configuration operation failed");
            return StatusCode(500, new { error = "server_error", error_description = ex.Message });
        }
    }

    private async Task<IActionResult?> Forbidden() {
        if (await IsUserSuperUser()) {
            return null;
        }
        return StatusCode(403, new {
            error = "forbidden",
            error_description = "Solo los super usuarios pueden administrar la configuración."
        });
    }

    private Guid? GetUserId() {
        string? claim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private async Task<bool> IsUserSuperUser() {
        if (GetUserId() is not { } userId) {
            return false;
        }
        var user = await userService.GetUserAsync(userId);
        return user?.SuperUser ?? false;
    }
}
