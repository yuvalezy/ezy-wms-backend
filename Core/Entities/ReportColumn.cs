using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

/// <summary>
/// One declared output column of a <see cref="ReportDefinition"/>. Owned by the definition
/// (cascade-deleted with it), not an aggregate root.
/// </summary>
[Table("ReportColumns")]
public class ReportColumn {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// The output column name as produced by the SQL, captured at discovery. Discovery enforces that
    /// these are non-empty and unique across the result set.
    /// <para>
    /// This is also the sort whitelist entry: a request's sort key is matched case-insensitively against
    /// it and then <b>this stored value</b> is what gets emitted into <c>ORDER BY</c> — never the request's
    /// own text — bracket-quoted with <c>]</c> → <c>]]</c>.
    /// </para>
    /// </summary>
    [Required]
    [MaxLength(128)]
    public required string Key { get; set; }

    /// <summary>
    /// Header text shown to the user.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string Label { get; set; }

    public ReportColumnFormat Format { get; set; } = ReportColumnFormat.Text;

    /// <summary>
    /// Cell alignment: <c>left</c>, <c>center</c> or <c>right</c>. Null lets the client derive it
    /// from <see cref="Format"/> (numerics right, everything else left).
    /// </summary>
    [MaxLength(10)]
    public string? Align { get; set; }

    /// <summary>
    /// Decimal places for numeric formats. Only meaningful for
    /// <see cref="ReportColumnFormat.Number"/> / <see cref="ReportColumnFormat.Currency"/> /
    /// <see cref="ReportColumnFormat.Percent"/>.
    /// </summary>
    public int Decimals { get; set; } = 2;

    /// <summary>
    /// Hidden columns are still selected by the SQL (and still sortable/exportable targets) but are
    /// not rendered in the table.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// False for types SQL Server refuses to <c>ORDER BY</c> (<c>text</c>, <c>ntext</c>, <c>image</c>,
    /// <c>xml</c>, <c>geography</c>, <c>geometry</c>) — marked at discovery — or when the author opts out.
    /// A request sorting by a non-sortable column is rejected.
    /// </summary>
    public bool Sortable { get; set; } = true;

    /// <summary>
    /// Whether the underlying column can be NULL. A NULL cell must render as an em dash, never as 0.
    /// </summary>
    public bool Nullable { get; set; } = true;

    /// <summary>
    /// Whether this column's cells render as a hyperlink, and of which kind. Default
    /// <see cref="ReportColumnLinkType.None"/> — plain text.
    /// </summary>
    public ReportColumnLinkType LinkType { get; set; } = ReportColumnLinkType.None;

    /// <summary>
    /// Where the cell links to, with <c>{ColumnKey}</c> placeholders resolved per row against that
    /// row's own columns — e.g. <c>/itemCheck/{ItemCode}</c>, or
    /// <c>https://www.google.com/search?q={WhsName}</c>. Null when <see cref="LinkType"/> is
    /// <see cref="ReportColumnLinkType.None"/>.
    /// <para>
    /// Validated by <c>ReportLinkTemplate</c>: internal templates are root-relative, external ones are
    /// absolute http/https, and every placeholder must name a declared column. The scheme is fixed
    /// <b>here</b>, at save time, and row values are only ever percent-encoded into it — which is what
    /// stops a SAP column containing <c>javascript:…</c> from deciding where a cell points.
    /// </para>
    /// </summary>
    [MaxLength(500)]
    public string? LinkTemplate { get; set; }

    /// <summary>
    /// Display order, ascending.
    /// </summary>
    public int Order { get; set; }

    // Navigation property
    [ForeignKey("ReportDefinition")]
    public Guid ReportDefinitionId { get; set; }

    public ReportDefinition ReportDefinition { get; set; } = null!;
}
