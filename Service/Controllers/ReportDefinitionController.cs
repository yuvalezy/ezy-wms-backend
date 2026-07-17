using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Core.DTOs.Reports;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers;

/// <summary>
/// Report Definition Controller - authoring of SQL-backed reports (super user only).
/// </summary>
/// <remarks>
/// The report SQL authored here is operator-trusted, matching the existing precedent for
/// <c>Filters.Vendors</c> / <c>PickPackOnly.Query</c> / <c>CustomField.Query</c> — hence
/// <c>[RequireSuperUser]</c> at class level. Variable <i>values</i>, which come from ordinary users, are never
/// trusted and never reach the SQL text; see <c>ReportParameterBinder</c>.
/// <para>
/// This is a business endpoint, so <c>SystemLockdownMiddleware</c> 503s it until SAP is configured. That is
/// correct: a SAP-backed report cannot be authored without SAP to author it against.
/// </para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireSuperUser]
public class ReportDefinitionController(IReportService reportService) : ControllerBase {
    private Guid? GetCurrentUserId() {
        string? userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets every report definition, enabled or not (super user only)
    /// </summary>
    /// <returns>A list of all report definitions</returns>
    /// <response code="200">Returns the list of report definitions</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReportDefinitionSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ReportDefinitionSummary>>> GetAll() {
        var definitions = await reportService.GetAllAsync();
        return Ok(definitions);
    }

    /// <summary>
    /// Gets a report definition by its ID, including its SQL (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the report definition</param>
    /// <returns>The full report definition</returns>
    /// <response code="200">Returns the report definition</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="404">If the report definition is not found</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportDefinitionDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReportDefinitionDetail>> GetById(Guid id) {
        var definition = await reportService.GetByIdAsync(id);
        if (definition == null) {
            return NotFound($"Report definition with ID {id} not found.");
        }
        return Ok(definition);
    }

    /// <summary>
    /// Validates a report definition and discovers its output columns without saving it — the Test button (super user only)
    /// </summary>
    /// <remarks>
    /// Compiles both the page and the count shape plus every lookup query inside an always-rollback transaction
    /// against SAP, then reads the output schema. Authoring mistakes come back in the result's Errors rather
    /// than as a 400, so the editor can show SQL Server's own message next to the SQL.
    /// </remarks>
    /// <param name="request">The report definition to validate</param>
    /// <param name="cancellationToken">Cancels the SQL server-side if the client disconnects</param>
    /// <returns>The validation result and the discovered columns</returns>
    /// <response code="200">Returns the validation result, valid or not</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("test")]
    [ProducesResponseType(typeof(ReportValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReportValidationResult>> Test(
        [FromBody] CreateReportDefinitionRequest request,
        CancellationToken cancellationToken) {
        var result = await reportService.TestAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a report definition (super user only)
    /// </summary>
    /// <param name="request">The report definition to create</param>
    /// <param name="cancellationToken">Cancels the validation SQL server-side if the client disconnects</param>
    /// <returns>The created report definition</returns>
    /// <response code="201">Returns the created report definition</response>
    /// <response code="400">If the definition is invalid or its SQL was rejected</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost]
    [ProducesResponseType(typeof(ReportDefinitionDetail), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReportDefinitionDetail>> Create(
        [FromBody] CreateReportDefinitionRequest request,
        CancellationToken cancellationToken) {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) {
            return Unauthorized("Unable to identify the current user.");
        }

        try {
            var definition = await reportService.CreateAsync(request, userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = definition.Id }, definition);
        }
        catch (ReportValidationException ex) {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    /// <summary>
    /// Updates a report definition, replacing its columns and variables wholesale (super user only)
    /// </summary>
    /// <remarks>Grants are untouched — they are managed from the authorization group screen.</remarks>
    /// <param name="request">The report definition to update</param>
    /// <param name="cancellationToken">Cancels the validation SQL server-side if the client disconnects</param>
    /// <returns>The updated report definition</returns>
    /// <response code="200">Returns the updated report definition</response>
    /// <response code="400">If the definition is invalid or its SQL was rejected</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="404">If the report definition is not found</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPut]
    [ProducesResponseType(typeof(ReportDefinitionDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReportDefinitionDetail>> Update(
        [FromBody] UpdateReportDefinitionRequest request,
        CancellationToken cancellationToken) {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) {
            return Unauthorized("Unable to identify the current user.");
        }

        try {
            var definition = await reportService.UpdateAsync(request, userId.Value, cancellationToken);
            return Ok(definition);
        }
        catch (KeyNotFoundException ex) {
            return NotFound(ex.Message);
        }
        catch (ReportValidationException ex) {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    /// <summary>
    /// Exports every report definition as a portable bundle (super user only)
    /// </summary>
    /// <remarks>
    /// Carries the authored fields only — ids, timestamps, authorship and authorization-group grants are
    /// install-specific and are left out. The client saves the JSON as a file; there is no file handling here.
    /// </remarks>
    /// <returns>A bundle of every report definition</returns>
    /// <response code="200">Returns the bundle</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    [HttpGet("export")]
    [ProducesResponseType(typeof(ReportExportBundle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ReportExportBundle>> ExportAll(CancellationToken cancellationToken) =>
        Ok(await reportService.ExportAsync(null, cancellationToken));

    /// <summary>
    /// Exports a single report definition as a portable bundle (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the report definition to export</param>
    /// <returns>A bundle containing just that report</returns>
    /// <response code="200">Returns the bundle</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="404">If the report definition is not found</response>
    [HttpGet("export/{id:guid}")]
    [ProducesResponseType(typeof(ReportExportBundle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportExportBundle>> ExportOne(Guid id, CancellationToken cancellationToken) {
        var bundle = await reportService.ExportAsync(id, cancellationToken);
        if (bundle.Reports.Count == 0) {
            return NotFound($"Report definition with ID {id} not found.");
        }
        return Ok(bundle);
    }

    /// <summary>
    /// Validates a bundle against this install and, unless <paramref name="dryRun"/>, applies it (super user only)
    /// </summary>
    /// <remarks>
    /// All-or-nothing: nothing is written unless every report is valid, so a bundle can never land
    /// half-imported. A report whose slug already exists is replaced in place, keeping its id — which is what
    /// makes its authorization-group grants survive a re-import.
    /// <para>
    /// A dry run compiles every report's SQL against <b>this</b> install's SAP, so a report needing a table or
    /// column the target does not have says so before anything is written. Call it first: it is what the
    /// preview shows.
    /// </para>
    /// <para>
    /// Returns 200 with per-report verdicts even when reports are invalid — the verdicts are the answer, not
    /// an error. Only an unusable file (wrong format version, no reports) populates the top-level errors.
    /// </para>
    /// </remarks>
    /// <param name="request">The bundle to import</param>
    /// <param name="dryRun">True to validate and preview without writing anything</param>
    /// <param name="cancellationToken">Cancels the validation SQL server-side if the client disconnects</param>
    /// <returns>The per-report verdicts</returns>
    /// <response code="200">Returns the import result, valid or not</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ReportImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReportImportResult>> Import(
        [FromBody] ReportImportRequest request,
        [FromQuery] bool dryRun,
        CancellationToken cancellationToken) {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) {
            return Unauthorized("Unable to identify the current user.");
        }

        return Ok(await reportService.ImportAsync(request, dryRun, userId.Value, cancellationToken));
    }

    /// <summary>
    /// Deletes a report definition and cascades its columns, variables and grants (super user only)
    /// </summary>
    /// <param name="id">The unique identifier of the report definition to delete</param>
    /// <returns>No content if deletion was successful</returns>
    /// <response code="204">Report definition deleted successfully</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks super user permissions</response>
    /// <response code="404">If the report definition is not found</response>
    /// <response code="500">If a server error occurs</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id) {
        var deleted = await reportService.DeleteAsync(id);
        if (!deleted) {
            return NotFound($"Report definition with ID {id} not found.");
        }
        return NoContent();
    }
}
