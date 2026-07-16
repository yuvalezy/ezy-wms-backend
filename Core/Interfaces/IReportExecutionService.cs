using Core.DTOs.Reports;
using Core.Entities;

namespace Core.Interfaces;

/// <summary>
/// Executes report SQL against SAP's database. The only DB-touching piece of the reporting engine — the
/// lexer, composer and parameter binder around it are pure.
/// </summary>
/// <remarks>
/// <para>
/// Every method takes a <see cref="ReportDefinition"/> <b>entity</b> rather than an id, deliberately: the
/// engine never sees <c>SystemDbContext</c> (loading is <see cref="IReportService"/>'s job), and the Test
/// button can hand it a transient, unsaved definition.
/// </para>
/// <para>
/// Every path — page, count, lookup, validate, discover — runs inside an always-rollback scope on the
/// reporting connection with the definition's <c>CommandTimeout</c> applied, and threads the caller's
/// <c>CancellationToken</c> into <c>ExecuteReaderAsync</c> so a client disconnect cancels server-side.
/// </para>
/// </remarks>
public interface IReportExecutionService {
    /// <summary>
    /// Runs the Page shape and returns one page of rows.
    /// </summary>
    /// <remarks>
    /// Resolves and whitelists the sort key, clamps offset/limit, fetches <c>limit + 1</c> rows and trims the
    /// probe row into <see cref="ReportPageResult.HasMore"/>.
    /// </remarks>
    /// <exception cref="Core.Exceptions.ReportValidationException">
    /// Unknown or non-sortable sort key, negative/over-capped offset, or a variable value that failed strict parsing.
    /// </exception>
    Task<ReportPageResult> ExecutePageAsync(
        ReportDefinition definition,
        ReportExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the Count shape (or the author's <c>CountSql</c>) and returns the exact total. Lazy and separate
    /// so a slow count never blocks the rows; only meaningful when the definition's
    /// <c>TotalCountMode</c> is <c>Exact</c>. Ignores the request's paging, honours its filters.
    /// </summary>
    Task<long> CountAsync(
        ReportDefinition definition,
        ReportExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the <c>LookupSql</c> of the named <c>SqlLookup</c> variable to populate its dropdown.
    /// </summary>
    /// <remarks>
    /// All of the report's declared variables are bound, not just the referenced ones (free under
    /// <c>sp_executesql</c>), so <paramref name="request"/> carrying the currently-selected filter values is what
    /// makes cascading lookups work. Rows are capped at 1000 in the read loop.
    /// </remarks>
    /// <param name="variableName">
    /// <see cref="ReportVariable.Name"/>; matched case-insensitively.
    /// </param>
    Task<IReadOnlyList<ReportLookupOption>> ExecuteLookupAsync(
        ReportDefinition definition,
        string variableName,
        ReportExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a definition and discovers its output columns — what the Test button calls and what every
    /// save runs first. Never throws for authoring mistakes; they come back in
    /// <see cref="ReportValidationResult.Errors"/>.
    /// </summary>
    /// <remarks>
    /// Compiles <b>both</b> the page and count shapes plus every lookup query, then runs the Discover shape and
    /// reads <c>GetColumnSchema()</c> without ever reading a row.
    /// </remarks>
    Task<ReportValidationResult> ValidateAndDiscoverAsync(
        ReportDefinition definition,
        CancellationToken cancellationToken = default);
}
