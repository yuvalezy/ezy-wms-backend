using Core.Exceptions;

namespace Infrastructure.Services;

/// <summary>
/// Comment- and string-aware scanner over author-written report SQL. <b>Pure</b>: no DB, no I/O, no
/// <c>SystemDbContext</c> — which is exactly what makes it CI-testable.
/// </summary>
/// <remarks>
/// <para>
/// Every question this class answers ("does it start with SELECT?", "is there a second statement?",
/// "where does the main SELECT begin?") is worthless if asked with <c>string.IndexOf</c>: a <c>;</c> inside
/// <c>'DROP SHIP'</c> is not a statement separator, and a <c>SELECT</c> inside <c>/* ... */</c> is not a query.
/// So the scanner skips, honouring SQL Server's own escape rules:
/// </para>
/// <list type="bullet">
/// <item><c>'…'</c> string literals, with <c>''</c> as an escaped quote;</item>
/// <item><c>"…"</c> quoted identifiers, with <c>""</c> as an escaped quote;</item>
/// <item><c>[…]</c> bracket identifiers, with <c>]]</c> as an escaped bracket;</item>
/// <item><c>--</c> line comments, to end of line;</item>
/// <item><c>/* … */</c> block comments, which in T-SQL <b>nest</b>.</item>
/// </list>
/// <para>
/// An unterminated literal or comment runs to the end of the input rather than throwing: SQL Server will
/// reject it at compile time with a far better message than anything invented here.
/// </para>
/// </remarks>
public static class ReportSqlLexer {
    /// <summary>
    /// The result of splitting a leading <c>WITH</c> clause off its main <c>SELECT</c>.
    /// </summary>
    /// <param name="CtePrefix">
    /// Everything up to (not including) the main <c>SELECT</c> — the <c>WITH x AS (…)</c> chain. Empty when
    /// the statement has no CTEs.
    /// </param>
    /// <param name="MainSelect">The main <c>SELECT</c> onward — what the count shape wraps in a derived table.</param>
    public readonly record struct CteSplit(string CtePrefix, string MainSelect) {
        public bool HasCtes => CtePrefix.Length > 0;
    }

    /// <summary>
    /// One <c>{{…}}</c> placeholder occurrence found in the SQL.
    /// </summary>
    /// <param name="Name">The trimmed text between the braces. Compared <b>case-sensitively</b> by the composer.</param>
    /// <param name="Start">Index of the opening <c>{</c>.</param>
    /// <param name="Length">Length of the whole <c>{{…}}</c> run, so callers can splice by index.</param>
    /// <param name="InCodeRegion">
    /// False when the placeholder sits inside a literal, quoted identifier or comment. Such an occurrence is a
    /// hard error, not something to skip: a naive <c>string.Replace</c> would happily expand a <c>{{paging}}</c>
    /// that the author buried in a string literal.
    /// </param>
    /// <param name="Terminated">False when <c>{{</c> has no matching <c>}}</c>.</param>
    public readonly record struct SqlPlaceholder(string Name, int Start, int Length, bool InCodeRegion, bool Terminated);

    /// <summary>
    /// The first keyword of the statement, skipping leading whitespace and comments, or null when the statement
    /// does not begin with a bare word (e.g. it opens with <c>(</c> or a literal).
    /// </summary>
    public static string? GetLeadingKeyword(string sql) {
        if (string.IsNullOrEmpty(sql)) return null;

        int i = SkipLeadingTrivia(sql, 0);
        int start = i;
        while (i < sql.Length && (char.IsLetter(sql[i]) || sql[i] == '_')) i++;

        return i == start ? null : sql[start..i];
    }

    /// <summary>
    /// Whether the statement starts with <c>SELECT</c> or <c>WITH</c> once leading comments are skipped.
    /// </summary>
    /// <remarks>
    /// Cheap, high signal, ~zero false positives — and deliberately the <i>only</i> keyword check here. A DML/DDL
    /// blocklist is defeated by <c>EXEC('DELETE…')</c> and <c>SELECT … INTO</c>, false-positives on a column named
    /// <c>Updated</c>, and — worst of all — manufactures the false confidence that leads people to skip the
    /// read-only SQL login, which is the guard with real teeth.
    /// </remarks>
    public static bool StartsWithSelectOrWith(string sql) {
        string? keyword = GetLeadingKeyword(sql);
        return keyword is not null
               && (keyword.Equals("SELECT", StringComparison.OrdinalIgnoreCase)
                   || keyword.Equals("WITH", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Whether the SQL contains a statement separator: a <c>;</c> in a code region that is not the single
    /// trailing terminator.
    /// </summary>
    /// <remarks>
    /// <c>SqlCommand</c> ships the report via <c>sp_executesql</c>, so a <c>;</c>-chained second statement inside
    /// <c>@stmt</c> executes perfectly happily. This turns "it's one statement" from an assumption into a checked
    /// property. One trailing <c>;</c> is idiomatic and tolerated (the composer strips it).
    /// </remarks>
    public static bool HasStatementSeparator(string sql) {
        if (string.IsNullOrEmpty(sql)) return false;

        bool[] code = BuildCodeMask(sql);
        int terminator = LastCodeNonWhitespaceIndex(sql, code);

        for (int i = 0; i < sql.Length; i++) {
            if (code[i] && sql[i] == ';' && i != terminator) return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a single trailing <c>;</c> (ignoring trailing whitespace and comments), leaving everything else —
    /// including a trailing comment — intact.
    /// </summary>
    /// <remarks>
    /// Mandatory before either wrap: <c>SELECT COUNT_BIG(*) FROM (SELECT …;) AS __c</c> and
    /// <c>SELECT …; OPTION (RECOMPILE)</c> are both syntax errors.
    /// </remarks>
    public static string StripTrailingSemicolon(string sql) {
        if (string.IsNullOrEmpty(sql)) return sql;

        bool[] code = BuildCodeMask(sql);
        int last = LastCodeNonWhitespaceIndex(sql, code);

        return last >= 0 && sql[last] == ';' ? sql.Remove(last, 1) : sql;
    }

    /// <summary>
    /// Splits a leading <c>WITH</c> chain off the main <c>SELECT</c>, so the count shape can hoist the CTEs
    /// instead of punting on them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>SELECT COUNT_BIG(*) FROM (WITH x AS (…) SELECT …) AS __c</c> is illegal; hoisting is legal and correct:
    /// <c>WITH x AS (…) SELECT COUNT_BIG(*) FROM (SELECT … ) AS __c</c>.
    /// </para>
    /// <para>
    /// The split is sound because <b>CTE bodies are always parenthesized</b>: the first <c>SELECT</c> at
    /// paren-depth 0 after a leading <c>WITH</c> is unambiguously the main select. That claim only holds with a
    /// real lexer behind it — the word "select" inside a CTE's string literal or comment must not count, which is
    /// precisely what the code mask handles.
    /// </para>
    /// </remarks>
    /// <exception cref="ReportValidationException">
    /// The statement does not start with <c>SELECT</c>/<c>WITH</c>, or starts with <c>WITH</c> but has no main
    /// select outside the CTE bodies.
    /// </exception>
    public static CteSplit SplitLeadingCtes(string sql) {
        string? keyword = GetLeadingKeyword(sql);

        if (keyword is null)
            throw new ReportValidationException("The SQL must start with SELECT or WITH.");
        if (keyword.Equals("SELECT", StringComparison.OrdinalIgnoreCase))
            return new CteSplit(string.Empty, sql);
        if (!keyword.Equals("WITH", StringComparison.OrdinalIgnoreCase))
            throw new ReportValidationException($"The SQL must start with SELECT or WITH, but starts with '{keyword}'.");

        bool[] code = BuildCodeMask(sql);
        int depth = 0;

        for (int i = SkipLeadingTrivia(sql, 0) + keyword.Length; i < sql.Length; i++) {
            if (!code[i]) continue;

            char c = sql[i];
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (depth != 0) continue;

            if ((c is 'S' or 's') && IsKeywordAt(sql, code, i, "SELECT")) return new CteSplit(sql[..i], sql[i..]);
        }

        throw new ReportValidationException(
            "The SQL starts with WITH but no main SELECT was found outside the CTE definitions.");
    }

    /// <summary>
    /// Every <c>{{…}}</c> occurrence in the SQL, in source order, each tagged with whether it lives in a code
    /// region. Returns occurrences rather than filtering them: the composer must be able to <b>reject</b> a
    /// placeholder found in a comment or literal, not silently ignore it.
    /// </summary>
    public static IReadOnlyList<SqlPlaceholder> FindPlaceholders(string sql) {
        var found = new List<SqlPlaceholder>();
        if (string.IsNullOrEmpty(sql)) return found;

        bool[] code = BuildCodeMask(sql);

        for (int i = 0; i + 1 < sql.Length; i++) {
            if (sql[i] != '{' || sql[i + 1] != '{') continue;

            int close = sql.IndexOf("}}", i + 2, StringComparison.Ordinal);
            if (close < 0) {
                found.Add(new SqlPlaceholder(sql[(i + 2)..].Trim(), i, sql.Length - i, code[i], Terminated: false));
                break;
            }

            found.Add(new SqlPlaceholder(sql[(i + 2)..close].Trim(), i, close + 2 - i, code[i], Terminated: true));
            i = close + 1;
        }

        return found;
    }

    /// <summary>
    /// Per-character map of which offsets are real code, i.e. not inside a literal, quoted identifier or comment.
    /// This single pass is what every public method above is built on.
    /// </summary>
    private static bool[] BuildCodeMask(string sql) {
        var code = new bool[sql.Length];
        int i = 0;

        while (i < sql.Length) {
            char c = sql[i];
            int end;

            if (c == '\'') end = SkipDelimited(sql, i, '\'');
            else if (c == '"') end = SkipDelimited(sql, i, '"');
            else if (c == '[') end = SkipDelimited(sql, i, ']');
            else if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-') end = SkipLineComment(sql, i);
            else if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*') end = SkipBlockComment(sql, i);
            else {
                code[i] = true;
                i++;
                continue;
            }

            // Every Skip* helper returns an index strictly greater than i, so this cannot loop forever.
            i = end;
        }

        return code;
    }

    private static int SkipLeadingTrivia(string sql, int start) {
        int i = start;

        while (i < sql.Length) {
            char c = sql[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-') { i = SkipLineComment(sql, i); continue; }
            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*') { i = SkipBlockComment(sql, i); continue; }
            break;
        }

        return i;
    }

    /// <summary>
    /// Skips a delimited run starting at <paramref name="start"/>, treating a doubled <paramref name="close"/> as
    /// an escape — which is the rule for all three of <c>'…'</c>, <c>"…"</c> and <c>[…]</c>.
    /// </summary>
    private static int SkipDelimited(string sql, int start, char close) {
        int i = start + 1;

        while (i < sql.Length) {
            if (sql[i] != close) { i++; continue; }
            if (i + 1 < sql.Length && sql[i + 1] == close) { i += 2; continue; }
            return i + 1;
        }

        return sql.Length;
    }

    /// <summary>Skips <c>--</c> to end of line, leaving the newline itself as code (it is only whitespace).</summary>
    private static int SkipLineComment(string sql, int start) {
        int i = start + 2;
        while (i < sql.Length && sql[i] != '\n') i++;
        return i;
    }

    /// <summary>Skips a <c>/* … */</c> comment. T-SQL nests these, so this tracks depth rather than stopping at the first <c>*/</c>.</summary>
    private static int SkipBlockComment(string sql, int start) {
        int depth = 0;
        int i = start;

        while (i < sql.Length) {
            if (sql[i] == '/' && i + 1 < sql.Length && sql[i + 1] == '*') { depth++; i += 2; continue; }
            if (sql[i] == '*' && i + 1 < sql.Length && sql[i + 1] == '/') {
                depth--;
                i += 2;
                if (depth == 0) return i;
                continue;
            }
            i++;
        }

        return sql.Length;
    }

    private static int LastCodeNonWhitespaceIndex(string sql, bool[] code) {
        for (int i = sql.Length - 1; i >= 0; i--) {
            if (code[i] && !char.IsWhiteSpace(sql[i])) return i;
        }
        return -1;
    }

    /// <summary>
    /// Whole-word, case-insensitive keyword match at <paramref name="index"/>, entirely within code regions.
    /// Whole-word matters: <c>SELECTED</c> and <c>x.select_id</c> are not the keyword.
    /// </summary>
    private static bool IsKeywordAt(string sql, bool[] code, int index, string keyword) {
        if (index + keyword.Length > sql.Length) return false;

        for (int k = 0; k < keyword.Length; k++) {
            if (!code[index + k]) return false;
            if (char.ToUpperInvariant(sql[index + k]) != keyword[k]) return false;
        }

        if (index > 0 && IsIdentifierChar(sql[index - 1])) return false;

        int after = index + keyword.Length;
        return after >= sql.Length || !IsIdentifierChar(sql[after]);
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';
}
