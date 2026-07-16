using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.DTOs.Reports;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
// For ReportExecutionService.LookupRowCap. The cap itself belongs to the engine; the controller only reports
// that it bit, and reads the constant rather than duplicating a literal that would silently drift from it.
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service.Controllers;

/// <summary>
/// Report Controller - lets an end user list and run the SQL-backed reports they have been granted.
/// </summary>
/// <remarks>
/// <para>
/// <c>[Authorize]</c> only — per-report access is <b>not</b> a <c>RoleType</c> (reports are created at runtime,
/// so there is no enum value to mint per report) and is therefore checked inside every action via
/// <see cref="IReportService"/>, live against the join table. That is deliberate: <c>SessionInfo.Roles</c> is
/// snapshotted at login and never invalidated, so a role-based check here would mean a grant did nothing until
/// the user logged in again, and a newly created report would be invisible to everyone already signed in.
/// </para>
/// <para>
/// This is a business endpoint, so <c>SystemLockdownMiddleware</c> 503s it until SAP is configured — correct,
/// since the reports read SAP's database.
/// </para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportController(
    IReportService reportService,
    IReportExecutionService executionService,
    ILogger<ReportController> logger) : ControllerBase {
    /// <summary>Rows an export will fetch before it stops, per the spec's 50,000 cap.</summary>
    private const int ExportMaxRows = 50_000;

    /// <summary>
    /// Rows per export chunk. Each chunk is its own short rollback scope, so a 50k export never pins SAP's
    /// transaction log inside one long transaction the way streaming it would.
    /// </summary>
    private const int ExportChunkRows = 200;

    /// <summary>
    /// Set on a lookup response when the row cap trimmed the options, so the client can say "showing the first
    /// N — refine your filter" instead of silently presenting a truncated list as complete.
    /// </summary>
    private const string LookupTruncatedHeader = "X-Report-Lookup-Truncated";

    private Guid? GetCurrentUserId() {
        string? userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the enabled reports the current user is allowed to run
    /// </summary>
    /// <returns>The reports available to this user</returns>
    /// <response code="200">Returns the available reports</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReportDefinitionSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ReportDefinitionSummary>>> GetAvailable() {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) {
            return Unauthorized("Unable to identify the current user.");
        }

        var reports = await reportService.GetAvailableAsync(userId.Value);
        return Ok(reports);
    }

    /// <summary>
    /// Gets the runner metadata for one report: its columns, variables and defaults
    /// </summary>
    /// <remarks>The SQL is stripped unless the caller is a super user.</remarks>
    /// <param name="slug">The report's slug</param>
    /// <returns>The report metadata</returns>
    /// <response code="200">Returns the report metadata</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="404">If the report does not exist, is disabled, or is not granted to this user</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ReportDefinitionDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReportDefinitionDetail>> GetBySlug(string slug) {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) {
            return Unauthorized("Unable to identify the current user.");
        }

        var report = await reportService.GetBySlugAsync(slug, userId.Value);
        if (report == null) {
            // Not found / disabled / not granted are deliberately indistinguishable to the caller.
            return NotFound($"Report '{slug}' not found.");
        }
        return Ok(report);
    }

    /// <summary>
    /// Runs one page of a report
    /// </summary>
    /// <param name="slug">The report's slug</param>
    /// <param name="request">Filter values, sort choice and paging</param>
    /// <param name="cancellationToken">Cancels the SQL server-side if the client disconnects</param>
    /// <returns>A page of rows plus the column metadata</returns>
    /// <response code="200">Returns the page</response>
    /// <response code="400">If a filter value, the sort key or the paging was rejected</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the report is not granted to this user</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("{slug}/run")]
    [ProducesResponseType(typeof(ReportPageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReportPageResult>> Run(
        string slug,
        [FromBody] ReportExecutionRequest request,
        CancellationToken cancellationToken) {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) {
            return Unauthorized("Unable to identify the current user.");
        }

        var definition = await reportService.GetForExecutionAsync(slug, userId.Value);
        if (definition == null) {
            return Denied(slug);
        }

        try {
            var startedAt = DateTime.UtcNow;
            var result = await executionService.ExecutePageAsync(definition, request, cancellationToken);
            await AuditAsync(definition, userId.Value, request, result.SortColumnKey, result.SortDirection,
                result.Rows.Count, result.DurationMs, startedAt);
            return Ok(result);
        }
        catch (ReportValidationException ex) {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    /// <summary>
    /// Returns the exact total row count for a report, honouring its filters and ignoring its paging
    /// </summary>
    /// <remarks>
    /// Deliberately a separate, lazily-called endpoint: paging itself never needs it (the page fetches
    /// limit + 1 and reports HasMore), so a slow count must never block the rows.
    /// </remarks>
    /// <param name="slug">The report's slug</param>
    /// <param name="request">Filter values; paging is ignored</param>
    /// <param name="cancellationToken">Cancels the SQL server-side if the client disconnects</param>
    /// <returns>The total row count</returns>
    /// <response code="200">Returns the total</response>
    /// <response code="400">If the report does not offer an exact count, or a filter value was rejected</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the report is not granted to this user</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("{slug}/count")]
    [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<long>> Count(
        string slug,
        [FromBody] ReportExecutionRequest request,
        CancellationToken cancellationToken) {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) {
            return Unauthorized("Unable to identify the current user.");
        }

        var definition = await reportService.GetForExecutionAsync(slug, userId.Value);
        if (definition == null) {
            return Denied(slug);
        }

        if (definition.TotalCountMode != ReportTotalCountMode.Exact) {
            // The author opted out, so counting anyway would run a query they never asked anyone to pay for.
            return BadRequest($"Report '{slug}' does not offer an exact total count.");
        }

        try {
            var total = await executionService.CountAsync(definition, request, cancellationToken);
            return Ok(total);
        }
        catch (ReportValidationException ex) {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    /// <summary>
    /// Populates the dropdown of a SQL-lookup variable
    /// </summary>
    /// <remarks>
    /// The request's current filter values are bound to the lookup query, which is what makes cascading
    /// lookups (warehouse then bin) work. Options are capped; when the cap bites, the
    /// <c>X-Report-Lookup-Truncated</c> response header is set.
    /// </remarks>
    /// <param name="slug">The report's slug</param>
    /// <param name="variable">The variable's name</param>
    /// <param name="request">The currently-selected filter values</param>
    /// <param name="cancellationToken">Cancels the SQL server-side if the client disconnects</param>
    /// <returns>The dropdown options</returns>
    /// <response code="200">Returns the options</response>
    /// <response code="400">If the variable is unknown, is not a SQL lookup, or a filter value was rejected</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the report is not granted to this user</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("{slug}/lookup/{variable}")]
    [ProducesResponseType(typeof(IEnumerable<ReportLookupOption>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ReportLookupOption>>> Lookup(
        string slug,
        string variable,
        [FromBody] ReportExecutionRequest request,
        CancellationToken cancellationToken) {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) {
            return Unauthorized("Unable to identify the current user.");
        }

        var definition = await reportService.GetForExecutionAsync(slug, userId.Value);
        if (definition == null) {
            return Denied(slug);
        }

        try {
            var options = await executionService.ExecuteLookupAsync(definition, variable, request, cancellationToken);
            if (options.Count >= ReportExecutionService.LookupRowCap) {
                Response.Headers[LookupTruncatedHeader] = "true";
            }
            return Ok(options);
        }
        catch (ReportValidationException ex) {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    /// <summary>
    /// Fetches every row of a report, up to the export cap, for the client to write to Excel
    /// </summary>
    /// <remarks>
    /// Honours the request's filters and sort and ignores its paging. Rows are fetched <b>chunked</b>, one short
    /// rollback scope per chunk — never one long transaction, which would pin SAP's log for the whole export.
    /// </remarks>
    /// <param name="slug">The report's slug</param>
    /// <param name="request">Filter values and sort choice; paging is ignored</param>
    /// <param name="cancellationToken">Cancels the SQL server-side if the client disconnects</param>
    /// <returns>Every row, capped, with HasMore set when the cap truncated the export</returns>
    /// <response code="200">Returns the rows</response>
    /// <response code="400">If a filter value or the sort key was rejected</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the report is not granted to this user</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("{slug}/export")]
    [ProducesResponseType(typeof(ReportPageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReportPageResult>> Export(
        string slug,
        [FromBody] ReportExecutionRequest request,
        CancellationToken cancellationToken) {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) {
            return Unauthorized("Unable to identify the current user.");
        }

        var definition = await reportService.GetForExecutionAsync(slug, userId.Value);
        if (definition == null) {
            return Denied(slug);
        }

        try {
            var result = await ExportChunkedAsync(definition, request, cancellationToken);
            await AuditAsync(definition, userId.Value, request, result.SortColumnKey, result.SortDirection,
                result.Rows.Count, result.DurationMs, DateTime.UtcNow.AddMilliseconds(-result.DurationMs));
            return Ok(result);
        }
        catch (ReportValidationException ex) {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    /// <summary>
    /// Walks the report a chunk at a time. Each <c>ExecutePageAsync</c> opens and closes its own scope, so the
    /// export is many short transactions rather than one long one.
    /// </summary>
    private async Task<ReportPageResult> ExportChunkedAsync(
        ReportDefinition definition,
        ReportExecutionRequest request,
        CancellationToken cancellationToken) {
        var rows = new List<Dictionary<string, object?>>();
        var stopwatch = Stopwatch.StartNew();
        ReportPageResult? last = null;
        bool hasMore = true;

        while (hasMore && rows.Count < ExportMaxRows) {
            var chunk = new ReportExecutionRequest {
                Variables = request.Variables,
                SortColumnKey = request.SortColumnKey,
                SortDirection = request.SortDirection,
                Offset = rows.Count,
                Limit = Math.Min(ExportChunkRows, ExportMaxRows - rows.Count)
            };

            last = await executionService.ExecutePageAsync(definition, chunk, cancellationToken);
            rows.AddRange(last.Rows);
            hasMore = last.HasMore;

            // Defensive: a report whose rows shift underneath the export could otherwise spin here forever.
            if (last.Rows.Count == 0) break;
        }

        stopwatch.Stop();

        if (rows.Count >= ExportMaxRows && hasMore) {
            logger.LogWarning("Export of report {Slug} hit the {Cap}-row cap and was truncated.",
                definition.Slug, ExportMaxRows);
        }

        return new ReportPageResult {
            Columns = last?.Columns ?? [],
            Rows = rows,
            // True only when the cap — not the data — ended the export, so the client can warn the user.
            HasMore = hasMore,
            Offset = 0,
            Limit = rows.Count,
            SortColumnKey = last?.SortColumnKey,
            SortDirection = last?.SortDirection ?? definition.DefaultSortDirection,
            DurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task AuditAsync(
        ReportDefinition definition,
        Guid userId,
        ReportExecutionRequest request,
        string? sortColumnKey,
        ReportSortDirection sortDirection,
        long rowCount,
        long durationMs,
        DateTime startedAtUtc) {
        await reportService.LogExecutionAsync(new ReportExecutionAudit {
            ReportDefinitionId = definition.Id,
            UserId = userId,
            SortColumnKey = sortColumnKey,
            SortDirection = sortDirection,
            ParametersJson = SerializeParameters(request),
            RowCount = rowCount,
            DurationMs = durationMs,
            StartedAtUtc = startedAtUtc
        });
    }

    private string? SerializeParameters(ReportExecutionRequest request) {
        if (request.Variables.Count == 0) return null;

        try {
            return JsonSerializer.Serialize(request.Variables);
        }
        catch (NotSupportedException ex) {
            // The audit row is worth more than its parameters column; never fail a user's report over it.
            logger.LogWarning(ex, "Could not serialize report parameters for the execution audit.");
            return null;
        }
    }

    /// <summary>
    /// 403 for "you may not run this", written as an explicit status rather than <c>Forbid()</c>: with the
    /// cookie/JWT hybrid scheme, <c>Forbid()</c> can hand back a challenge redirect instead of the plain 403
    /// the frontend's axios interceptor keys off.
    /// </summary>
    private ObjectResult Denied(string slug) =>
        StatusCode(StatusCodes.Status403Forbidden, $"You do not have access to report '{slug}'.");
}
