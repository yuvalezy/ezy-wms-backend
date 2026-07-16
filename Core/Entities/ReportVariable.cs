using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

/// <summary>
/// One declared filter variable of a <see cref="ReportDefinition"/>, referenced from the author's SQL
/// as <c>@{Name}</c>. Owned by the definition (cascade-deleted with it), not an aggregate root.
/// </summary>
[Table("ReportVariables")]
public class ReportVariable {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// The SQL parameter name without the <c>@</c>. Validated at save against
    /// <c>^[A-Za-z_][A-Za-z0-9_]{0,59}$</c> <b>plus an explicit rejection of the <c>__</c> prefix</b>
    /// (the engine's own <c>@__offset</c>/<c>@__limit</c> would otherwise be hijackable), and rejected
    /// as a duplicate case-insensitively (<c>SqlCommand.Parameters</c> lookup is case-insensitive).
    /// <para>
    /// Capped at 60 chars, not 63, to leave room for <see cref="ReportVariableType.DateRange"/>'s
    /// <c>From</c>/<c>To</c> suffix.
    /// </para>
    /// </summary>
    [Required]
    [MaxLength(60)]
    public required string Name { get; set; }

    /// <summary>
    /// Filter label shown to the user.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string Label { get; set; }

    public ReportVariableType Type { get; set; } = ReportVariableType.Text;

    /// <summary>
    /// Scale for <see cref="ReportVariableType.Decimal"/> — binds <c>Decimal(19, Decimals)</c>.
    /// </summary>
    public int Decimals { get; set; } = 2;

    /// <summary>
    /// When false, an omitted value binds a <b>typed</b> <c>DBNull.Value</c> (build the typed parameter
    /// first, then set its value — never <c>new SqlParameter(name, null)</c>, which changes the
    /// <c>sp_executesql</c> declaration), pairing with the author's <c>(@X IS NULL OR col = @X)</c> pattern.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Multi-select, for <see cref="ReportVariableType.SelectList"/> / <see cref="ReportVariableType.SqlLookup"/>.
    /// Binds <b>one</b> <c>NVarChar(4000)</c> CSV parameter — the author unpacks it with
    /// <c>STRING_SPLIT(@X, ',')</c>. N parameters from N submitted values would make the SQL text vary at
    /// run time, breaking the invariant and voiding save-time validation.
    /// </summary>
    public bool AllowMultiple { get; set; }

    /// <summary>
    /// Default value as an ISO/invariant string, parsed with the same strictness as a submitted value.
    /// For <see cref="AllowMultiple"/>, a CSV.
    /// </summary>
    [MaxLength(4000)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// JSON array of <c>{ "value": "...", "label": "..." }</c> for
    /// <see cref="ReportVariableType.SelectList"/>. A submitted value must match one of these ordinally.
    /// </summary>
    public string? StaticOptions { get; set; }

    /// <summary>
    /// Query backing a <see cref="ReportVariableType.SqlLookup"/> dropdown. Admin SQL of the same class as
    /// <see cref="ReportDefinition.Sql"/> and gets identical treatment (leading-keyword check, <c>;</c> check,
    /// rollback scope, timeout). Must return exactly two columns named <c>Value</c> and <c>Label</c>.
    /// <b>All</b> of the report's declared variables are bound to it — unreferenced parameters are free under
    /// <c>sp_executesql</c>, which is what makes cascading lookups (warehouse → bin) work for nothing.
    /// </summary>
    public string? LookupSql { get; set; }

    /// <summary>
    /// Display order in the filter bar, ascending.
    /// </summary>
    public int Order { get; set; }

    // Navigation property
    [ForeignKey("ReportDefinition")]
    public Guid ReportDefinitionId { get; set; }

    public ReportDefinition ReportDefinition { get; set; } = null!;
}
