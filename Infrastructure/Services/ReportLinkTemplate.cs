using System.Text.RegularExpressions;
using Core.Enums;

namespace Infrastructure.Services;

/// <summary>
/// Validation for a report column's link template. <b>Pure</b> — no DB, no HTTP — so the rules that
/// decide what may become an <c>href</c> are covered by the unit suite CI actually runs.
///
/// <para>
/// A template is author-written text with <c>{ColumnKey}</c> placeholders resolved per row against
/// that row's other columns, e.g. <c>/itemCheck/{ItemCode}</c> or
/// <c>https://www.google.com/search?q={WhsName}</c>.
/// </para>
///
/// <para>
/// <b>Why the scheme rules are not paranoia about the author.</b> Report SQL is operator-trusted, and
/// so is this template — a superuser who wants to link somewhere hostile does not need a bug to do it.
/// What these rules actually buy is the guarantee that a <b>row value</b> can never decide the scheme.
/// The template fixes the scheme at save time and the client only ever substitutes
/// percent-encoded values into it, so a SAP column containing <c>javascript:alert(1)</c> lands as an
/// inert path segment rather than as executable script. Anchoring the scheme here is what makes that
/// hold; the client re-checks the composed URL anyway, because a definition row can be hand-edited in
/// the database and never pass through this method at all.
/// </para>
/// </summary>
public static class ReportLinkTemplate {
    /// <summary>
    /// One <c>{Key}</c> placeholder. Column keys are SQL output names, so anything but a brace is fair
    /// game inside — the key is checked against the declared columns rather than against a charset.
    /// </summary>
    private static readonly Regex PlaceholderPattern = new(@"\{([^{}]*)\}", RegexOptions.Compiled);

    /// <summary>Longest an author may make a template; mirrors <c>ReportColumn.LinkTemplate</c>'s column width.</summary>
    public const int MaxLength = 500;

    /// <summary>
    /// Every <c>{Key}</c> referenced by the template, in order, duplicates included.
    /// </summary>
    public static IReadOnlyList<string> Placeholders(string? template) =>
        string.IsNullOrEmpty(template)
            ? []
            : PlaceholderPattern.Matches(template).Select(m => m.Groups[1].Value).ToList();

    /// <summary>
    /// Validates one column's link configuration, appending human-readable errors.
    /// </summary>
    /// <param name="linkType">The column's declared link type.</param>
    /// <param name="template">The raw template as authored.</param>
    /// <param name="columnKey">The column being validated, for error messages.</param>
    /// <param name="declaredKeys">Every column key declared by the report, matched case-insensitively.</param>
    /// <param name="errors">Collector; nothing is added when the configuration is valid.</param>
    public static void Validate(
        ReportColumnLinkType linkType,
        string? template,
        string columnKey,
        IReadOnlyCollection<string> declaredKeys,
        List<string> errors) {
        if (linkType == ReportColumnLinkType.None) {
            // A template left behind on a None column is dropped rather than rejected: an author
            // toggling a link off mid-edit should not have to also clear the box to save.
            return;
        }

        var value = template?.Trim();
        if (string.IsNullOrEmpty(value)) {
            errors.Add($"Column '{columnKey}' is set to link but has no link template.");
            return;
        }

        if (value.Length > MaxLength) {
            errors.Add($"Column '{columnKey}' has a link template longer than {MaxLength} characters.");
            return;
        }

        // Checked before the placeholders: a stray brace makes every downstream reading of this
        // template a guess, and the substituting client would emit the brace verbatim into the URL.
        if (HasUnbalancedBraces(value)) {
            errors.Add($"Column '{columnKey}' has a link template with an unmatched '{{' or '}}': "
                     + "write each placeholder as {ColumnKey}.");
            return;
        }

        ValidateScheme(linkType, value, columnKey, errors);
        ValidatePlaceholders(value, columnKey, declaredKeys, errors);
    }

    private static void ValidateScheme(
        ReportColumnLinkType linkType, string template, string columnKey, List<string> errors) {
        if (linkType == ReportColumnLinkType.Internal) {
            if (!template.StartsWith('/')) {
                errors.Add($"Column '{columnKey}' has an internal link template that must start with '/', "
                         + $"e.g. /itemCheck/{{ItemCode}} — got '{template}'.");
            }
            else if (template.StartsWith("//", StringComparison.Ordinal)) {
                // "//evil.com/x" is protocol-relative: it looks root-relative but leaves the app.
                errors.Add($"Column '{columnKey}' has an internal link template starting with '//', which is an "
                         + "external address, not a route in this app. Use a single '/', or set the link to External.");
            }
            return;
        }

        if (!template.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
         && !template.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
            errors.Add($"Column '{columnKey}' has an external link template that must start with http:// or "
                     + $"https:// — got '{template}'.");
        }
    }

    private static void ValidatePlaceholders(
        string template, string columnKey, IReadOnlyCollection<string> declaredKeys, List<string> errors) {
        var known = new HashSet<string>(declaredKeys, StringComparer.OrdinalIgnoreCase);

        foreach (var placeholder in Placeholders(template)) {
            if (string.IsNullOrWhiteSpace(placeholder)) {
                errors.Add($"Column '{columnKey}' has an empty '{{}}' placeholder in its link template.");
            }
            else if (!known.Contains(placeholder.Trim())) {
                // A placeholder naming a column the SQL does not return can only ever resolve to
                // nothing, so the link would silently never render. Say so at save time.
                errors.Add($"Column '{columnKey}' links to '{{{placeholder}}}', which is not one of the report's "
                         + "columns. Placeholders must name a column the query returns — select it and mark it "
                         + "hidden if it should not show in the table.");
            }
        }
    }

    /// <summary>
    /// True when braces do not pair up strictly left-to-right — a nested or dangling brace.
    /// </summary>
    private static bool HasUnbalancedBraces(string template) {
        var open = false;
        foreach (var c in template) {
            switch (c) {
                case '{' when open:
                    return true;
                case '{':
                    open = true;
                    break;
                case '}' when !open:
                    return true;
                case '}':
                    open = false;
                    break;
            }
        }
        return open;
    }

    /// <summary>
    /// The template as it should be persisted: trimmed, and dropped entirely for
    /// <see cref="ReportColumnLinkType.None"/> so a row never carries a template it does not use.
    /// </summary>
    public static string? Normalize(ReportColumnLinkType linkType, string? template) =>
        linkType == ReportColumnLinkType.None || string.IsNullOrWhiteSpace(template) ? null : template.Trim();
}
