namespace Core.Enums;

/// <summary>
/// Declared type of a report filter variable. Drives strict parsing of the submitted value and
/// the <c>SqlDbType</c> it binds to. Values are never string-interpolated into the SQL — they are
/// always typed <c>SqlParameter</c>s — and are rejected rather than loosely coerced.
/// Serialized as a string (member name is the wire contract).
/// </summary>
public enum ReportVariableType {
    /// <summary>
    /// Free text. Binds <c>NVarChar</c> with a <b>fixed</b> <c>Size = 4000</c>: an inferred size would vary the
    /// <c>sp_executesql</c> declaration per input length, producing a fresh cached plan per length.
    /// </summary>
    Text = 0,

    /// <summary>
    /// Whole number. Binds <c>BigInt</c>. Deliberately split from <see cref="Decimal"/>: binding a SAP
    /// <c>int</c> column against a <c>Decimal(19,6)</c> parameter forces an implicit conversion that can
    /// lose the index seek.
    /// </summary>
    Integer = 1,

    /// <summary>
    /// Fractional number. Binds <c>Decimal(19, Decimals)</c> — never <c>Float</c>.
    /// </summary>
    Decimal = 2,

    /// <summary>
    /// Date only. Binds <c>Date</c>. ISO input only, parsed exactly.
    /// </summary>
    Date = 3,

    /// <summary>
    /// Date and time. Binds <c>DateTime2(3)</c>. ISO input only, parsed exactly.
    /// </summary>
    DateTime = 4,

    /// <summary>
    /// Binds <c>Bit</c>. For genuine bit columns only — see <see cref="YesNo"/> for SAP's flag columns.
    /// </summary>
    Boolean = 5,

    /// <summary>
    /// Binds <c>Char(1)</c> holding <c>'Y'</c> or <c>'N'</c>. SAP B1 stores its flags this way; a
    /// <c>Bit</c> parameter simply will not match, which would force every author into a <c>CASE</c> expression.
    /// </summary>
    YesNo = 6,

    /// <summary>
    /// Dropdown over author-declared static options (<c>ReportVariable.StaticOptions</c>).
    /// Binds per the declared value type; the submitted value must be one of the declared options (ordinal match).
    /// </summary>
    SelectList = 7,

    /// <summary>
    /// Dropdown populated by the variable's <c>LookupSql</c> (must return exactly <c>Value</c> and <c>Label</c> columns).
    /// The submitted value is type-coerced only — the lookup is never re-run to verify membership; a bogus
    /// value simply returns zero rows, which is correct.
    /// </summary>
    SqlLookup = 8,

    /// <summary>
    /// A from/to date pair. This is <b>two</b> parameters, not one: it emits <c>@{Name}From</c> and
    /// <c>@{Name}To</c>, both <c>Date</c>, and requires From &lt;= To.
    /// </summary>
    DateRange = 9
}
