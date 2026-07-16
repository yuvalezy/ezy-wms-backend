using System.Text;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;

namespace Infrastructure.Services;

/// <summary>
/// Expands the author's <c>{{orderBy}}</c>/<c>{{paging}}</c> tokens into the three runtime shapes.
/// <b>Pure</b>: no DB, no I/O, no <c>SystemDbContext</c>.
/// </summary>
/// <remarks>
/// <para>
/// This class is where the invariant everything hangs off is enforced:
/// <b>shape is fixed at save time; only parameter values vary at run time.</b> Every string this class emits is
/// a pure function of <c>(definition, sortKey, sortDir, mode)</c> — never of a submitted value. That is what makes
/// save-time validation in the rollback scope genuinely cover run time: the shape space is finite, so compiling
/// the page and count shapes once at save proves they compile forever.
/// </para>
/// <para>
/// Three modes:
/// <list type="table">
/// <item><term>Page</term><description><c>[Key] ASC|DESC</c> + <c>OFFSET @__offset ROWS FETCH NEXT @__limit ROWS ONLY</c></description></item>
/// <item><term>Count</term><description><c>(SELECT NULL)</c> + <c>OFFSET 0 ROWS</c>, CTEs hoisted, wrapped in <c>COUNT_BIG(*)</c></description></item>
/// <item><term>Discover</term><description><c>(SELECT NULL)</c> + <c>OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY</c></description></item>
/// </list>
/// </para>
/// </remarks>
public static class ReportSqlComposer {
    /// <summary>The order-by token, written <c>{{orderBy}}</c>. Matched case-sensitively — see <see cref="ValidateReportSql"/>.</summary>
    public const string OrderByTokenName = "orderBy";

    /// <summary>The paging token, written <c>{{paging}}</c>. Matched case-sensitively — see <see cref="ValidateReportSql"/>.</summary>
    public const string PagingTokenName = "paging";

    /// <summary>Alias of the derived table the count shape wraps the main select in. Uses the reserved <c>__</c> prefix.</summary>
    public const string CountAlias = "__c";

    /// <summary>
    /// What <c>{{orderBy}}</c> becomes in the count and discover shapes. Discovery has no choice — it is the
    /// chicken-and-egg: <c>{{orderBy}}</c> needs a column key, but discovery is what <i>produces</i> the keys.
    /// Happily it also kills the sort operator, so both shapes get cheaper.
    /// </summary>
    private const string NullOrderBy = "(SELECT NULL)";

    private const int MaxIdentifierLength = 128;

    /// <summary>
    /// A sort choice that has been checked against the definition's column whitelist. Produced only by
    /// <see cref="ResolveSort"/>; <see cref="ComposePage"/> re-checks it anyway (defence in depth — the cost is
    /// a scan of a handful of columns).
    /// </summary>
    /// <param name="ColumnKey">The <b>stored</b> <see cref="ReportColumn.Key"/>, never the request's own text.</param>
    public readonly record struct ResolvedSort(string ColumnKey, ReportSortDirection Direction);

    /// <summary>
    /// Static validation of a report's main SQL: leading keyword, single statement, and the placeholder contract.
    /// Returns every problem at once rather than one per round trip. Empty means it passed.
    /// </summary>
    /// <remarks>
    /// Deliberately does <b>not</b> cross-check declared against referenced parameters. SQL Server gives that away
    /// free at save time ("Must declare the scalar variable '@X'"), and declared-but-unused is harmless under
    /// <c>sp_executesql</c> — that is a warning, not a rejection.
    /// </remarks>
    public static IReadOnlyList<string> ValidateReportSql(string? sql) {
        var errors = new List<string>();
        ValidateStatementShape(sql, "The report SQL", errors);
        if (!string.IsNullOrWhiteSpace(sql)) ValidateTokens(sql, errors);
        return errors;
    }

    /// <summary>
    /// Static validation of an author-supplied count query. Same statement rules as the main SQL, but placeholders
    /// are rejected: a hand-written count does its own counting, so it has nothing to order or page.
    /// </summary>
    public static IReadOnlyList<string> ValidateCountSql(string? sql) {
        var errors = new List<string>();
        ValidateStatementShape(sql, "The report count SQL", errors);
        if (!string.IsNullOrWhiteSpace(sql)) ValidateNoTokens(sql, "The report count SQL", errors);
        return errors;
    }

    /// <summary>
    /// Static validation of a <c>SqlLookup</c> variable's query. Admin SQL of the same class as the report's own,
    /// so it gets identical treatment; placeholders are meaningless in a dropdown query and are rejected.
    /// </summary>
    public static IReadOnlyList<string> ValidateLookupSql(string? sql) {
        var errors = new List<string>();
        ValidateStatementShape(sql, "The lookup SQL", errors);
        if (!string.IsNullOrWhiteSpace(sql)) ValidateNoTokens(sql, "The lookup SQL", errors);
        return errors;
    }

    /// <summary>
    /// Resolves a requested sort into a whitelisted column key and direction.
    /// </summary>
    /// <remarks>
    /// The whitelist is the point. An <i>unknown</i> key is a hard rejection — a header arrow reading "Qty ▼" over
    /// data actually sorted by <c>DocEntry</c> is how someone ships the wrong pallet. An <i>unspecified</i> key
    /// falls back to the report's default, then to its first sortable column, because <c>{{orderBy}}</c> has to
    /// expand to something.
    /// </remarks>
    /// <exception cref="ReportValidationException">
    /// The requested key is unknown or non-sortable, the report's own default is unknown or non-sortable
    /// (a hand-edited row, or a column renamed by a re-discover), or the report has no sortable column at all.
    /// </exception>
    public static ResolvedSort ResolveSort(
        ReportDefinition definition,
        string? requestedKey,
        ReportSortDirection? requestedDirection) {
        ReportSortDirection direction = requestedDirection ?? definition.DefaultSortDirection;

        if (!string.IsNullOrWhiteSpace(requestedKey)) {
            ReportColumn? requested = FindColumn(definition, requestedKey);
            if (requested is null)
                throw new ReportValidationException($"Unknown sort column '{requestedKey}'.");
            if (!requested.Sortable)
                throw new ReportValidationException($"Column '{requested.Key}' is not sortable.");
            return new ResolvedSort(requested.Key, direction);
        }

        if (!string.IsNullOrWhiteSpace(definition.DefaultSortColumnKey)) {
            ReportColumn? fallback = FindColumn(definition, definition.DefaultSortColumnKey);
            if (fallback is null)
                throw new ReportValidationException(
                    $"The report's default sort column '{definition.DefaultSortColumnKey}' does not match any declared column.");
            if (!fallback.Sortable)
                throw new ReportValidationException(
                    $"The report's default sort column '{fallback.Key}' is not sortable.");
            return new ResolvedSort(fallback.Key, direction);
        }

        ReportColumn? first = definition.Columns
            .Where(c => c.Sortable)
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Key, StringComparer.Ordinal)
            .FirstOrDefault();

        if (first is null)
            throw new ReportValidationException("The report has no sortable column to order by.");

        return new ResolvedSort(first.Key, direction);
    }

    /// <summary>
    /// The Page shape: the author's SQL with a real <c>ORDER BY</c> key and the paging parameters.
    /// </summary>
    /// <remarks>
    /// The engine fetches <c>limit + 1</c> rows through <c>@__limit</c> and trims the extra to derive
    /// <c>HasMore</c> — real Next/Prev from one query, no count.
    /// </remarks>
    public static string ComposePage(ReportDefinition definition, ResolvedSort sort) {
        string body = PrepareBody(definition.Sql);
        string orderBy = $"{QuoteIdentifier(RequireSortableColumnKey(definition, sort.ColumnKey))} {DirectionKeyword(sort.Direction)}";
        string paging = $"OFFSET {ReportParameterBinder.OffsetParameterName} ROWS "
                        + $"FETCH NEXT {ReportParameterBinder.LimitParameterName} ROWS ONLY";

        return Finish(Substitute(body, orderBy, paging), definition.Recompile);
    }

    /// <summary>
    /// The Count shape: CTEs hoisted, the main select wrapped in <c>SELECT COUNT_BIG(*) FROM (…) AS __c</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>COUNT_BIG</c>, not <c>COUNT</c> — the latter throws past 2^31.
    /// </para>
    /// <para>
    /// Honours an author-supplied <see cref="ReportDefinition.CountSql"/> when present. That override exists for
    /// <b>cost</b> (a hand-written cheap count), not for CTEs — this method hoists those on its own.
    /// </para>
    /// </remarks>
    public static string ComposeCount(ReportDefinition definition) {
        if (!string.IsNullOrWhiteSpace(definition.CountSql)) {
            ThrowIfInvalid(ValidateCountSql(definition.CountSql));
            return Finish(ReportSqlLexer.StripTrailingSemicolon(definition.CountSql), definition.Recompile);
        }

        string body = PrepareBody(definition.Sql);
        string inner = Substitute(body, NullOrderBy, "OFFSET 0 ROWS");
        ReportSqlLexer.CteSplit split = ReportSqlLexer.SplitLeadingCtes(inner);

        // The newlines are load-bearing: if the CTE chain or the main select ends in a `--` comment, putting the
        // wrap on the same line would bury it inside that comment.
        string wrapped = $"{split.CtePrefix}\nSELECT COUNT_BIG(*) FROM (\n{split.MainSelect}\n) AS {CountAlias}";

        return Finish(wrapped, definition.Recompile);
    }

    /// <summary>
    /// The Discover shape, run to read <c>GetColumnSchema()</c> without ever reading a row.
    /// </summary>
    /// <remarks>
    /// <c>FETCH NEXT <b>1</b></c>, not 0: <c>FETCH NEXT 0 ROWS ONLY</c> is illegal in SQL Server ("the number of
    /// rows for a FETCH clause must be greater than zero"). Nothing is read from the reader regardless, so the
    /// row is never materialised to the client.
    /// </remarks>
    public static string ComposeDiscover(ReportDefinition definition) {
        string body = PrepareBody(definition.Sql);
        return Finish(Substitute(body, NullOrderBy, "OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY"), definition.Recompile);
    }

    /// <summary>
    /// Bracket-quotes an identifier, escaping <c>]</c> as <c>]]</c>.
    /// </summary>
    /// <remarks>
    /// Defence in depth. Column keys already come from a whitelist populated by discovery, so this should never
    /// have anything to do — but "should never" is doing a lot of work if someone hand-edits a definition row.
    /// </remarks>
    public static string QuoteIdentifier(string? identifier) {
        if (string.IsNullOrEmpty(identifier))
            throw new ReportValidationException("A column identifier cannot be empty.");
        if (identifier.Length > MaxIdentifierLength)
            throw new ReportValidationException(
                $"Column identifier '{identifier}' exceeds the {MaxIdentifierLength} character limit.");
        if (identifier.Contains('\0'))
            throw new ReportValidationException("A column identifier cannot contain a NUL character.");

        return $"[{identifier.Replace("]", "]]")}]";
    }

    private static void ValidateStatementShape(string? sql, string label, List<string> errors) {
        if (string.IsNullOrWhiteSpace(sql)) {
            errors.Add($"{label} is required.");
            return;
        }

        if (!ReportSqlLexer.StartsWithSelectOrWith(sql))
            errors.Add($"{label} must start with SELECT or WITH. Comments before it are fine.");

        if (ReportSqlLexer.HasStatementSeparator(sql))
            errors.Add($"{label} must be a single statement. A ';' is only allowed as one trailing terminator.");
    }

    /// <summary>
    /// The placeholder contract: each token exactly once, in code, and nothing else in <c>{{…}}</c>.
    /// </summary>
    /// <remarks>
    /// Rejecting unknown tokens is the whole point of this check — it is what catches a <c>{{OrderBy}}</c> casing
    /// typo that <c>string.Replace</c> would pass straight through to SQL Server.
    /// </remarks>
    private static void ValidateTokens(string sql, List<string> errors) {
        int orderByCount = 0;
        int pagingCount = 0;

        foreach (ReportSqlLexer.SqlPlaceholder placeholder in ReportSqlLexer.FindPlaceholders(sql)) {
            if (!placeholder.Terminated) {
                errors.Add($"Unterminated placeholder: the '{{{{' at position {placeholder.Start} has no matching '}}}}'.");
                continue;
            }

            if (placeholder.Name is not (OrderByTokenName or PagingTokenName)) {
                errors.Add($"Unknown placeholder '{{{{{placeholder.Name}}}}}'. Only '{{{{{OrderByTokenName}}}}}' and "
                           + $"'{{{{{PagingTokenName}}}}}' are supported, and they are case-sensitive.");
                continue;
            }

            if (!placeholder.InCodeRegion) {
                errors.Add($"Placeholder '{{{{{placeholder.Name}}}}}' must appear in the query itself, not inside a "
                           + "string literal, quoted identifier or comment.");
                continue;
            }

            if (placeholder.Name == OrderByTokenName) orderByCount++;
            else pagingCount++;
        }

        if (orderByCount != 1)
            errors.Add($"The report SQL must contain '{{{{{OrderByTokenName}}}}}' exactly once (found {orderByCount}). "
                       + "Add a tiebreaker after it, e.g. ORDER BY {{orderBy}}, T0.\"DocEntry\", so paging is stable.");
        if (pagingCount != 1)
            errors.Add($"The report SQL must contain '{{{{{PagingTokenName}}}}}' exactly once (found {pagingCount}).");
    }

    private static void ValidateNoTokens(string sql, string label, List<string> errors) {
        if (ReportSqlLexer.FindPlaceholders(sql).Count > 0)
            errors.Add($"{label} must not contain '{{{{…}}}}' placeholders — it is not ordered or paged.");
    }

    /// <summary>
    /// Validates the author's SQL and strips its trailing <c>;</c>, so both wraps have a clean statement to work
    /// with. Compose always re-validates: fail closed, on every path, including run time.
    /// </summary>
    private static string PrepareBody(string? sql) {
        ThrowIfInvalid(ValidateReportSql(sql));
        return ReportSqlLexer.StripTrailingSemicolon(sql!);
    }

    /// <summary>
    /// Splices the two tokens by index, which tolerates <c>{{ orderBy }}</c> spacing that a
    /// <c>string.Replace("{{orderBy}}")</c> would miss. Safe only because <see cref="ValidateTokens"/> has already
    /// proven each token appears exactly once, in code.
    /// </summary>
    private static string Substitute(string sql, string orderByReplacement, string pagingReplacement) {
        var builder = new StringBuilder(sql.Length + orderByReplacement.Length + pagingReplacement.Length);
        int cursor = 0;

        foreach (ReportSqlLexer.SqlPlaceholder placeholder in ReportSqlLexer.FindPlaceholders(sql)) {
            builder.Append(sql, cursor, placeholder.Start - cursor);
            builder.Append(placeholder.Name == OrderByTokenName ? orderByReplacement : pagingReplacement);
            cursor = placeholder.Start + placeholder.Length;
        }

        builder.Append(sql, cursor, sql.Length - cursor);
        return builder.ToString();
    }

    /// <summary>
    /// Appends <c>OPTION (RECOMPILE)</c> at <b>statement</b> level, which is the only level where it is legal.
    /// </summary>
    /// <remarks>
    /// This is why <c>Recompile</c> is a declarative flag and not a token an author writes: <c>OPTION</c> is
    /// illegal inside a derived table, so a hand-written one saves clean against the page shape and then blows up
    /// the first time anyone asks for a count. The leading newline guards against a trailing <c>--</c> comment
    /// swallowing the clause.
    /// </remarks>
    private static string Finish(string sql, bool recompile) => recompile ? $"{sql}\nOPTION (RECOMPILE)" : sql;

    private static string DirectionKeyword(ReportSortDirection direction) =>
        direction == ReportSortDirection.Desc ? "DESC" : "ASC";

    private static ReportColumn? FindColumn(ReportDefinition definition, string? key) =>
        definition.Columns.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));

    private static string RequireSortableColumnKey(ReportDefinition definition, string? key) {
        ReportColumn? column = FindColumn(definition, key);
        if (column is null) throw new ReportValidationException($"Unknown sort column '{key}'.");
        if (!column.Sortable) throw new ReportValidationException($"Column '{column.Key}' is not sortable.");
        return column.Key;
    }

    private static void ThrowIfInvalid(IReadOnlyList<string> errors) {
        if (errors.Count > 0) throw new ReportValidationException(errors);
    }
}
