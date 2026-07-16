namespace Core.Enums;

/// <summary>
/// How a report column's raw SQL value is rendered by the client.
/// Serialized as a string (member name is the wire contract).
/// </summary>
public enum ReportColumnFormat {
    /// <summary>
    /// Rendered verbatim. Default for anything not confidently mapped at discovery.
    /// </summary>
    Text = 0,

    /// <summary>
    /// Numeric, grouped, honouring the column's <c>Decimals</c>.
    /// </summary>
    Number = 1,

    /// <summary>
    /// Numeric with the company currency symbol.
    /// </summary>
    Currency = 2,

    /// <summary>
    /// Numeric rendered as a percentage. The stored value is used as-is (not multiplied by 100).
    /// </summary>
    Percent = 3,

    /// <summary>
    /// Date only, no time component.
    /// </summary>
    Date = 4,

    /// <summary>
    /// Date and time.
    /// </summary>
    DateTime = 5,

    /// <summary>
    /// Time of day only.
    /// </summary>
    Time = 6,

    /// <summary>
    /// True/false. Note SAP B1 stores Y/N in <c>char(1)</c> columns — those discover as
    /// <see cref="Text"/> and the author flips them to <see cref="Boolean"/> if desired.
    /// </summary>
    Boolean = 7,

    /// <summary>
    /// Binary payload; rendered as a size/placeholder rather than raw bytes.
    /// </summary>
    Binary = 8
}
