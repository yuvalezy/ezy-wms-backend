using Core.DTOs.Reports;
using Core.Entities;

namespace Core.Interfaces;

/// <summary>
/// Owns report definitions (WMS's own EF data, so all CRUD here is EF) and the per-report access checks.
/// The raw-SQL path is <see cref="IReportExecutionService"/>'s alone.
/// </summary>
/// <remarks>
/// <b>Access is always checked live against the join table</b>, never against the login-time
/// <c>SessionInfo.Roles</c> snapshot: a grant must take effect without re-login, and a newly created report
/// must not be invisible to everyone already logged in. Superusers implicitly have access to every report.
/// </remarks>
public interface IReportService {
    // ---- Superuser CRUD (Service/Controllers/ReportDefinitionController) ----

    /// <summary>
    /// Every definition, enabled or not. Superuser-only; also what the authorization-group grant card lists.
    /// </summary>
    Task<IReadOnlyList<ReportDefinitionSummary>> GetAllAsync();

    /// <summary>
    /// Full definition including SQL. Superuser-only. Null when not found.
    /// </summary>
    Task<ReportDefinitionDetail?> GetByIdAsync(Guid id);

    /// <summary>
    /// Validates (both shapes plus every lookup) and discovers columns without saving — the Test button.
    /// </summary>
    Task<ReportValidationResult> TestAsync(CreateReportDefinitionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates then saves.
    /// </summary>
    /// <exception cref="Core.Exceptions.ReportValidationException">
    /// Duplicate slug, bad/duplicate/reserved variable name, placeholder token problems, a default sort key
    /// that isn't a sortable declared column, or SQL that SQL Server rejected.
    /// </exception>
    Task<ReportDefinitionDetail> CreateAsync(CreateReportDefinitionRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates then saves, replacing columns and variables wholesale. Grants are untouched.
    /// </summary>
    /// <exception cref="Core.Exceptions.ReportValidationException">As <see cref="CreateAsync"/>.</exception>
    Task<ReportDefinitionDetail> UpdateAsync(UpdateReportDefinitionRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a definition and cascades its columns, variables and grants. False when not found.
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    // ---- End user, live access-checked (Service/Controllers/ReportController) ----

    /// <summary>
    /// The enabled reports this user may run — every enabled report for a superuser, otherwise those granted
    /// live to one of the user's authorization groups. <c>GET /api/report</c>.
    /// </summary>
    Task<IReadOnlyList<ReportDefinitionSummary>> GetAvailableAsync(Guid userId);

    /// <summary>
    /// Runner metadata for one report: columns, variables (with static options parsed and lookups flagged),
    /// defaults. <c>GET /api/report/{slug}</c>.
    /// <para>
    /// <b>SQL is stripped unless the caller is a superuser</b> — <c>Sql</c>, <c>CountSql</c> and each variable's
    /// <c>LookupSql</c> come back null.
    /// </para>
    /// Null when the report doesn't exist, is disabled, or the user isn't granted it — the controller must not
    /// distinguish those cases to the caller.
    /// </summary>
    Task<ReportDefinitionDetail?> GetBySlugAsync(string slug, Guid userId);

    /// <summary>
    /// Loads the full entity (columns and variables included) for the execution engine, applying the same live
    /// access check. Null when not found, disabled, or not granted → the controller returns 403.
    /// </summary>
    /// <remarks>
    /// This is the seam that keeps <see cref="IReportExecutionService"/> free of <c>SystemDbContext</c>: the
    /// controller loads here, then hands the entity to the engine.
    /// </remarks>
    Task<ReportDefinition?> GetForExecutionAsync(string slug, Guid userId);

    /// <summary>
    /// Appends an execution audit row. Written by the caller of the engine rather than the engine itself,
    /// which has no <c>SystemDbContext</c>. Must never fail the user's request.
    /// </summary>
    Task LogExecutionAsync(ReportExecutionAudit audit);
}
