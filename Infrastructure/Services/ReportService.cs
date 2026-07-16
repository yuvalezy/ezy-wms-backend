using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.DTOs.Reports;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// See <see cref="IReportService"/>. This class never composes or executes report SQL — it only hands the
/// engine a definition entity.
/// </summary>
public class ReportService(
    SystemDbContext db,
    IReportExecutionService executionService,
    ILogger<ReportService> logger) : IReportService {
    /// <summary>Variable names, per the spec. 60 chars, not 63 — DateRange needs room for a From/To suffix.</summary>
    private static readonly Regex VariableNamePattern =
        new(@"^[A-Za-z_][A-Za-z0-9_]{0,59}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Slugs land in a route segment, so no slashes, spaces or anything needing escaping.</summary>
    private static readonly Regex SlugPattern =
        new(@"^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions StaticOptionsJson = new() {
        PropertyNamingPolicy       = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Expression<Func<ReportDefinition, ReportDefinitionSummary>> SummaryProjection =
        r => new ReportDefinitionSummary {
            Id            = r.Id,
            Name          = r.Name,
            Slug          = r.Slug,
            Description   = r.Description,
            Enabled       = r.Enabled,
            ColumnCount   = r.Columns.Count,
            VariableCount = r.Variables.Count,
            CreatedAt     = r.CreatedAt,
            UpdatedAt     = r.UpdatedAt
        };

    // ---------------------------------------------------------------- superuser CRUD

    public async Task<IReadOnlyList<ReportDefinitionSummary>> GetAllAsync() =>
        await db.ReportDefinitions
            .AsNoTracking()
            .Where(r => !r.Deleted)
            .OrderBy(r => r.Name)
            .Select(SummaryProjection)
            .ToListAsync();

    public async Task<ReportDefinitionDetail?> GetByIdAsync(Guid id) {
        var definition = await DetailQuery().FirstOrDefaultAsync(r => r.Id == id);
        return definition == null ? null : ToDetail(definition, includeSql: true);
    }

    public async Task<ReportValidationResult> TestAsync(CreateReportDefinitionRequest request, CancellationToken cancellationToken = default) {
        // The Test button is what *produces* the columns, so it validates the shape of an unsaved definition
        // that legitimately has none yet — hence no "at least one column" and no slug-uniqueness check here.
        var errors = ValidateShape(request);
        if (errors.Count > 0) {
            return new ReportValidationResult { Valid = false, Errors = errors };
        }

        return await executionService.ValidateAndDiscoverAsync(NewEntity(request), cancellationToken);
    }

    public async Task<ReportDefinitionDetail> CreateAsync(CreateReportDefinitionRequest request, Guid userId, CancellationToken cancellationToken = default) {
        await ValidateForSaveAsync(request, existingId: null, cancellationToken);

        var definition = NewEntity(request);
        definition.CreatedAt       = DateTime.UtcNow;
        definition.CreatedByUserId = userId;

        db.ReportDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created report definition {Slug} ({Id})", definition.Slug, definition.Id);
        return ToDetail(definition, includeSql: true);
    }

    public async Task<ReportDefinitionDetail> UpdateAsync(UpdateReportDefinitionRequest request, Guid userId, CancellationToken cancellationToken = default) {
        var definition = await db.ReportDefinitions
            .Include(r => r.Columns)
            .Include(r => r.Variables)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (definition == null) {
            throw new KeyNotFoundException($"Report definition with ID {request.Id} not found.");
        }

        await ValidateForSaveAsync(request, request.Id, cancellationToken);

        ApplyScalars(definition, request);
        definition.UpdatedAt       = DateTime.UtcNow;
        definition.UpdatedByUserId = userId;

        // Columns and variables are replaced wholesale. Delete and re-insert in two saves inside one
        // transaction: a single save could otherwise insert a re-used (ReportDefinitionId, Key) before it
        // deletes the old row and trip the unique index.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Copy first: RemoveRange enumerates what it is handed, and EF's fixup mutates the navigation
        // collections as the children change state.
        db.ReportColumns.RemoveRange(definition.Columns.ToList());
        db.ReportVariables.RemoveRange(definition.Variables.ToList());
        await db.SaveChangesAsync(cancellationToken);

        ApplyChildren(definition, request);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Updated report definition {Slug} ({Id})", definition.Slug, definition.Id);
        return ToDetail(definition, includeSql: true);
    }

    public async Task<bool> DeleteAsync(Guid id) {
        var definition = await db.ReportDefinitions.FirstOrDefaultAsync(r => r.Id == id);
        if (definition == null) {
            return false;
        }

        // A hard delete on purpose: the FK cascades take the columns, variables and grants with it. A soft
        // delete (BaseEntity.Deleted) fires no cascade and would leave grant rows pointing at a report
        // nobody can see. Execution audits carry no FK, so the forensic trail survives either way.
        db.ReportDefinitions.Remove(definition);
        await db.SaveChangesAsync();

        logger.LogInformation("Deleted report definition {Slug} ({Id})", definition.Slug, id);
        return true;
    }

    // ---------------------------------------------------------------- end user, live access-checked

    public async Task<IReadOnlyList<ReportDefinitionSummary>> GetAvailableAsync(Guid userId) =>
        await AccessibleReports(await LoadAccessAsync(userId))
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(SummaryProjection)
            .ToListAsync();

    public async Task<ReportDefinitionDetail?> GetBySlugAsync(string slug, Guid userId) {
        var access     = await LoadAccessAsync(userId);
        var definition = await LoadAccessibleAsync(slug, access);

        // The runner has no business seeing the query; only a superuser gets the SQL back.
        return definition == null ? null : ToDetail(definition, includeSql: access.SuperUser);
    }

    public async Task<ReportDefinition?> GetForExecutionAsync(string slug, Guid userId) =>
        await LoadAccessibleAsync(slug, await LoadAccessAsync(userId));


    public async Task LogExecutionAsync(ReportExecutionAudit audit) {
        try {
            db.ReportExecutionAudits.Add(audit);
            await db.SaveChangesAsync();
        }
        catch (Exception ex) {
            // Forensic, not transactional: never fail a user's report over the audit row.
            logger.LogError(ex, "Failed to write report execution audit for report {ReportId}", audit.ReportDefinitionId);
            db.Entry(audit).State = EntityState.Detached;
        }
    }

    // ---------------------------------------------------------------- access

    /// <summary>The user facts an access decision needs, read fresh — never a login-time snapshot.</summary>
    private sealed record ReportAccess(bool Found, bool SuperUser, Guid? AuthorizationGroupId);

    private async Task<ReportAccess> LoadAccessAsync(Guid userId) {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.SuperUser, u.AuthorizationGroupId })
            .FirstOrDefaultAsync();

        return user == null
            ? new ReportAccess(false, false, null)
            : new ReportAccess(true, user.SuperUser, user.AuthorizationGroupId);
    }

    /// <summary>
    /// The reports <paramref name="access"/> may run. This is the security boundary — every caller that
    /// answers "may this user see this report" goes through here.
    /// </summary>
    private IQueryable<ReportDefinition> AccessibleReports(ReportAccess access) {
        // `Deleted` is spelled out even though SystemDbContext's global query filter already applies it: an
        // access check must not silently depend on a filter configured in another file.
        var reports = db.ReportDefinitions.Where(r => !r.Deleted && r.Enabled);

        if (!access.Found) {
            return reports.Where(r => false);
        }

        if (access.SuperUser) {
            return reports;
        }

        if (access.AuthorizationGroupId is not { } groupId) {
            return reports.Where(r => false);
        }

        // Live against the join table on every call, never SessionInfo.Roles: a grant must work without a
        // re-login, and a newly created report must not be invisible to everyone already logged in.
        return reports.Where(r => db.AuthorizationGroupReports
            .Any(g => g.ReportDefinitionId == r.Id && g.AuthorizationGroupId == groupId));
    }

    private async Task<ReportDefinition?> LoadAccessibleAsync(string slug, ReportAccess access) {
        var normalized = slug.Trim().ToLowerInvariant();

        return await AccessibleReports(access)
            .AsNoTracking()
            .Include(r => r.Columns)
            .Include(r => r.Variables)
            .FirstOrDefaultAsync(r => r.Slug.ToLower() == normalized);
    }

    // ---------------------------------------------------------------- validation

    /// <summary>
    /// Everything a save needs on top of <see cref="ValidateShape"/>: uniqueness, at least one column, and
    /// the engine's own compile of both shapes plus every lookup query.
    /// </summary>
    private async Task ValidateForSaveAsync(CreateReportDefinitionRequest request, Guid? existingId, CancellationToken cancellationToken) {
        var errors = ValidateShape(request);

        if (request.Columns.Count == 0) {
            errors.Add("A report must declare at least one column — run Test to discover them.");
        }

        if (await SlugTakenAsync(request.Slug, existingId)) {
            errors.Add($"Slug '{request.Slug.Trim()}' is already used by another report.");
        }

        if (errors.Count > 0) {
            throw new ReportValidationException(errors);
        }

        // Compiles the page and count shapes plus every lookup, inside the always-rollback scope.
        var validation = await executionService.ValidateAndDiscoverAsync(NewEntity(request), cancellationToken);
        if (!validation.Valid) {
            throw new ReportValidationException(validation.Errors);
        }
    }

    private async Task<bool> SlugTakenAsync(string slug, Guid? existingId) {
        var normalized = slug.Trim().ToLowerInvariant();

        return await db.ReportDefinitions
            .AnyAsync(r => !r.Deleted && r.Slug.ToLower() == normalized && (existingId == null || r.Id != existingId));
    }

    /// <summary>
    /// Definition-level checks that hold for both Test and save. Pure, and it collects every problem rather
    /// than stopping at the first so one round trip surfaces them all.
    /// </summary>
    private static List<string> ValidateShape(CreateReportDefinitionRequest request) {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Slug)) {
            errors.Add("Slug is required.");
        }
        else if (!SlugPattern.IsMatch(request.Slug.Trim())) {
            errors.Add($"Slug '{request.Slug.Trim()}' is invalid: start with a letter or digit, then letters, digits, dots, dashes or underscores.");
        }

        ValidateColumns(request, errors);
        ValidateVariables(request, errors);
        ValidateDefaultSort(request, errors);

        return errors;
    }

    private static void ValidateColumns(CreateReportDefinitionRequest request, List<string> errors) {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Collected up front: a link template may reference any column of the report, including ones
        // declared after it and ones marked hidden (selecting a key purely to link by is the intended
        // pattern), so per-column validation cannot resolve placeholders on its own.
        var declaredKeys = request.Columns
            .Select(c => c.Key?.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .Select(k => k!)
            .ToList();

        foreach (var column in request.Columns) {
            var key = column.Key?.Trim();
            if (string.IsNullOrEmpty(key)) {
                errors.Add("Every column needs a key.");
                continue;
            }

            // Rows come back as dictionaries keyed by this, and ORDER BY resolves against it — under SQL
            // Server's default (case-insensitive) collation "Qty" and "qty" are one column to both.
            if (!seen.Add(key)) {
                errors.Add($"Duplicate column key '{key}' (keys are compared case-insensitively).");
            }

            if (!string.IsNullOrWhiteSpace(column.Align) && NormalizeAlign(column.Align) == null) {
                errors.Add($"Column '{key}' has an invalid alignment '{column.Align}': use left, center or right.");
            }

            ReportLinkTemplate.Validate(column.LinkType, column.LinkTemplate, key, declaredKeys, errors);
        }
    }

    private static void ValidateVariables(CreateReportDefinitionRequest request, List<string> errors) {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var variable in request.Variables) {
            var name = variable.Name?.Trim();
            if (string.IsNullOrEmpty(name)) {
                errors.Add("Every variable needs a name.");
                continue;
            }

            if (!VariableNamePattern.IsMatch(name)) {
                errors.Add($"Variable name '{name}' is invalid: start with a letter or underscore, then letters, digits or underscores, 60 characters max.");
            }
            else if (name.StartsWith("__", StringComparison.Ordinal)) {
                // The obvious regex happily permits __offset/__limit — the engine's own paging parameters —
                // so an admin could otherwise hijack paging just by naming a variable.
                errors.Add($"Variable name '{name}' is reserved: names may not start with '__'.");
            }

            // SqlCommand.Parameters lookup is case-insensitive, so @foo and @Foo collide.
            if (!seen.Add(name)) {
                errors.Add($"Duplicate variable name '{name}' (names are compared case-insensitively).");
            }

            ValidateVariableType(variable, name, errors);
        }
    }

    private static void ValidateVariableType(ReportVariableRequest variable, string name, List<string> errors) {
        switch (variable.Type) {
            case ReportVariableType.SelectList:
                if (variable.Options.Count == 0) {
                    errors.Add($"Variable '{name}' is a select list but declares no options.");
                }
                // A <Select> item's value can never be null or empty on the frontend.
                if (variable.Options.Any(o => string.IsNullOrWhiteSpace(o.Value))) {
                    errors.Add($"Variable '{name}' has an option with an empty value.");
                }
                if (variable.Options.Select(o => o.Value).Distinct(StringComparer.Ordinal).Count() != variable.Options.Count) {
                    errors.Add($"Variable '{name}' has duplicate option values.");
                }
                // Multi-select binds one CSV parameter that the author unpacks with STRING_SPLIT(@X, ','),
                // so a comma inside a value would silently split into two.
                if (variable.AllowMultiple && variable.Options.Any(o => o.Value?.Contains(',') == true)) {
                    errors.Add($"Variable '{name}' allows multiple values, so its option values cannot contain a comma — they are bound as a single CSV parameter.");
                }
                break;

            case ReportVariableType.SqlLookup:
                if (string.IsNullOrWhiteSpace(variable.LookupSql)) {
                    errors.Add($"Variable '{name}' is a SQL lookup but declares no query.");
                }
                break;

            default:
                if (variable.AllowMultiple) {
                    errors.Add($"Variable '{name}' cannot allow multiple values: only select lists and SQL lookups can.");
                }
                break;
        }
    }

    private static void ValidateDefaultSort(CreateReportDefinitionRequest request, List<string> errors) {
        // Nothing to check against before discovery has produced the columns.
        if (string.IsNullOrWhiteSpace(request.DefaultSortColumnKey) || request.Columns.Count == 0) {
            return;
        }

        var key    = request.DefaultSortColumnKey.Trim();
        var column = FindColumn(request, key);

        if (column == null) {
            errors.Add($"Default sort column '{key}' is not one of the declared columns.");
        }
        else if (!column.Sortable) {
            errors.Add($"Default sort column '{key}' is not sortable.");
        }
    }

    private static ReportColumnRequest? FindColumn(CreateReportDefinitionRequest request, string key) =>
        request.Columns.FirstOrDefault(c => string.Equals(c.Key?.Trim(), key, StringComparison.OrdinalIgnoreCase));

    // ---------------------------------------------------------------- request -> entity

    private static ReportDefinition NewEntity(CreateReportDefinitionRequest request) {
        var definition = new ReportDefinition {
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim(),
            Sql  = request.Sql
        };

        ApplyScalars(definition, request);
        ApplyChildren(definition, request);
        return definition;
    }

    private static void ApplyScalars(ReportDefinition definition, CreateReportDefinitionRequest request) {
        definition.Name                 = request.Name.Trim();
        definition.Slug                 = request.Slug.Trim();
        definition.Description          = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        definition.Sql                  = request.Sql;
        definition.CountSql             = string.IsNullOrWhiteSpace(request.CountSql) ? null : request.CountSql;
        definition.Enabled              = request.Enabled;
        definition.Recompile            = request.Recompile;
        definition.TotalCountMode       = request.TotalCountMode;
        definition.DefaultPageSize      = request.DefaultPageSize;
        definition.DefaultSortColumnKey = ResolveDefaultSortKey(request);
        definition.DefaultSortDirection = request.DefaultSortDirection;
        definition.TimeoutSeconds       = request.TimeoutSeconds;
    }

    /// <summary>
    /// Stores the declared column's exact key rather than whatever the author typed, so the stored default
    /// always matches a stored column key verbatim — the composer emits it into ORDER BY.
    /// </summary>
    private static string? ResolveDefaultSortKey(CreateReportDefinitionRequest request) {
        if (string.IsNullOrWhiteSpace(request.DefaultSortColumnKey)) {
            return null;
        }

        var key = request.DefaultSortColumnKey.Trim();
        return FindColumn(request, key)?.Key.Trim() ?? key;
    }

    private static void ApplyChildren(ReportDefinition definition, CreateReportDefinitionRequest request) {
        definition.Columns.Clear();
        foreach (var column in request.Columns) {
            definition.Columns.Add(new ReportColumn {
                Key      = column.Key.Trim(),
                Label    = column.Label.Trim(),
                Format   = column.Format,
                Align    = NormalizeAlign(column.Align),
                Decimals = column.Decimals,
                Visible  = column.Visible,
                Sortable = column.Sortable,
                Nullable = column.Nullable,
                LinkType = column.LinkType,
                // Normalized, not stored raw: a None column must not keep a template it does not use.
                LinkTemplate = ReportLinkTemplate.Normalize(column.LinkType, column.LinkTemplate),
                Order        = column.Order
            });
        }

        definition.Variables.Clear();
        foreach (var variable in request.Variables) {
            definition.Variables.Add(new ReportVariable {
                Name          = variable.Name.Trim(),
                Label         = variable.Label.Trim(),
                Type          = variable.Type,
                Decimals      = variable.Decimals,
                Required      = variable.Required,
                AllowMultiple = variable.AllowMultiple,
                DefaultValue  = variable.DefaultValue,
                StaticOptions = SerializeOptions(variable),
                // Only a SqlLookup has a query; keeping one on a type that ignores it is a trap for the
                // next author to read.
                LookupSql = variable.Type == ReportVariableType.SqlLookup ? variable.LookupSql : null,
                Order     = variable.Order
            });
        }
    }

    private static string? SerializeOptions(ReportVariableRequest variable) =>
        variable.Type != ReportVariableType.SelectList || variable.Options.Count == 0
            ? null
            : JsonSerializer.Serialize(variable.Options, StaticOptionsJson);

    private static string? NormalizeAlign(string? align) {
        var value = align?.Trim().ToLowerInvariant();
        return value is "left" or "center" or "right" ? value : null;
    }

    // ---------------------------------------------------------------- entity -> DTO

    private IQueryable<ReportDefinition> DetailQuery() =>
        db.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Columns)
            .Include(r => r.Variables)
            .Where(r => !r.Deleted);

    private ReportDefinitionDetail ToDetail(ReportDefinition definition, bool includeSql) =>
        new() {
            Id                   = definition.Id,
            Name                 = definition.Name,
            Slug                 = definition.Slug,
            Description          = definition.Description,
            Sql                  = includeSql ? definition.Sql : null,
            CountSql             = includeSql ? definition.CountSql : null,
            Enabled              = definition.Enabled,
            Recompile            = definition.Recompile,
            TotalCountMode       = definition.TotalCountMode,
            DefaultPageSize      = definition.DefaultPageSize,
            DefaultSortColumnKey = definition.DefaultSortColumnKey,
            DefaultSortDirection = definition.DefaultSortDirection,
            TimeoutSeconds       = definition.TimeoutSeconds,
            Columns              = definition.Columns.OrderBy(c => c.Order).ThenBy(c => c.Key).Select(ToDescriptor).ToList(),
            Variables            = definition.Variables.OrderBy(v => v.Order).ThenBy(v => v.Name).Select(v => ToDescriptor(v, includeSql)).ToList(),
            CreatedAt            = definition.CreatedAt,
            UpdatedAt            = definition.UpdatedAt
        };

    private static ReportColumnDescriptor ToDescriptor(ReportColumn column) =>
        new() {
            Key      = column.Key,
            Label    = column.Label,
            Format   = column.Format,
            Align    = column.Align,
            Decimals = column.Decimals,
            Visible  = column.Visible,
            Sortable = column.Sortable,
            Nullable = column.Nullable,
            LinkType = column.LinkType,
            LinkTemplate = column.LinkTemplate,
            Order        = column.Order
            // SqlTypeName stays null: it is discovery-only output, not stored runtime metadata.
        };

    private ReportVariableDescriptor ToDescriptor(ReportVariable variable, bool includeSql) =>
        new() {
            Name          = variable.Name,
            Label         = variable.Label,
            Type          = variable.Type,
            Decimals      = variable.Decimals,
            Required      = variable.Required,
            AllowMultiple = variable.AllowMultiple,
            DefaultValue  = variable.DefaultValue,
            Options       = ParseOptions(variable),
            HasLookup     = variable.Type == ReportVariableType.SqlLookup && !string.IsNullOrWhiteSpace(variable.LookupSql),
            LookupSql     = includeSql ? variable.LookupSql : null,
            Order         = variable.Order
        };

    /// <summary>
    /// Parses <see cref="ReportVariable.StaticOptions"/> so the client never has to. We wrote this JSON, so a
    /// failure here means a hand-edited row: log it loudly rather than letting one bad blob throw the whole
    /// report's metadata away.
    /// </summary>
    private List<ReportLookupOption> ParseOptions(ReportVariable variable) {
        if (variable.Type != ReportVariableType.SelectList || string.IsNullOrWhiteSpace(variable.StaticOptions)) {
            return [];
        }

        try {
            var options = JsonSerializer.Deserialize<List<ReportLookupOption>>(variable.StaticOptions, StaticOptionsJson);

            // A <Select> item's value must never be null or empty.
            return options?.Where(o => !string.IsNullOrEmpty(o.Value)).ToList() ?? [];
        }
        catch (JsonException ex) {
            logger.LogError(ex, "Report variable {Variable} has unreadable StaticOptions JSON; rendering it with no options", variable.Name);
            return [];
        }
    }
}
