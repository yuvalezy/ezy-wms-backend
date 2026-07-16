using Core.Enums;

namespace Core.DTOs.Reports;

/// <summary>
/// Describes one report column on the wire. Serves both directions:
/// <list type="bullet">
/// <item>as <b>discovery output</b> (the Test button) — the engine's suggested label/format/sortability
/// for each column the SQL actually produced, which the author then adjusts;</item>
/// <item>as <b>runtime metadata</b> — the stored <see cref="Core.Entities.ReportColumn"/> definition the
/// runner renders its table from.</item>
/// </list>
/// </summary>
public class ReportColumnDescriptor {
    /// <summary>
    /// The SQL output column name — also the row dictionary key in <see cref="ReportPageResult.Rows"/>
    /// and the sort key a client sends back. Guaranteed non-empty and unique within a report.
    /// </summary>
    public required string Key { get; set; }

    public string Label { get; set; } = string.Empty;

    public ReportColumnFormat Format { get; set; } = ReportColumnFormat.Text;

    /// <summary>
    /// <c>left</c>, <c>center</c> or <c>right</c>; null means let the client derive it from <see cref="Format"/>.
    /// </summary>
    public string? Align { get; set; }

    public int Decimals { get; set; } = 2;

    public bool Visible { get; set; } = true;

    /// <summary>
    /// False when SQL Server can't <c>ORDER BY</c> the underlying type (<c>text</c>, <c>ntext</c>,
    /// <c>image</c>, <c>xml</c>, <c>geography</c>, <c>geometry</c>) or the author opted out. The client must
    /// not offer a sort arrow for these, and the backend rejects them regardless.
    /// </summary>
    public bool Sortable { get; set; } = true;

    /// <summary>
    /// Whether the column can be NULL. A NULL cell renders as an em dash — never as 0, which would make the
    /// report lie about the data.
    /// </summary>
    public bool Nullable { get; set; } = true;

    /// <summary>
    /// Whether cells render as a hyperlink. Always <see cref="ReportColumnLinkType.None"/> on the
    /// <b>discovery</b> path — SQL Server's column schema cannot know an author's intent to link.
    /// </summary>
    public ReportColumnLinkType LinkType { get; set; } = ReportColumnLinkType.None;

    /// <summary>
    /// The link target with <c>{ColumnKey}</c> placeholders, or null. The client re-validates the
    /// composed URL rather than trusting this: a definition row can be edited straight in the database
    /// without ever passing through save-time validation.
    /// </summary>
    public string? LinkTemplate { get; set; }

    public int Order { get; set; }

    /// <summary>
    /// The underlying SQL type name (e.g. <c>nvarchar</c>, <c>datetime</c>, <c>char</c>), informational.
    /// Populated on the <b>discovery</b> path only so the author can see why a format was suggested —
    /// notably that SAP's Y/N flags are <c>char(1)</c> and so arrive as <see cref="ReportColumnFormat.Text"/>
    /// until flipped by hand. Null on the runtime path.
    /// </summary>
    public string? SqlTypeName { get; set; }
}
