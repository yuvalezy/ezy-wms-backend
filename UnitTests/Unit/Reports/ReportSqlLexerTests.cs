using Core.Exceptions;
using Infrastructure.Services;

namespace UnitTests.Unit.Reports;

/// <summary>
/// The lexer is the foundation of the report security model: every check above it (leading keyword, single
/// statement, CTE hoist) is only as good as its ability to tell code from a string literal or a comment.
/// </summary>
[TestFixture]
public class ReportSqlLexerTests {
    // ---------------------------------------------------------------- leading keyword

    [Test]
    public void StartsWithSelectOrWith_PlainSelect_ReturnsTrue() {
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("SELECT 1"), Is.True);
    }

    [Test]
    public void StartsWithSelectOrWith_LowerCaseAndLeadingWhitespace_ReturnsTrue() {
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("\n\t  select 1"), Is.True);
    }

    [Test]
    public void StartsWithSelectOrWith_LeadingLineComment_SkipsItAndReturnsTrue() {
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("-- open invoices report\nSELECT 1"), Is.True);
    }

    [Test]
    public void StartsWithSelectOrWith_LeadingNestedBlockComment_SkipsItAndReturnsTrue() {
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("/* outer /* inner */ still outer */ SELECT 1"), Is.True);
    }

    [Test]
    public void StartsWithSelectOrWith_Delete_ReturnsFalse() {
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("DELETE FROM OINV"), Is.False);
    }

    [Test]
    public void StartsWithSelectOrWith_CommentedOutDeleteThenSelect_ReturnsTrue() {
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("/* DELETE FROM OINV */ SELECT 1"), Is.True);
    }

    [Test]
    public void StartsWithSelectOrWith_SelectHiddenInsideBlockComment_ReturnsFalse() {
        // The only SELECT here is commented out, so the real statement is the EXEC.
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("/* SELECT 1 */ EXEC sp_who"), Is.False);
    }

    [Test]
    public void StartsWithSelectOrWith_LeadingStringLiteral_ReturnsFalse() {
        // A literal is not trivia: the scanner must stop at it rather than skip ahead to the SELECT.
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("'x' SELECT 1"), Is.False);
    }

    [Test]
    public void StartsWithSelectOrWith_OpenParen_ReturnsFalse() {
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("(SELECT 1)"), Is.False);
    }

    [Test]
    public void StartsWithSelectOrWith_SelectedIdentifier_ReturnsFalse() {
        // Whole-word only: SELECTED is not SELECT.
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("SELECTED FROM x"), Is.False);
    }

    [Test]
    public void StartsWithSelectOrWith_UnterminatedBlockComment_ReturnsFalse() {
        Assert.That(ReportSqlLexer.StartsWithSelectOrWith("/* never closed SELECT 1"), Is.False);
    }

    [Test]
    public void GetLeadingKeyword_With_ReturnsWith() {
        Assert.That(ReportSqlLexer.GetLeadingKeyword("  WITH x AS (SELECT 1) SELECT * FROM x"), Is.EqualTo("WITH"));
    }

    // ---------------------------------------------------------------- statement separator

    [Test]
    public void HasStatementSeparator_SingleStatement_ReturnsFalse() {
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 1"), Is.False);
    }

    [Test]
    public void HasStatementSeparator_OneTrailingSemicolon_ReturnsFalse() {
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 1;"), Is.False);
    }

    [Test]
    public void HasStatementSeparator_TrailingSemicolonThenCommentAndWhitespace_ReturnsFalse() {
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 1; -- done\n  "), Is.False);
    }

    [Test]
    public void HasStatementSeparator_ChainedStatements_ReturnsTrue() {
        // sp_executesql runs whatever is in @stmt, so a chained DELETE would execute.
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 1; DELETE FROM OINV"), Is.True);
    }

    [Test]
    public void HasStatementSeparator_DoubledTrailingSemicolon_ReturnsTrue() {
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 1;;"), Is.True);
    }

    [Test]
    public void HasStatementSeparator_SemicolonInsideStringLiteral_ReturnsFalse() {
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 'a;b' AS x"), Is.False);
    }

    [Test]
    public void HasStatementSeparator_SemicolonInsideLineComment_ReturnsFalse() {
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 1 -- ; not a statement\n"), Is.False);
    }

    [Test]
    public void HasStatementSeparator_SemicolonInsideNestedBlockComment_ReturnsFalse() {
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 1 /* a /* ; */ b */ , 2"), Is.False);
    }

    [Test]
    public void HasStatementSeparator_SemicolonInsideBracketIdentifier_ReturnsFalse() {
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT [odd;name] FROM t"), Is.False);
    }

    [Test]
    public void HasStatementSeparator_SemicolonAfterEscapedQuote_ReturnsTrue() {
        // '' is an escaped quote, so the literal ends at the final quote and the ; that follows is real.
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 'it''s' ; DELETE FROM OINV"), Is.True);
    }

    [Test]
    public void HasStatementSeparator_SemicolonAfterEscapedBracket_ReturnsTrue() {
        // ]] is an escaped bracket, so [a]]b] is one identifier and the ; after it is real.
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT [a]]b] FROM t; DROP TABLE t"), Is.True);
    }

    [Test]
    public void HasStatementSeparator_SemicolonBetweenNestedBlockCommentCloses_ReturnsFalse() {
        // T-SQL nests block comments, so the first */ closes only the INNER one and this ; is still inside the
        // outer comment. A scanner that stopped at the first */ would see a real ; here and reject a valid
        // report. This case is what makes the depth counter observable — a ; *after* the outer close is caught
        // either way, so it would not pin the behaviour.
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 1 /* /* */ ; */ , 2"), Is.False);
    }

    [Test]
    public void HasStatementSeparator_SemicolonAfterNestedBlockCommentCloses_ReturnsTrue() {
        // The mirror image: once the outer comment really is closed, the ; that follows is a real separator.
        Assert.That(ReportSqlLexer.HasStatementSeparator("SELECT 1 /* /* */ x */ ; DELETE FROM t"), Is.True);
    }

    // ---------------------------------------------------------------- trailing semicolon

    [Test]
    public void StripTrailingSemicolon_TrailingSemicolon_RemovesIt() {
        Assert.That(ReportSqlLexer.StripTrailingSemicolon("SELECT 1;"), Is.EqualTo("SELECT 1"));
    }

    [Test]
    public void StripTrailingSemicolon_SemicolonThenTrailingComment_RemovesOnlyTheSemicolon() {
        Assert.That(ReportSqlLexer.StripTrailingSemicolon("SELECT 1; -- done"), Is.EqualTo("SELECT 1 -- done"));
    }

    [Test]
    public void StripTrailingSemicolon_NoSemicolon_ReturnsInputUnchanged() {
        Assert.That(ReportSqlLexer.StripTrailingSemicolon("SELECT 1"), Is.EqualTo("SELECT 1"));
    }

    [Test]
    public void StripTrailingSemicolon_SemicolonInsideLiteralOnly_ReturnsInputUnchanged() {
        Assert.That(ReportSqlLexer.StripTrailingSemicolon("SELECT 'a;'"), Is.EqualTo("SELECT 'a;'"));
    }

    // ---------------------------------------------------------------- CTE split

    [Test]
    public void SplitLeadingCtes_NoCtes_ReturnsWholeStatementAsMainSelect() {
        ReportSqlLexer.CteSplit split = ReportSqlLexer.SplitLeadingCtes("SELECT * FROM OINV");

        Assert.Multiple(() => {
            Assert.That(split.HasCtes, Is.False);
            Assert.That(split.CtePrefix, Is.Empty);
            Assert.That(split.MainSelect, Is.EqualTo("SELECT * FROM OINV"));
        });
    }

    [Test]
    public void SplitLeadingCtes_SingleCte_SplitsAtDepthZeroSelect() {
        ReportSqlLexer.CteSplit split = ReportSqlLexer.SplitLeadingCtes("WITH x AS (SELECT 1 AS a) SELECT * FROM x");

        Assert.Multiple(() => {
            Assert.That(split.HasCtes, Is.True);
            Assert.That(split.CtePrefix, Is.EqualTo("WITH x AS (SELECT 1 AS a) "));
            Assert.That(split.MainSelect, Is.EqualTo("SELECT * FROM x"));
        });
    }

    [Test]
    public void SplitLeadingCtes_MultipleCtes_SplitsAtTheFinalDepthZeroSelect() {
        ReportSqlLexer.CteSplit split =
            ReportSqlLexer.SplitLeadingCtes("WITH a AS (SELECT 1 AS n), b AS (SELECT 2 AS n) SELECT * FROM a JOIN b ON 1=1");

        Assert.Multiple(() => {
            Assert.That(split.CtePrefix, Is.EqualTo("WITH a AS (SELECT 1 AS n), b AS (SELECT 2 AS n) "));
            Assert.That(split.MainSelect, Is.EqualTo("SELECT * FROM a JOIN b ON 1=1"));
        });
    }

    [Test]
    public void SplitLeadingCtes_NestedParensInCteBody_SplitsAtTheMainSelect() {
        ReportSqlLexer.CteSplit split =
            ReportSqlLexer.SplitLeadingCtes("WITH x AS (SELECT (SELECT MAX(n) FROM t) AS a FROM u) SELECT * FROM x");

        Assert.That(split.MainSelect, Is.EqualTo("SELECT * FROM x"));
    }

    [Test]
    public void SplitLeadingCtes_CteWithColumnList_SplitsAtTheMainSelect() {
        ReportSqlLexer.CteSplit split = ReportSqlLexer.SplitLeadingCtes("WITH x (a, b) AS (SELECT 1, 2) SELECT * FROM x");

        Assert.That(split.MainSelect, Is.EqualTo("SELECT * FROM x"));
    }

    [Test]
    public void SplitLeadingCtes_SelectInsideCteStringLiteral_IsNotMistakenForTheMainSelect() {
        // The word "select" here is data, not code. A naive IndexOf("SELECT") would cut the statement in half
        // and produce SQL that either fails to compile or, worse, counts the wrong thing.
        const string sql = "WITH x AS (SELECT 'a select b' AS note FROM t) SELECT * FROM x";
        ReportSqlLexer.CteSplit split = ReportSqlLexer.SplitLeadingCtes(sql);

        Assert.Multiple(() => {
            Assert.That(split.CtePrefix, Is.EqualTo("WITH x AS (SELECT 'a select b' AS note FROM t) "));
            Assert.That(split.MainSelect, Is.EqualTo("SELECT * FROM x"));
        });
    }

    [Test]
    public void SplitLeadingCtes_SelectInsideCteLineComment_IsNotMistakenForTheMainSelect() {
        const string sql = "WITH x AS (\n-- SELECT everything\nSELECT 1 AS a\n) SELECT * FROM x";
        ReportSqlLexer.CteSplit split = ReportSqlLexer.SplitLeadingCtes(sql);

        Assert.That(split.MainSelect, Is.EqualTo("SELECT * FROM x"));
    }

    [Test]
    public void SplitLeadingCtes_SelectInsideCteBlockComment_IsNotMistakenForTheMainSelect() {
        const string sql = "WITH x AS (/* SELECT everything */ SELECT 1 AS a) SELECT * FROM x";
        ReportSqlLexer.CteSplit split = ReportSqlLexer.SplitLeadingCtes(sql);

        Assert.That(split.MainSelect, Is.EqualTo("SELECT * FROM x"));
    }

    [Test]
    public void SplitLeadingCtes_CommentBetweenCteAndMainSelect_KeepsCommentInThePrefix() {
        const string sql = "WITH x AS (SELECT 1 AS a) -- now the real one\nSELECT * FROM x";
        ReportSqlLexer.CteSplit split = ReportSqlLexer.SplitLeadingCtes(sql);

        Assert.Multiple(() => {
            Assert.That(split.CtePrefix, Is.EqualTo("WITH x AS (SELECT 1 AS a) -- now the real one\n"));
            Assert.That(split.MainSelect, Is.EqualTo("SELECT * FROM x"));
        });
    }

    [Test]
    public void SplitLeadingCtes_WithNoMainSelect_Throws() {
        Assert.Throws<ReportValidationException>(() => ReportSqlLexer.SplitLeadingCtes("WITH x AS (SELECT 1)"));
    }

    [Test]
    public void SplitLeadingCtes_NotSelectOrWith_Throws() {
        Assert.Throws<ReportValidationException>(() => ReportSqlLexer.SplitLeadingCtes("DELETE FROM OINV"));
    }

    // ---------------------------------------------------------------- placeholders

    [Test]
    public void FindPlaceholders_BothTokensInCode_ReportsBothAsCodeRegion() {
        const string sql = "SELECT 1 ORDER BY {{orderBy}} {{paging}}";
        IReadOnlyList<ReportSqlLexer.SqlPlaceholder> found = ReportSqlLexer.FindPlaceholders(sql);

        Assert.Multiple(() => {
            Assert.That(found, Has.Count.EqualTo(2));
            Assert.That(found[0].Name, Is.EqualTo("orderBy"));
            Assert.That(found[0].InCodeRegion, Is.True);
            Assert.That(found[0].Terminated, Is.True);
            Assert.That(found[1].Name, Is.EqualTo("paging"));
            Assert.That(found[1].InCodeRegion, Is.True);
        });
    }

    [Test]
    public void FindPlaceholders_TokenSpliceIsExact() {
        const string sql = "A {{orderBy}} B";
        ReportSqlLexer.SqlPlaceholder placeholder = ReportSqlLexer.FindPlaceholders(sql)[0];

        // Start/Length must delimit the whole {{...}} run so callers can splice by index.
        Assert.That(sql.Substring(placeholder.Start, placeholder.Length), Is.EqualTo("{{orderBy}}"));
    }

    [Test]
    public void FindPlaceholders_TokenInsideComment_IsFlaggedAsNotCode() {
        IReadOnlyList<ReportSqlLexer.SqlPlaceholder> found =
            ReportSqlLexer.FindPlaceholders("SELECT 1 -- {{paging}} was here\n");

        Assert.Multiple(() => {
            Assert.That(found, Has.Count.EqualTo(1));
            Assert.That(found[0].InCodeRegion, Is.False);
        });
    }

    [Test]
    public void FindPlaceholders_TokenInsideStringLiteral_IsFlaggedAsNotCode() {
        IReadOnlyList<ReportSqlLexer.SqlPlaceholder> found =
            ReportSqlLexer.FindPlaceholders("SELECT '{{paging}}' AS s");

        Assert.That(found[0].InCodeRegion, Is.False);
    }

    [Test]
    public void FindPlaceholders_UnterminatedToken_IsFlaggedAsUnterminated() {
        IReadOnlyList<ReportSqlLexer.SqlPlaceholder> found = ReportSqlLexer.FindPlaceholders("SELECT 1 {{orderBy");

        Assert.Multiple(() => {
            Assert.That(found, Has.Count.EqualTo(1));
            Assert.That(found[0].Terminated, Is.False);
        });
    }

    [Test]
    public void FindPlaceholders_SpacedToken_TrimsTheName() {
        Assert.That(ReportSqlLexer.FindPlaceholders("{{ orderBy }}")[0].Name, Is.EqualTo("orderBy"));
    }

    [Test]
    public void FindPlaceholders_NoTokens_ReturnsEmpty() {
        Assert.That(ReportSqlLexer.FindPlaceholders("SELECT 1"), Is.Empty);
    }
}
