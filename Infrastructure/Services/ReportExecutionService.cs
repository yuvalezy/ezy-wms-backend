using System.Data.Common;
using System.Diagnostics;
using Adapters.Common.SBO.Services;
using Core.DTOs.Reports;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// The only DB-touching class in the reporting engine — the lexer, composer and parameter binder it leans on
/// are pure. Takes a <see cref="ReportDefinition"/> entity rather than an id, so it never sees
/// <c>SystemDbContext</c> and the Test button can hand it an unsaved definition.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every</b> path — page, count, discover, lookup, validate — runs inside
/// <see cref="SboDatabaseService.BeginReportingScopeAsync"/>: an always-rollback transaction on the reporting
/// connection. That is deliberate rather than incidental. The Test button means admins paste half-finished SQL
/// and run it against a live SAP database, so that path must be in the scope; and once Test and Execute share
/// one code path, scoping everything is simpler than scoping some of it.
/// </para>
/// <para>
/// The scope is why <c>GetDataTableAsync</c>/<c>QueryAsync</c> are unusable here: they open their own
/// untransacted connection. Commands are built as
/// <c>new SqlCommand(sql, scope.Connection, scope.Transaction)</c>, following the
/// <c>SboGeneralRepository.ValidateMimicQueryAsync</c> precedent.
/// </para>
/// <para>
/// The scope pins SAP's transaction log for its duration, so every scope here is bounded by an explicit
/// <c>CommandTimeout</c> and each one covers exactly one page. Export chunks page-by-page — many short scopes,
/// never one long one.
/// </para>
/// </remarks>
public class ReportExecutionService(
    SboDatabaseService dbService,
    ILogger<ReportExecutionService> logger) : IReportExecutionService {
    /// <summary>Applied when a definition carries no usable timeout. ADO.NET's own 30s default is never relied on.</summary>
    private const int DefaultTimeoutSeconds = 60;

    private const int MinTimeoutSeconds = 5;
    private const int MaxTimeoutSeconds = 300;

    /// <summary>
    /// Lookup rows are capped in the read loop: a dropdown is not a data dump, and an author pointing one at
    /// OITM should not be able to stream 200k options into every client.
    /// </summary>
    public const int LookupRowCap = 1000;

    /// <summary>Output column names that <c>ORDER BY</c> rejects outright, so they are never marked sortable.</summary>
    private static readonly HashSet<string> UnsortableSqlTypes =
        new(StringComparer.OrdinalIgnoreCase) { "text", "ntext", "image", "xml", "geography", "geometry" };

    public async Task<ReportPageResult> ExecutePageAsync(
        ReportDefinition definition,
        ReportExecutionRequest request,
        CancellationToken cancellationToken = default) {
        ReportSqlComposer.ResolvedSort sort =
            ReportSqlComposer.ResolveSort(definition, request.SortColumnKey, request.SortDirection);
        int limit = ReportParameterBinder.ClampLimit(request.Limit, definition.DefaultPageSize);
        int offset = ReportParameterBinder.ValidateOffset(request.Offset);
        string sql = ReportSqlComposer.ComposePage(definition, sort);

        var parameters = new List<SqlParameter>(ReportParameterBinder.Bind(definition.Variables, request)) {
            ReportParameterBinder.CreateOffsetParameter(offset),
            // limit + 1 — the probe row is what makes HasMore free. No count round trip, real Next/Prev from
            // one query. It is trimmed off below so the caller never sees it.
            ReportParameterBinder.CreateLimitParameter(limit + 1)
        };

        var stopwatch = Stopwatch.StartNew();
        var rows = new List<Dictionary<string, object?>>(limit + 1);

        await using (SboRollbackScope scope = await dbService.BeginReportingScopeAsync()) {
            await using SqlCommand command = CreateCommand(scope, sql, definition, parameters);
            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (rows.Count <= limit && await reader.ReadAsync(cancellationToken)) rows.Add(ReadRow(reader));
        }

        stopwatch.Stop();

        bool hasMore = rows.Count > limit;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        return new ReportPageResult {
            Columns = DescribeDeclaredColumns(definition),
            Rows = rows,
            HasMore = hasMore,
            Offset = offset,
            Limit = limit,
            SortColumnKey = sort.ColumnKey,
            SortDirection = sort.Direction,
            DurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    public async Task<long> CountAsync(
        ReportDefinition definition,
        ReportExecutionRequest request,
        CancellationToken cancellationToken = default) {
        // The count shape hard-codes OFFSET 0 ROWS and (SELECT NULL) for the order, so it takes the filter
        // parameters but neither @__offset nor @__limit.
        string sql = ReportSqlComposer.ComposeCount(definition);
        IReadOnlyList<SqlParameter> parameters = ReportParameterBinder.Bind(definition.Variables, request);

        await using SboRollbackScope scope = await dbService.BeginReportingScopeAsync();
        await using SqlCommand command = CreateCommand(scope, sql, definition, parameters);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0L : Convert.ToInt64(result);
    }

    public async Task<IReadOnlyList<ReportLookupOption>> ExecuteLookupAsync(
        ReportDefinition definition,
        string variableName,
        ReportExecutionRequest request,
        CancellationToken cancellationToken = default) {
        ReportVariable variable = definition.Variables
                                      .FirstOrDefault(v => string.Equals(v.Name, variableName, StringComparison.OrdinalIgnoreCase))
                                  ?? throw new ReportValidationException($"Report '{definition.Slug}' has no variable '{variableName}'.");

        if (variable.Type != ReportVariableType.SqlLookup)
            throw new ReportValidationException($"Variable '{variable.Name}' is a {variable.Type}, not a SqlLookup, so it has no lookup query.");

        ThrowIfInvalid(ReportSqlComposer.ValidateLookupSql(variable.LookupSql));

        string sql = ReportSqlLexer.StripTrailingSemicolon(variable.LookupSql!);
        // Bind *every* declared variable, not just the ones this lookup references: unreferenced parameters are
        // free under sp_executesql, and it is what makes a cascading warehouse -> bin lookup work for nothing.
        IReadOnlyList<SqlParameter> parameters = ReportParameterBinder.Bind(AsOptional(definition.Variables), request);

        var options = new List<ReportLookupOption>();

        await using SboRollbackScope scope = await dbService.BeginReportingScopeAsync();
        await using SqlCommand command = CreateCommand(scope, sql, definition, parameters);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        ThrowIfInvalid(ValidateLookupColumns(reader, variable.Name));

        int valueOrdinal = reader.GetOrdinal("Value");
        int labelOrdinal = reader.GetOrdinal("Label");

        while (options.Count < LookupRowCap && await reader.ReadAsync(cancellationToken)) {
            object rawValue = reader.GetValue(valueOrdinal);
            if (rawValue == DBNull.Value) continue;

            string value = Stringify(rawValue);
            // A dropdown option with an empty value is unselectable in the client, so it is dropped rather than
            // shipped as a row the user can pick but never submit.
            if (value.Length == 0) continue;

            object rawLabel = reader.GetValue(labelOrdinal);
            string label = rawLabel == DBNull.Value ? value : Stringify(rawLabel);

            options.Add(new ReportLookupOption { Value = value, Label = label.Length == 0 ? value : label });
        }

        if (options.Count == LookupRowCap)
            logger.LogWarning(
                "Lookup for variable {Variable} on report {Slug} hit the {Cap}-row cap; the dropdown is truncated.",
                variable.Name, definition.Slug, LookupRowCap);

        return options;
    }

    public async Task<ReportValidationResult> ValidateAndDiscoverAsync(
        ReportDefinition definition,
        CancellationToken cancellationToken = default) {
        var result = new ReportValidationResult();

        // Pure checks first. A shape error means the composer would only throw, and there is no point paying a
        // round trip to SAP to learn what the lexer already knows.
        result.Errors.AddRange(ReportSqlComposer.ValidateReportSql(definition.Sql));
        if (!string.IsNullOrWhiteSpace(definition.CountSql))
            result.Errors.AddRange(ReportSqlComposer.ValidateCountSql(definition.CountSql));
        result.Errors.AddRange(ReportParameterBinder.ValidateDeclarations(definition.Variables));

        List<ReportVariable> lookups = [.. definition.Variables.Where(v => v.Type == ReportVariableType.SqlLookup)];
        foreach (ReportVariable lookup in lookups) {
            result.Errors.AddRange(ReportSqlComposer.ValidateLookupSql(lookup.LookupSql)
                .Select(e => $"Variable '{lookup.Name}': {e}"));
        }

        if (result.Errors.Count > 0) return result;

        try {
            await using SboRollbackScope scope = await dbService.BeginReportingScopeAsync();

            // Discovery runs first because it is what *produces* the column keys: {{orderBy}} needs a key, but
            // discovery is where keys come from. Hence its (SELECT NULL) order — which also kills the sort
            // operator, so it is cheap.
            List<ReportColumnDescriptor> columns = await DiscoverAsync(scope, definition, result.Errors, cancellationToken);
            if (result.Errors.Count > 0) return result;
            result.Columns = columns;

            CrossCheckDeclaredColumns(definition, columns, result);
            if (result.Errors.Count > 0) return result;

            // On the Test path the author has no columns yet, so the page shape is compiled against the columns
            // just discovered; on save it is compiled against the declared ones.
            ReportDefinition probe = definition.Columns.Count > 0 ? definition : WithColumns(definition, columns);
            await CompileShapesAsync(scope, definition, probe, lookups, cancellationToken);
        }
        catch (ReportValidationException ex) {
            result.Errors.AddRange(ex.Errors);
        }
        catch (SqlException ex) {
            // Verbatim: SQL Server's own message ("Must declare the scalar variable '@X'", "Invalid column
            // name 'DocNumm'") is far more useful to the author than anything paraphrased here.
            result.Errors.Add(ex.Message);
        }

        result.Valid = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// Compiles the page shape, the count shape and every lookup by running each one — the "mimic query"
    /// doctrine: only a real execution makes SQL Server fully parse and bind the statement and report its
    /// actual error.
    /// </summary>
    /// <remarks>
    /// Compiling the <b>count</b> shape is not optional. Skipping it is the trap that lets an
    /// <c>OPTION (RECOMPILE)</c> report save clean and then fail the moment someone asks for a total, because
    /// <c>OPTION</c> is illegal inside the derived table the count wrap introduces.
    /// </remarks>
    private async Task CompileShapesAsync(
        SboRollbackScope scope,
        ReportDefinition definition,
        ReportDefinition probe,
        IReadOnlyList<ReportVariable> lookups,
        CancellationToken cancellationToken) {
        // Every sortable column, not just the default. The shape space is finite — one page shape per sortable
        // column, plus the count — so save time can cover the whole of runtime, and that is the only reason the
        // author is allowed to hand-write the ORDER BY tail at all. Compiling only the default sort is what let
        // `ORDER BY {{orderBy}}, T0."ItemCode"` save clean and then fail with "A column has been specified more
        // than once in the order by list" the first time someone clicked the ItemCode header: {{orderBy}} expands
        // to that same column, and the collision exists only for that one sort key.
        // Direction is not enumerated: ASC/DESC changes no binding, only the keyword.
        foreach (ReportSqlComposer.ResolvedSort sort in EnumerateSortShapes(probe)) {
            // Bound fresh per shape, never hoisted out of the loop: a SqlParameter may belong to exactly one
            // SqlParameterCollection, so reusing one list across commands throws "The SqlParameter is already
            // contained by another SqlParameterCollection" on the second iteration.
            var pageParameters = new List<SqlParameter>(ReportParameterBinder.BindAllNull(definition.Variables)) {
                ReportParameterBinder.CreateOffsetParameter(0),
                ReportParameterBinder.CreateLimitParameter(1)
            };
            await CompileAsync(scope, ReportSqlComposer.ComposePage(probe, sort), definition, pageParameters, cancellationToken);
        }

        await CompileAsync(scope, ReportSqlComposer.ComposeCount(probe), definition,
            ReportParameterBinder.BindAllNull(definition.Variables), cancellationToken);

        foreach (ReportVariable lookup in lookups) {
            string sql = ReportSqlLexer.StripTrailingSemicolon(lookup.LookupSql!);
            await using SqlCommand command = CreateCommand(scope, sql, definition,
                ReportParameterBinder.BindAllNull(definition.Variables));
            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            ThrowIfInvalid(ValidateLookupColumns(reader, lookup.Name));
        }
    }

    /// <summary>
    /// Every distinct page shape a caller could ever ask for: one per sortable column, plus the resolved
    /// default. Returned as an ordered, de-duplicated set so the default is compiled first — its error message
    /// is the one the author is most likely to be able to act on.
    /// </summary>
    /// <remarks>
    /// Enumerating this is what makes save-time validation honest. An author's ORDER BY tail interacts with the
    /// column {{orderBy}} expands to, so a report can be perfectly valid under its default sort and invalid
    /// under another — the failure then surfaces to a warehouse user mid-shift rather than to the author.
    /// </remarks>
    private static IEnumerable<ReportSqlComposer.ResolvedSort> EnumerateSortShapes(ReportDefinition probe) {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ReportSqlComposer.ResolvedSort defaultSort = ReportSqlComposer.ResolveSort(probe, null, null);
        seen.Add(defaultSort.ColumnKey);
        yield return defaultSort;

        foreach (ReportColumn column in probe.Columns.Where(c => c.Sortable)) {
            if (seen.Add(column.Key))
                yield return new ReportSqlComposer.ResolvedSort(column.Key, probe.DefaultSortDirection);
        }
    }

    /// <summary>Runs a shape purely to make SQL Server compile it. No row is ever read.</summary>
    private async Task CompileAsync(
        SboRollbackScope scope,
        string sql,
        ReportDefinition definition,
        IReadOnlyList<SqlParameter> parameters,
        CancellationToken cancellationToken) {
        await using SqlCommand command = CreateCommand(scope, sql, definition, parameters);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        // Parsing and binding already happened; reading the rows would only cost time.
    }

    /// <summary>
    /// Runs the Discover shape and reads <c>GetColumnSchema()</c> <b>without ever calling ReadAsync</b>.
    /// </summary>
    /// <remarks>
    /// Not <c>CommandBehavior.SchemaOnly</c> (deprecated <c>FMTONLY</c>; breaks on temp tables and does not
    /// traverse the real code path) and not <c>sp_describe_first_result_set</c> (a second code path to test).
    /// The Discover shape's <c>FETCH NEXT 1</c> is deliberate — <c>FETCH NEXT 0 ROWS ONLY</c> is illegal.
    /// </remarks>
    private async Task<List<ReportColumnDescriptor>> DiscoverAsync(
        SboRollbackScope scope,
        ReportDefinition definition,
        List<string> errors,
        CancellationToken cancellationToken) {
        string sql = ReportSqlComposer.ComposeDiscover(definition);
        await using SqlCommand command = CreateCommand(scope, sql, definition,
            ReportParameterBinder.BindAllNull(definition.Variables));
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        var columns = new List<ReportColumnDescriptor>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int ordinal = 0;

        foreach (DbColumn column in reader.GetColumnSchema()) {
            string name = column.ColumnName ?? string.Empty;

            // Unique, non-empty output names are enforced here and only here — it is one rule that solves three
            // problems: an unnamed expression is Msg 8155 in the count wrap, a duplicate is Msg 8156, and both
            // break the Dictionary row keys and an unambiguous ORDER BY [alias].
            if (string.IsNullOrWhiteSpace(name)) {
                errors.Add($"Output column {ordinal + 1} has no name. Alias every expression, e.g. "
                           + "SUM(T0.\"Quantity\") AS \"TotalQty\".");
            }
            else if (!seen.Add(name)) {
                errors.Add($"Output column '{name}' appears more than once. Give each one a distinct alias, "
                           + "e.g. T0.\"CardName\" AS \"Customer\", T1.\"CardName\" AS \"Vendor\".");
            }
            else {
                columns.Add(Describe(column, name, ordinal));
            }

            ordinal++;
        }

        if (errors.Count == 0 && columns.Count == 0)
            errors.Add("The report SQL returns no columns.");

        return columns;
    }

    /// <summary>
    /// Maps one discovered column. Mapping is by CLR <see cref="DbColumn.DataType"/>, with
    /// <see cref="DbColumn.DataTypeName"/> used only to disambiguate the temporal types.
    /// </summary>
    private static ReportColumnDescriptor Describe(DbColumn column, string name, int ordinal) {
        string sqlType = column.DataTypeName ?? string.Empty;

        var descriptor = new ReportColumnDescriptor {
            Key = name,
            Label = name,
            Format = MapFormat(column, sqlType),
            // Align stays null: the client derives it from the format, and a stored value would override it.
            Align = null,
            Decimals = MapDecimals(column),
            Visible = true,
            Sortable = !UnsortableSqlTypes.Contains(sqlType),
            Nullable = column.AllowDBNull ?? true,
            Order = ordinal,
            SqlTypeName = sqlType
        };

        return descriptor;
    }

    private static ReportColumnFormat MapFormat(DbColumn column, string sqlType) {
        Type? type = column.DataType;
        if (type is null) return ReportColumnFormat.Text;

        if (type == typeof(bool)) return ReportColumnFormat.Boolean;
        if (type == typeof(byte[])) return ReportColumnFormat.Binary;
        if (type == typeof(TimeSpan)) return ReportColumnFormat.Time;
        if (type == typeof(DateTimeOffset)) return ReportColumnFormat.DateTime;

        if (type == typeof(DateTime)) {
            // The one place DataTypeName earns its keep: date, datetime, datetime2 and smalldatetime all
            // surface as CLR DateTime.
            return sqlType.Equals("date", StringComparison.OrdinalIgnoreCase)
                ? ReportColumnFormat.Date
                : ReportColumnFormat.DateTime;
        }

        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)
            || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)) {
            return ReportColumnFormat.Number;
        }

        // Everything else — string, Guid, char — is Text. Notably char(1) lands here rather than on Boolean:
        // SAP stores Y/N in char(1) *and* dozens of code columns, so the author flips the ones that are flags.
        return ReportColumnFormat.Text;
    }

    private static int MapDecimals(DbColumn column) {
        Type? type = column.DataType;

        if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)) {
            return 0;
        }

        if (type == typeof(decimal) && column.NumericScale is { } scale) return Math.Clamp(scale, 0, 10);

        return 2;
    }

    /// <summary>
    /// A declared column that the query does not actually output is a hard error, not cosmetic: the row
    /// dictionary would never carry that key, and if it is sortable then <c>ORDER BY [key]</c> is an invalid
    /// column name on the very first page.
    /// </summary>
    private static void CrossCheckDeclaredColumns(
        ReportDefinition definition,
        IReadOnlyList<ReportColumnDescriptor> discovered,
        ReportValidationResult result) {
        if (definition.Columns.Count == 0) return;

        var output = new HashSet<string>(discovered.Select(c => c.Key), StringComparer.OrdinalIgnoreCase);
        foreach (ReportColumn declared in definition.Columns) {
            if (!output.Contains(declared.Key))
                result.Errors.Add($"Declared column '{declared.Key}' is not one of the query's output columns "
                                  + $"({string.Join(", ", output)}). Re-run Test to refresh the columns.");
        }

        var declaredKeys = new HashSet<string>(definition.Columns.Select(c => c.Key), StringComparer.OrdinalIgnoreCase);
        foreach (ReportColumnDescriptor column in discovered) {
            if (!declaredKeys.Contains(column.Key))
                result.Warnings.Add($"The query outputs '{column.Key}', which the report does not declare, so it "
                                    + "will not be shown. Re-run Test to pick it up.");
        }
    }

    private static IReadOnlyList<string> ValidateLookupColumns(SqlDataReader reader, string variableName) {
        var names = reader.GetColumnSchema()
            .Select(c => c.ColumnName ?? string.Empty)
            .ToList();

        bool shaped = names.Count == 2
                      && names.Any(n => n.Equals("Value", StringComparison.OrdinalIgnoreCase))
                      && names.Any(n => n.Equals("Label", StringComparison.OrdinalIgnoreCase));

        return shaped
            ? []
            : [
                $"The lookup query for variable '{variableName}' must return exactly two columns named Value and "
                + $"Label (it returned {names.Count}: {string.Join(", ", names.Select(n => n.Length == 0 ? "<unnamed>" : n))})."
            ];
    }

    /// <summary>
    /// Clones the declarations with <c>Required</c> cleared, for the lookup path.
    /// </summary>
    /// <remarks>
    /// A lookup is what <i>helps the user fill</i> the filters, so enforcing Required while binding it would
    /// mean the dropdown for a required variable could never load — it would reject the request for the very
    /// value it exists to supply. Cloning rather than mutating matters: these entities are EF-tracked, and
    /// flipping <c>Required</c> in place would persist on the next SaveChanges.
    /// </remarks>
    private static IEnumerable<ReportVariable> AsOptional(IEnumerable<ReportVariable> variables) =>
        variables.Select(v => new ReportVariable {
            Name = v.Name,
            Label = v.Label,
            Type = v.Type,
            Decimals = v.Decimals,
            Required = false,
            AllowMultiple = v.AllowMultiple,
            DefaultValue = v.DefaultValue,
            StaticOptions = v.StaticOptions,
            LookupSql = v.LookupSql,
            Order = v.Order
        });

    private static ReportDefinition WithColumns(ReportDefinition definition, IReadOnlyList<ReportColumnDescriptor> columns) =>
        new() {
            Name = definition.Name,
            Slug = definition.Slug,
            Sql = definition.Sql,
            CountSql = definition.CountSql,
            Recompile = definition.Recompile,
            TotalCountMode = definition.TotalCountMode,
            DefaultPageSize = definition.DefaultPageSize,
            DefaultSortColumnKey = definition.DefaultSortColumnKey,
            DefaultSortDirection = definition.DefaultSortDirection,
            TimeoutSeconds = definition.TimeoutSeconds,
            Columns = [.. columns.Select(c => new ReportColumn {
                Key = c.Key, Label = c.Label, Format = c.Format, Align = c.Align, Decimals = c.Decimals,
                Visible = c.Visible, Sortable = c.Sortable, Nullable = c.Nullable, Order = c.Order
            })]
        };

    /// <summary>Runtime column metadata: the author's declarations, never the discovery-only SqlTypeName.</summary>
    private static List<ReportColumnDescriptor> DescribeDeclaredColumns(ReportDefinition definition) =>
        [.. definition.Columns.OrderBy(c => c.Order).Select(c => new ReportColumnDescriptor {
            Key = c.Key,
            Label = c.Label,
            Format = c.Format,
            Align = c.Align,
            Decimals = c.Decimals,
            Visible = c.Visible,
            Sortable = c.Sortable,
            Nullable = c.Nullable,
            Order = c.Order
        })];

    private static SqlCommand CreateCommand(
        SboRollbackScope scope,
        string sql,
        ReportDefinition definition,
        IReadOnlyList<SqlParameter> parameters) {
        // Bound to the scope's transaction, not just its connection — an untransacted command here would defeat
        // the whole always-rollback guarantee.
        var command = new SqlCommand(sql, scope.Connection, scope.Transaction) {
            CommandTimeout = ResolveTimeout(definition)
        };

        foreach (SqlParameter parameter in parameters) command.Parameters.Add(parameter);
        return command;
    }

    /// <summary>
    /// The timeout is mandatory — ADO.NET's silent 30s default is never what a report wants, in either
    /// direction. Clamped so a hand-edited definition row cannot ask for 0 (which means "no limit").
    /// </summary>
    private static int ResolveTimeout(ReportDefinition definition) {
        int seconds = definition.TimeoutSeconds <= 0 ? DefaultTimeoutSeconds : definition.TimeoutSeconds;
        return Math.Clamp(seconds, MinTimeoutSeconds, MaxTimeoutSeconds);
    }

    private static Dictionary<string, object?> ReadRow(SqlDataReader reader) {
        var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.Ordinal);
        for (int i = 0; i < reader.FieldCount; i++) {
            object value = reader.GetValue(i);
            // NULL becomes null, never a zero or an empty string — a report that renders a NULL as 0 lies
            // about the data.
            row[reader.GetName(i)] = value == DBNull.Value ? null : value;
        }

        return row;
    }

    private static string Stringify(object value) =>
        value switch {
            string s => s,
            DateTime d => d.ToString("O"),
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

    private static void ThrowIfInvalid(IReadOnlyList<string> errors) {
        if (errors.Count > 0) throw new ReportValidationException(errors);
    }
}
