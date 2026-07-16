using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Infrastructure.Services;

namespace UnitTests.Unit.Reports;

/// <summary>
/// The composer owns the invariant: the SQL text is a pure function of (definition, sort key, sort direction,
/// mode) and never of a submitted value. These tests pin the three shapes and the rejections that protect it.
/// </summary>
[TestFixture]
public class ReportSqlComposerTests {
    private const string Body = "SELECT T0.\"DocNum\" FROM OINV T0 ORDER BY {{orderBy}}, T0.\"DocEntry\"\n{{paging}}";

    private static ReportDefinition Definition(
        string sql = Body,
        string? countSql = null,
        bool recompile = false,
        string? defaultSortKey = "DocNum",
        ReportSortDirection defaultDirection = ReportSortDirection.Asc,
        IEnumerable<ReportColumn>? columns = null) =>
        new() {
            Name = "Open Invoices",
            Slug = "open-invoices",
            Sql = sql,
            CountSql = countSql,
            Recompile = recompile,
            DefaultSortColumnKey = defaultSortKey,
            DefaultSortDirection = defaultDirection,
            Columns = columns?.ToList() ?? [Column("DocNum"), Column("DocDate", order: 1)]
        };

    private static ReportColumn Column(string key, bool sortable = true, int order = 0) =>
        new() { Key = key, Label = key, Sortable = sortable, Order = order };

    // ---------------------------------------------------------------- token validation

    [Test]
    public void ValidateReportSql_BothTokensPresentOnce_ReturnsNoErrors() {
        Assert.That(ReportSqlComposer.ValidateReportSql(Body), Is.Empty);
    }

    [Test]
    public void ValidateReportSql_MisCasedOrderByToken_IsRejected() {
        // The reason unknown tokens are rejected at all: string.Replace would pass {{OrderBy}} straight through
        // to SQL Server, which is a runtime explosion instead of a save-time message.
        IReadOnlyList<string> errors =
            ReportSqlComposer.ValidateReportSql("SELECT 1 ORDER BY {{OrderBy}}\n{{paging}}");

        Assert.That(errors, Is.Not.Empty);
        Assert.That(errors.Any(e => e.Contains("Unknown placeholder", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateReportSql_UnknownToken_IsRejected() {
        IReadOnlyList<string> errors =
            ReportSqlComposer.ValidateReportSql("SELECT 1 WHERE x = {{userId}} ORDER BY {{orderBy}}\n{{paging}}");

        Assert.That(errors.Any(e => e.Contains("{{userId}}", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateReportSql_MissingOrderByToken_IsRejected() {
        IReadOnlyList<string> errors = ReportSqlComposer.ValidateReportSql("SELECT 1\n{{paging}}");

        Assert.That(errors.Any(e => e.Contains("{{orderBy}}", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateReportSql_MissingPagingToken_IsRejected() {
        IReadOnlyList<string> errors = ReportSqlComposer.ValidateReportSql("SELECT 1 ORDER BY {{orderBy}}");

        Assert.That(errors.Any(e => e.Contains("{{paging}}", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateReportSql_DuplicatedPagingToken_IsRejected() {
        IReadOnlyList<string> errors =
            ReportSqlComposer.ValidateReportSql("SELECT 1 ORDER BY {{orderBy}}\n{{paging}} {{paging}}");

        Assert.That(errors.Any(e => e.Contains("exactly once", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateReportSql_TokenInsideStringLiteral_IsRejected() {
        IReadOnlyList<string> errors =
            ReportSqlComposer.ValidateReportSql("SELECT '{{paging}}' AS s ORDER BY {{orderBy}}\n{{paging}}");

        Assert.That(errors.Any(e => e.Contains("string literal", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateReportSql_NotSelectOrWith_IsRejected() {
        IReadOnlyList<string> errors =
            ReportSqlComposer.ValidateReportSql("DELETE FROM OINV ORDER BY {{orderBy}}\n{{paging}}");

        Assert.That(errors.Any(e => e.Contains("SELECT or WITH", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateReportSql_ChainedStatement_IsRejected() {
        IReadOnlyList<string> errors =
            ReportSqlComposer.ValidateReportSql("SELECT 1 ORDER BY {{orderBy}}\n{{paging}}; DELETE FROM OINV");

        Assert.That(errors.Any(e => e.Contains("single statement", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateReportSql_Empty_IsRejected() {
        Assert.That(ReportSqlComposer.ValidateReportSql("  "), Is.Not.Empty);
    }

    [Test]
    public void ValidateCountSql_ContainsPlaceholder_IsRejected() {
        IReadOnlyList<string> errors = ReportSqlComposer.ValidateCountSql("SELECT COUNT_BIG(*) FROM OINV {{paging}}");

        Assert.That(errors.Any(e => e.Contains("placeholders", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateLookupSql_PlainSelect_ReturnsNoErrors() {
        Assert.That(ReportSqlComposer.ValidateLookupSql("SELECT \"WhsCode\" AS Value, \"WhsName\" AS Label FROM OWHS"), Is.Empty);
    }

    [Test]
    public void ValidateLookupSql_ChainedStatement_IsRejected() {
        // Lookup SQL is admin SQL of the same class and gets identical treatment.
        Assert.That(ReportSqlComposer.ValidateLookupSql("SELECT 1 AS Value, 'x' AS Label; DROP TABLE t"), Is.Not.Empty);
    }

    // ---------------------------------------------------------------- page shape

    [Test]
    public void ComposePage_SubstitutesOrderByAndPaging() {
        var sort = new ReportSqlComposer.ResolvedSort("DocNum", ReportSortDirection.Desc);

        string sql = ReportSqlComposer.ComposePage(Definition(), sort);

        Assert.Multiple(() => {
            Assert.That(sql, Does.Contain("ORDER BY [DocNum] DESC, T0.\"DocEntry\""));
            Assert.That(sql, Does.Contain("OFFSET @__offset ROWS FETCH NEXT @__limit ROWS ONLY"));
            Assert.That(sql, Does.Not.Contain("{{"));
        });
    }

    [Test]
    public void ComposePage_AscendingSort_EmitsAsc() {
        string sql = ReportSqlComposer.ComposePage(
            Definition(), new ReportSqlComposer.ResolvedSort("DocNum", ReportSortDirection.Asc));

        Assert.That(sql, Does.Contain("[DocNum] ASC"));
    }

    [Test]
    public void ComposePage_SpacedTokens_AreStillSubstituted() {
        string sql = ReportSqlComposer.ComposePage(
            Definition("SELECT 1 ORDER BY {{ orderBy }}\n{{ paging }}"),
            new ReportSqlComposer.ResolvedSort("DocNum", ReportSortDirection.Asc));

        Assert.That(sql, Does.Not.Contain("{{"));
    }

    [Test]
    public void ComposePage_UnknownSortColumn_Throws() {
        // Defence in depth: even a ResolvedSort built by hand is re-checked against the whitelist.
        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.ComposePage(
            Definition(), new ReportSqlComposer.ResolvedSort("DROP TABLE", ReportSortDirection.Asc)));
    }

    [Test]
    public void ComposePage_NonSortableColumn_Throws() {
        ReportDefinition definition = Definition(columns: [Column("Notes", sortable: false)]);

        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.ComposePage(
            definition, new ReportSqlComposer.ResolvedSort("Notes", ReportSortDirection.Asc)));
    }

    [Test]
    public void ComposePage_TrailingSemicolon_IsStrippedBeforePaging() {
        string sql = ReportSqlComposer.ComposePage(
            Definition("SELECT 1 ORDER BY {{orderBy}}\n{{paging}};"),
            new ReportSqlComposer.ResolvedSort("DocNum", ReportSortDirection.Asc));

        Assert.That(sql, Does.Not.Contain(";"));
    }

    [Test]
    public void ComposePage_RecompileEnabled_AppendsOptionAtStatementLevel() {
        string sql = ReportSqlComposer.ComposePage(
            Definition(recompile: true), new ReportSqlComposer.ResolvedSort("DocNum", ReportSortDirection.Asc));

        Assert.That(sql.TrimEnd(), Does.EndWith("OPTION (RECOMPILE)"));
    }

    [Test]
    public void ComposePage_RecompileWithTrailingLineComment_KeepsOptionOnItsOwnLine() {
        // Without the newline the OPTION clause would be swallowed by the author's trailing comment.
        string sql = ReportSqlComposer.ComposePage(
            Definition("SELECT 1 ORDER BY {{orderBy}}\n{{paging}} -- end", recompile: true),
            new ReportSqlComposer.ResolvedSort("DocNum", ReportSortDirection.Asc));

        Assert.That(sql, Does.Contain("\nOPTION (RECOMPILE)"));
    }

    [Test]
    public void ComposePage_RecompileDisabled_OmitsOption() {
        string sql = ReportSqlComposer.ComposePage(
            Definition(), new ReportSqlComposer.ResolvedSort("DocNum", ReportSortDirection.Asc));

        Assert.That(sql, Does.Not.Contain("OPTION (RECOMPILE)"));
    }

    // ---------------------------------------------------------------- discover shape

    [Test]
    public void ComposeDiscover_FetchesOneRowNotZero() {
        // FETCH NEXT 0 ROWS ONLY is illegal in SQL Server ("the number of rows for a FETCH clause must be
        // greater than zero"). Nothing is ever read from the reader, so the row never reaches the client.
        string sql = ReportSqlComposer.ComposeDiscover(Definition());

        Assert.Multiple(() => {
            Assert.That(sql, Does.Contain("OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY"));
            Assert.That(sql, Does.Not.Contain("FETCH NEXT 0"));
        });
    }

    [Test]
    public void ComposeDiscover_UsesNullOrderBy() {
        // Chicken-and-egg: {{orderBy}} needs a column key, but discovery is what produces the keys.
        string sql = ReportSqlComposer.ComposeDiscover(Definition());

        Assert.Multiple(() => {
            Assert.That(sql, Does.Contain("ORDER BY (SELECT NULL)"));
            Assert.That(sql, Does.Not.Contain("@__offset"));
            Assert.That(sql, Does.Not.Contain("@__limit"));
        });
    }

    [Test]
    public void ComposeDiscover_NoSortableColumnsDeclared_StillComposes() {
        // Discovery must work on a brand-new report that has no columns yet — that is the point of the Test button.
        string sql = ReportSqlComposer.ComposeDiscover(Definition(defaultSortKey: null, columns: []));

        Assert.That(sql, Does.Contain("(SELECT NULL)"));
    }

    // ---------------------------------------------------------------- count shape

    [Test]
    public void ComposeCount_NoCtes_WrapsInCountBig() {
        string sql = ReportSqlComposer.ComposeCount(Definition());

        Assert.Multiple(() => {
            Assert.That(sql, Does.Contain("SELECT COUNT_BIG(*) FROM ("));
            Assert.That(sql, Does.Contain(") AS __c"));
            Assert.That(sql, Does.Contain("ORDER BY (SELECT NULL)"));
            Assert.That(sql, Does.Contain("OFFSET 0 ROWS"));
            Assert.That(sql, Does.Not.Contain("FETCH NEXT"));
        });
    }

    [Test]
    public void ComposeCount_UsesCountBigNotCount() {
        // COUNT throws past 2^31.
        Assert.That(ReportSqlComposer.ComposeCount(Definition()), Does.Not.Contain("COUNT(*)"));
    }

    [Test]
    public void ComposeCount_LeadingCte_HoistsItOutsideTheDerivedTable() {
        // SELECT COUNT_BIG(*) FROM (WITH x AS (...) SELECT ...) AS __c is illegal; hoisting is legal and correct.
        ReportDefinition definition =
            Definition("WITH x AS (SELECT 1 AS a) SELECT * FROM x ORDER BY {{orderBy}}\n{{paging}}");

        string sql = ReportSqlComposer.ComposeCount(definition);

        Assert.Multiple(() => {
            Assert.That(sql, Does.StartWith("WITH x AS (SELECT 1 AS a)"));
            Assert.That(sql.IndexOf("WITH x AS", StringComparison.Ordinal),
                Is.LessThan(sql.IndexOf("COUNT_BIG", StringComparison.Ordinal)),
                "the CTE must be hoisted above the count, not left inside the derived table");
            Assert.That(sql, Does.Contain(") AS __c"));
        });
    }

    [Test]
    public void ComposeCount_CteContainingSelectInsideAStringLiteral_HoistsTheWholeCte() {
        // The literal's "select" must not be mistaken for the main select, or the CTE gets cut in half.
        ReportDefinition definition = Definition(
            "WITH x AS (SELECT 'a select b' AS note FROM t) SELECT * FROM x ORDER BY {{orderBy}}\n{{paging}}");

        string sql = ReportSqlComposer.ComposeCount(definition);

        Assert.Multiple(() => {
            Assert.That(sql, Does.StartWith("WITH x AS (SELECT 'a select b' AS note FROM t)"));
            Assert.That(sql, Does.Contain("SELECT COUNT_BIG(*) FROM (\nSELECT * FROM x"));
        });
    }

    [Test]
    public void ComposeCount_CteContainingSelectInsideAComment_HoistsTheWholeCte() {
        ReportDefinition definition = Definition(
            "WITH x AS (\n-- SELECT everything\nSELECT 1 AS a\n) SELECT * FROM x ORDER BY {{orderBy}}\n{{paging}}");

        string sql = ReportSqlComposer.ComposeCount(definition);

        Assert.Multiple(() => {
            Assert.That(sql, Does.StartWith("WITH x AS (\n-- SELECT everything\nSELECT 1 AS a\n)"));
            Assert.That(sql, Does.Contain("SELECT COUNT_BIG(*) FROM (\nSELECT * FROM x"));
        });
    }

    [Test]
    public void ComposeCount_CteContainingSelectInsideANestedBlockComment_HoistsTheWholeCte() {
        ReportDefinition definition = Definition(
            "WITH x AS (/* outer /* SELECT inner */ still outer */ SELECT 1 AS a) SELECT * FROM x ORDER BY {{orderBy}}\n{{paging}}");

        string sql = ReportSqlComposer.ComposeCount(definition);

        Assert.That(sql, Does.Contain("SELECT COUNT_BIG(*) FROM (\nSELECT * FROM x"));
    }

    [Test]
    public void ComposeCount_MainSelectEndingInLineComment_KeepsTheWrapOutsideTheComment() {
        // The newline before ") AS __c" is load-bearing: on the same line it would be inside the comment.
        ReportDefinition definition = Definition("SELECT 1 ORDER BY {{orderBy}}\n{{paging}} -- trailing note");

        string sql = ReportSqlComposer.ComposeCount(definition);

        Assert.That(sql, Does.Contain("-- trailing note\n) AS __c"));
    }

    [Test]
    public void ComposeCount_RecompileEnabled_AppendsOptionOutsideTheDerivedTable() {
        // OPTION is illegal inside a derived table, which is exactly why Recompile is a flag and not a token.
        string sql = ReportSqlComposer.ComposeCount(Definition(recompile: true));

        Assert.Multiple(() => {
            Assert.That(sql.TrimEnd(), Does.EndWith("OPTION (RECOMPILE)"));
            Assert.That(sql.IndexOf(") AS __c", StringComparison.Ordinal),
                Is.LessThan(sql.IndexOf("OPTION (RECOMPILE)", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void ComposeCount_AuthorSuppliedCountSql_IsUsedVerbatimWithoutWrapping() {
        ReportDefinition definition = Definition(countSql: "SELECT COUNT_BIG(*) FROM OINV WHERE \"DocStatus\" = 'O'");

        string sql = ReportSqlComposer.ComposeCount(definition);

        Assert.Multiple(() => {
            Assert.That(sql, Is.EqualTo("SELECT COUNT_BIG(*) FROM OINV WHERE \"DocStatus\" = 'O'"));
            Assert.That(sql, Does.Not.Contain("__c"));
        });
    }

    [Test]
    public void ComposeCount_InvalidAuthorSuppliedCountSql_Throws() {
        ReportDefinition definition = Definition(countSql: "DELETE FROM OINV");

        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.ComposeCount(definition));
    }

    [Test]
    public void ComposeCount_InvalidTokens_ThrowsRatherThanEmittingBrokenSql() {
        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.ComposeCount(Definition("SELECT 1")));
    }

    // ---------------------------------------------------------------- sort resolution

    [Test]
    public void ResolveSort_KnownKey_EmitsTheStoredKeyNotTheRequestText() {
        // Case-insensitive match, then emit the *stored* key — the request's own text never reaches the SQL.
        ReportSqlComposer.ResolvedSort sort = ReportSqlComposer.ResolveSort(Definition(), "docnum", null);

        Assert.That(sort.ColumnKey, Is.EqualTo("DocNum"));
    }

    [Test]
    public void ResolveSort_UnknownKey_Throws() {
        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.ResolveSort(Definition(), "Bogus", null));
    }

    [Test]
    public void ResolveSort_NonSortableKey_Throws() {
        ReportDefinition definition = Definition(columns: [Column("DocNum"), Column("Notes", sortable: false)]);

        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.ResolveSort(definition, "Notes", null));
    }

    [Test]
    public void ResolveSort_NoKeyRequested_FallsBackToTheDefault() {
        ReportSqlComposer.ResolvedSort sort =
            ReportSqlComposer.ResolveSort(Definition(defaultSortKey: "DocDate"), null, null);

        Assert.That(sort.ColumnKey, Is.EqualTo("DocDate"));
    }

    [Test]
    public void ResolveSort_NoKeyAndNoDefault_FallsBackToFirstSortableColumn() {
        ReportDefinition definition = Definition(
            defaultSortKey: null,
            columns: [Column("Notes", sortable: false, order: 0), Column("DocNum", order: 1)]);

        Assert.That(ReportSqlComposer.ResolveSort(definition, null, null).ColumnKey, Is.EqualTo("DocNum"));
    }

    [Test]
    public void ResolveSort_DefaultKeyNotAmongColumns_Throws() {
        ReportDefinition definition = Definition(defaultSortKey: "Renamed");

        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.ResolveSort(definition, null, null));
    }

    [Test]
    public void ResolveSort_NoSortableColumnAtAll_Throws() {
        ReportDefinition definition =
            Definition(defaultSortKey: null, columns: [Column("Notes", sortable: false)]);

        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.ResolveSort(definition, null, null));
    }

    [Test]
    public void ResolveSort_NoDirectionRequested_FallsBackToTheDefaultDirection() {
        ReportSqlComposer.ResolvedSort sort = ReportSqlComposer.ResolveSort(
            Definition(defaultDirection: ReportSortDirection.Desc), "DocNum", null);

        Assert.That(sort.Direction, Is.EqualTo(ReportSortDirection.Desc));
    }

    [Test]
    public void ResolveSort_DirectionRequested_OverridesTheDefault() {
        ReportSqlComposer.ResolvedSort sort = ReportSqlComposer.ResolveSort(
            Definition(defaultDirection: ReportSortDirection.Desc), "DocNum", ReportSortDirection.Asc);

        Assert.That(sort.Direction, Is.EqualTo(ReportSortDirection.Asc));
    }

    // ---------------------------------------------------------------- identifier quoting

    [Test]
    public void QuoteIdentifier_PlainName_IsBracketQuoted() {
        Assert.That(ReportSqlComposer.QuoteIdentifier("DocNum"), Is.EqualTo("[DocNum]"));
    }

    [Test]
    public void QuoteIdentifier_NameContainingClosingBracket_EscapesIt() {
        // The bracket-escape is what stops a hand-edited definition row from breaking out of the identifier.
        Assert.That(ReportSqlComposer.QuoteIdentifier("a]b"), Is.EqualTo("[a]]b]"));
    }

    [Test]
    public void QuoteIdentifier_InjectionAttempt_StaysInsideTheBrackets() {
        Assert.That(ReportSqlComposer.QuoteIdentifier("x] ; DROP TABLE OINV --"),
            Is.EqualTo("[x]] ; DROP TABLE OINV --]"));
    }

    [Test]
    public void QuoteIdentifier_Empty_Throws() {
        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.QuoteIdentifier(""));
    }

    [Test]
    public void QuoteIdentifier_Null_Throws() {
        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.QuoteIdentifier(null));
    }

    [Test]
    public void QuoteIdentifier_LongerThan128_Throws() {
        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.QuoteIdentifier(new string('a', 129)));
    }

    [Test]
    public void QuoteIdentifier_ExactlyMaxLength_IsAccepted() {
        Assert.That(ReportSqlComposer.QuoteIdentifier(new string('a', 128)), Has.Length.EqualTo(130));
    }

    [Test]
    public void QuoteIdentifier_ContainsNul_Throws() {
        Assert.Throws<ReportValidationException>(() => ReportSqlComposer.QuoteIdentifier("a\0b"));
    }
}
