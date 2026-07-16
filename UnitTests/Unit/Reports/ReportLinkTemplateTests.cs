using Core.Enums;
using Infrastructure.Services;
using NUnit.Framework;

namespace UnitTests.Unit.Reports;

/// <summary>
/// Link-template rules. These decide what a report cell may become an <c>href</c> to, so they are
/// pure and covered here rather than only by a click in a browser.
///
/// <para>
/// The scheme cases are not about distrusting the author — report SQL is already operator-trusted.
/// They exist because fixing the scheme at save time is what guarantees a <b>row value</b> can never
/// choose it: the client only substitutes percent-encoded values into a template that already starts
/// with <c>/</c> or <c>https://</c>.
/// </para>
/// </summary>
[TestFixture]
public class ReportLinkTemplateTests {
    private static readonly string[] Columns = ["ItemCode", "WhsName", "OnHand"];

    private static List<string> Validate(ReportColumnLinkType type, string? template) {
        var errors = new List<string>();
        ReportLinkTemplate.Validate(type, template, "Code", Columns, errors);
        return errors;
    }

    // ------------------------------------------------------------------ valid

    [Test]
    public void Validate_InternalRouteWithPlaceholder_Passes() {
        Assert.That(Validate(ReportColumnLinkType.Internal, "/itemCheck/{ItemCode}"), Is.Empty);
    }

    [Test]
    public void Validate_ExternalHttpsWithPlaceholder_Passes() {
        Assert.That(Validate(ReportColumnLinkType.External, "https://www.google.com/search?q={WhsName}"), Is.Empty);
    }

    [Test]
    public void Validate_PlaceholderCasingDiffers_Passes() {
        // Column keys are matched case-insensitively everywhere else (SQL Server's default collation);
        // the link template must not be the one place that suddenly cares.
        Assert.That(Validate(ReportColumnLinkType.Internal, "/itemCheck/{itemcode}"), Is.Empty);
    }

    [Test]
    public void Validate_MultiplePlaceholders_Passes() {
        Assert.That(Validate(ReportColumnLinkType.Internal, "/binCheck/{WhsName}/{ItemCode}"), Is.Empty);
    }

    [Test]
    public void Validate_NoPlaceholders_Passes() {
        // A constant link is legitimate — every row pointing at the same dashboard.
        Assert.That(Validate(ReportColumnLinkType.External, "https://intranet/stock"), Is.Empty);
    }

    [Test]
    public void Validate_NoneIgnoresEverything_Passes() {
        // A template left behind after toggling the link off must not block the save.
        Assert.That(Validate(ReportColumnLinkType.None, "javascript:alert(1)"), Is.Empty);
    }

    // ---------------------------------------------------------------- missing

    [Test]
    public void Validate_LinkTypeWithoutTemplate_Fails() {
        Assert.That(Validate(ReportColumnLinkType.Internal, null), Has.Count.EqualTo(1));
        Assert.That(Validate(ReportColumnLinkType.External, "   "), Has.Count.EqualTo(1));
    }

    // ----------------------------------------------------------------- scheme

    [Test]
    public void Validate_InternalNotRootRelative_Fails() {
        Assert.That(Validate(ReportColumnLinkType.Internal, "itemCheck/{ItemCode}"), Has.Count.EqualTo(1));
    }

    [Test]
    public void Validate_InternalProtocolRelative_FailsAndSaysWhy() {
        // "//evil.com/x" reads as root-relative but leaves the app entirely. The single most likely
        // way an internal link becomes an external one by accident.
        List<string> errors = Validate(ReportColumnLinkType.Internal, "//evil.com/{ItemCode}");
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Does.Contain("external address"));
    }

    [Test]
    public void Validate_InternalWithScriptScheme_Fails() {
        Assert.That(Validate(ReportColumnLinkType.Internal, "javascript:alert(1)"), Is.Not.Empty);
    }

    [Test]
    public void Validate_ExternalWithScriptScheme_Fails() {
        Assert.That(Validate(ReportColumnLinkType.External, "javascript:alert(1)"), Is.Not.Empty);
        Assert.That(Validate(ReportColumnLinkType.External, "data:text/html,<script>alert(1)</script>"), Is.Not.Empty);
    }

    [Test]
    public void Validate_ExternalNonHttpScheme_Fails() {
        Assert.That(Validate(ReportColumnLinkType.External, "ftp://files/{ItemCode}"), Is.Not.Empty);
    }

    [Test]
    public void Validate_ExternalSchemeIsCaseInsensitive_Passes() {
        Assert.That(Validate(ReportColumnLinkType.External, "HTTPS://intranet/{ItemCode}"), Is.Empty);
    }

    // ----------------------------------------------------------- placeholders

    [Test]
    public void Validate_UnknownPlaceholder_FailsAndNamesIt() {
        List<string> errors = Validate(ReportColumnLinkType.Internal, "/itemCheck/{NoSuchColumn}");
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Does.Contain("NoSuchColumn"));
    }

    [Test]
    public void Validate_EmptyPlaceholder_Fails() {
        Assert.That(Validate(ReportColumnLinkType.Internal, "/itemCheck/{}"), Is.Not.Empty);
    }

    [Test]
    public void Validate_UnbalancedBraces_Fails() {
        Assert.That(Validate(ReportColumnLinkType.Internal, "/itemCheck/{ItemCode"), Is.Not.Empty);
        Assert.That(Validate(ReportColumnLinkType.Internal, "/itemCheck/ItemCode}"), Is.Not.Empty);
        Assert.That(Validate(ReportColumnLinkType.Internal, "/itemCheck/{{ItemCode}}"), Is.Not.Empty);
    }

    [Test]
    public void Validate_TooLong_Fails() {
        var template = "/itemCheck/" + new string('a', ReportLinkTemplate.MaxLength);
        Assert.That(Validate(ReportColumnLinkType.Internal, template), Is.Not.Empty);
    }

    // ------------------------------------------------------------ placeholders API

    [Test]
    public void Placeholders_ReturnsEveryReferenceInOrder() {
        Assert.That(ReportLinkTemplate.Placeholders("/a/{One}/b/{Two}?x={One}"),
            Is.EqualTo(new[] {"One", "Two", "One"}));
    }

    [Test]
    public void Placeholders_NoTemplate_ReturnsEmpty() {
        Assert.That(ReportLinkTemplate.Placeholders(null), Is.Empty);
        Assert.That(ReportLinkTemplate.Placeholders(""), Is.Empty);
    }

    // -------------------------------------------------------------- normalize

    [Test]
    public void Normalize_NoneDropsTemplate() {
        Assert.That(ReportLinkTemplate.Normalize(ReportColumnLinkType.None, "/itemCheck/{ItemCode}"), Is.Null);
    }

    [Test]
    public void Normalize_TrimsAndKeeps() {
        Assert.That(ReportLinkTemplate.Normalize(ReportColumnLinkType.Internal, "  /itemCheck/{ItemCode}  "),
            Is.EqualTo("/itemCheck/{ItemCode}"));
    }

    [Test]
    public void Normalize_BlankBecomesNull() {
        Assert.That(ReportLinkTemplate.Normalize(ReportColumnLinkType.Internal, "   "), Is.Null);
    }
}
