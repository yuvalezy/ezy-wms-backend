using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.DTOs.Reports;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Services;

/// <summary>
/// Turns declared <see cref="ReportVariable"/>s plus submitted values into typed <see cref="SqlParameter"/>s.
/// <b>Pure</b>: no DB, no I/O, no <c>SystemDbContext</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the boundary where untrusted input meets the database. The report SQL is operator-trusted
/// (superuser-authored); variable <b>values</b> come from ordinary users and are never trusted. So values are
/// <b>always</b> a typed <see cref="SqlParameter"/>, never string-interpolated, and are parsed strictly and
/// <b>rejected, never loosely coerced</b>.
/// </para>
/// <para>
/// Two rules here look pedantic and are not:
/// </para>
/// <list type="bullet">
/// <item>
/// <b>Fixed sizes and explicit types.</b> An inferred <c>NVarChar</c> size varies the <c>sp_executesql</c>
/// declaration per input length — a fresh cached plan for every distinct length, and a violation of the
/// invariant that only values vary at run time. Same reason an omitted optional must be a <b>typed</b>
/// <c>DBNull</c>: <c>new SqlParameter(name, null)</c> changes the declaration.
/// </item>
/// <item>
/// <b>The <c>__</c> reserved prefix.</b> The obvious name regex happily matches <c>__limit</c>, so an admin
/// naming a variable <c>__limit</c> would silently hijack the engine's own paging parameter.
/// </item>
/// </list>
/// </remarks>
public static partial class ReportParameterBinder {
    /// <summary>The engine's own offset parameter. Its <c>__</c> prefix is reserved from author variables.</summary>
    public const string OffsetParameterName = "@__offset";

    /// <summary>The engine's own row-count parameter. Its <c>__</c> prefix is reserved from author variables.</summary>
    public const string LimitParameterName = "@__limit";

    /// <summary>Prefix reserved for engine-generated parameters and aliases. Author variables may not use it.</summary>
    public const string ReservedPrefix = "__";

    /// <summary>Fixed <c>NVarChar</c> size for every text-ish parameter. Fixed on purpose — see the class remarks.</summary>
    public const int TextParameterSize = 4000;

    /// <summary>Precision of a <c>Decimal</c> parameter; the scale comes from the variable's <c>Decimals</c>.</summary>
    public const byte DecimalPrecision = 19;

    /// <summary>Upper bound on a variable's declared <c>Decimals</c>, matching the DTO's <c>[Range(0, 10)]</c>.</summary>
    public const int MaxDecimals = 10;

    /// <summary>Default page-size ceiling.</summary>
    public const int DefaultMaxLimit = 200;

    /// <summary>Absolute page-size ceiling. Only the chunked export path should reach for this.</summary>
    public const int HardMaxLimit = 500;

    /// <summary>
    /// Offset ceiling. <c>OFFSET</c> is O(n): offset 500k rescans 500k rows on <b>every</b> page.
    /// </summary>
    public const int MaxOffset = 100_000;

    private static readonly JsonSerializerOptions StaticOptionsJson = new() { PropertyNameCaseInsensitive = true };

    /// <summary>ISO only, parsed exactly — no culture, no guessing.</summary>
    private static readonly string[] DateFormats = ["yyyy-MM-dd"];

    /// <summary>
    /// ISO only. A trailing <c>Z</c> is accepted and treated as the same wall-clock instant (SAP's datetime
    /// columns hold local wall-clock, not an instant). A numeric offset such as <c>+02:00</c> is rejected rather
    /// than shifted — guessing which wall clock the author meant is exactly the loose coercion this class exists
    /// to prevent.
    /// </summary>
    private static readonly string[] DateTimeFormats = [
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.f", "yyyy-MM-ddTHH:mm:ss.ff", "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.f", "yyyy-MM-dd HH:mm:ss.ff", "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm'Z'", "yyyy-MM-ddTHH:mm:ss'Z'", "yyyy-MM-ddTHH:mm:ss.fff'Z'"
    ];

    /// <summary>
    /// <c>{0,59}</c> rather than <c>{0,63}</c>: <c>DateRange</c> appends a <c>From</c>/<c>To</c> suffix and the
    /// result still has to be a legal parameter name.
    /// </summary>
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]{0,59}$", RegexOptions.CultureInvariant)]
    private static partial Regex VariableNameRegex();

    /// <summary>
    /// Save-time validation of the variable declarations themselves, independent of any submitted value.
    /// Returns every problem at once. Empty means it passed.
    /// </summary>
    public static IReadOnlyList<string> ValidateDeclarations(IEnumerable<ReportVariable> variables) {
        var errors = new List<string>();
        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ReportVariable variable in variables) {
            string name = variable.Name ?? string.Empty;

            if (!VariableNameRegex().IsMatch(name)) {
                errors.Add($"Variable name '{name}' is invalid. Names must match ^[A-Za-z_][A-Za-z0-9_]{{0,59}}$.");
                continue;
            }

            if (name.StartsWith(ReservedPrefix, StringComparison.Ordinal)) {
                errors.Add($"Variable name '{name}' is reserved: names must not start with '{ReservedPrefix}', which "
                           + $"the engine uses for its own {OffsetParameterName}/{LimitParameterName} parameters.");
                continue;
            }

            // Collision is checked on the *emitted* names, not the declared ones, so a DateRange named `Doc`
            // clashing with a Date named `DocFrom` is caught rather than shipped.
            foreach (string emittedName in EmittedNames(variable)) {
                if (!emitted.Add(emittedName))
                    errors.Add($"Duplicate parameter '@{emittedName}' from variable '{name}'. Names are compared "
                               + "case-insensitively because SqlCommand's parameter lookup is case-insensitive.");
            }

            ValidateDeclarationDetails(variable, errors);
        }

        return errors;
    }

    /// <summary>
    /// Binds a submitted request against the declarations. Throws with every error at once rather than one per
    /// round trip.
    /// </summary>
    /// <exception cref="ReportValidationException">A declaration is invalid, a required value is missing, or a value failed strict parsing.</exception>
    public static IReadOnlyList<SqlParameter> Bind(IEnumerable<ReportVariable> variables, ReportExecutionRequest request) {
        List<ReportVariable> declared = [.. variables.OrderBy(v => v.Order)];
        ThrowIfInvalid(ValidateDeclarations(declared));

        var errors = new List<string>();
        Dictionary<string, ReportVariableValue> submitted = NormalizeKeys(request.Variables, errors);
        var parameters = new List<SqlParameter>();

        foreach (ReportVariable variable in declared) {
            submitted.TryGetValue(variable.Name, out ReportVariableValue? value);
            BindVariable(variable, value, parameters, errors);
        }

        ThrowIfInvalid(errors);
        return parameters;
    }

    /// <summary>
    /// Binds every declared variable to a <b>typed</b> <c>DBNull</c> — the discovery and save-time validation path,
    /// where the shape must compile but no value exists yet.
    /// </summary>
    /// <remarks>
    /// Ignores <see cref="ReportVariable.Required"/> by design: discovery is not a run, and the author's
    /// <c>(@X IS NULL OR col = @X)</c> pattern means all-null is a perfectly valid shape to compile.
    /// </remarks>
    public static IReadOnlyList<SqlParameter> BindAllNull(IEnumerable<ReportVariable> variables) {
        List<ReportVariable> declared = [.. variables.OrderBy(v => v.Order)];
        ThrowIfInvalid(ValidateDeclarations(declared));

        var parameters = new List<SqlParameter>();
        foreach (ReportVariable variable in declared) {
            foreach (string name in EmittedNames(variable)) parameters.Add(NullParameter($"@{name}", variable));
        }

        return parameters;
    }

    /// <summary>Creates the typed <c>@__offset</c> parameter, validating the offset first.</summary>
    public static SqlParameter CreateOffsetParameter(int offset) =>
        new(OffsetParameterName, SqlDbType.Int) { Value = ValidateOffset(offset) };

    /// <summary>
    /// Creates the typed <c>@__limit</c> parameter. The caller passes the <b>fetch</b> count — the engine asks for
    /// <c>limit + 1</c> rows and trims the extra to derive <c>HasMore</c>.
    /// </summary>
    public static SqlParameter CreateLimitParameter(int fetchCount) =>
        new(LimitParameterName, SqlDbType.Int) { Value = fetchCount };

    /// <summary>
    /// Clamps a requested page size into <c>[1, maxLimit]</c>, falling back to the report's default page size.
    /// </summary>
    /// <param name="maxLimit">Ceiling, defaulting to <see cref="DefaultMaxLimit"/> and never above <see cref="HardMaxLimit"/>.</param>
    public static int ClampLimit(int? requested, int defaultPageSize, int maxLimit = DefaultMaxLimit) {
        if (maxLimit is < 1 or > HardMaxLimit)
            throw new ReportValidationException($"The page-size ceiling must be between 1 and {HardMaxLimit}.");

        int value = requested ?? defaultPageSize;
        return Math.Clamp(value, 1, maxLimit);
    }

    /// <summary>Rejects a negative offset and hard-caps it at <see cref="MaxOffset"/>.</summary>
    public static int ValidateOffset(int offset) {
        if (offset < 0) throw new ReportValidationException("Offset must not be negative.");
        if (offset > MaxOffset)
            throw new ReportValidationException(
                $"Offset must not exceed {MaxOffset:N0}. OFFSET is O(n), so a larger one rescans those rows on every page.");
        return offset;
    }

    /// <summary>
    /// The parameter names a variable actually emits: one, or — for <see cref="ReportVariableType.DateRange"/> —
    /// <c>{Name}From</c> and <c>{Name}To</c>. A date range is two parameters, not one.
    /// </summary>
    private static IEnumerable<string> EmittedNames(ReportVariable variable) =>
        variable.Type == ReportVariableType.DateRange
            ? [$"{variable.Name}From", $"{variable.Name}To"]
            : [variable.Name];

    private static void ValidateDeclarationDetails(ReportVariable variable, List<string> errors) {
        int errorsBefore = errors.Count;

        if (variable.Type == ReportVariableType.Decimal && variable.Decimals is < 0 or > MaxDecimals)
            errors.Add($"Variable '{variable.Name}' declares {variable.Decimals} decimals; it must be between 0 and {MaxDecimals}.");

        if (variable.AllowMultiple && variable.Type is not (ReportVariableType.SelectList or ReportVariableType.SqlLookup))
            errors.Add($"Variable '{variable.Name}' is {variable.Type} and cannot allow multiple values — only "
                       + "SelectList and SqlLookup can.");

        if (variable.Type == ReportVariableType.SelectList) {
            int before = errors.Count;
            List<ReportLookupOption> options = ParseStaticOptions(variable, errors);
            // Only complain about "no options" when the JSON itself parsed — otherwise it is the same mistake twice.
            if (errors.Count == before && options.Count == 0)
                errors.Add($"Variable '{variable.Name}' is a SelectList but declares no options.");
        }

        if (variable.Type == ReportVariableType.SqlLookup && string.IsNullOrWhiteSpace(variable.LookupSql))
            errors.Add($"Variable '{variable.Name}' is a SqlLookup but declares no lookup query.");

        // Only when the declaration itself is sound: a malformed SelectList would otherwise be reported twice,
        // once here and again as its default failing the membership check it can no longer perform.
        if (errors.Count == errorsBefore) ValidateDefaultValue(variable, errors);
    }

    /// <summary>
    /// Parses the author's <see cref="ReportVariable.DefaultValue"/> at save time, exactly as a run would.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The save-time and discovery paths bind a typed <c>DBNull</c> for every variable
    /// (<see cref="BindAllNull"/>), so they never look at the default — but <see cref="BindVariable"/> coalesces
    /// <c>submitted ?? DefaultValue</c> on <b>every</b> run. Without this check a default that fails strict
    /// parsing saves perfectly clean and then makes the report <b>permanently unrunnable</b>: the user cannot
    /// clear their way out of it, because clearing the filter is exactly what makes the bad default apply. The
    /// entity's own contract is that a default is "parsed with the same strictness as a submitted value" — this
    /// is where that promise is kept, and it belongs at save time, to the author who typed it.
    /// </para>
    /// <para>
    /// Deliberately reuses <see cref="BindVariable"/> rather than re-implementing the parse, so the check can
    /// never drift from the run-time path it is predicting — and covers the multi-select CSV and
    /// <see cref="ReportVariableType.DateRange"/>'s <c>from,to</c> defaults for free.
    /// </para>
    /// </remarks>
    private static void ValidateDefaultValue(ReportVariable variable, List<string> errors) {
        if (string.IsNullOrEmpty(variable.DefaultValue)) return;

        // Probed as optional: this validates the default's *parse*, not its presence. Requiredness is a
        // property of the run (was a value supplied?), and judging it here would reject an author who
        // legitimately pairs a required DateRange with a one-sided default.
        var probe = new ReportVariable {
            Name = variable.Name,
            Label = variable.Label,
            Type = variable.Type,
            Decimals = variable.Decimals,
            Required = false,
            AllowMultiple = variable.AllowMultiple,
            DefaultValue = variable.DefaultValue,
            StaticOptions = variable.StaticOptions,
            LookupSql = variable.LookupSql,
        };

        var defaultErrors = new List<string>();
        BindVariable(probe, null, [], defaultErrors);

        foreach (string error in defaultErrors)
            errors.Add($"Variable '{variable.Name}' has an invalid default value — {error}");
    }

    /// <summary>
    /// Rebuilds the submitted map with a case-insensitive comparer.
    /// </summary>
    /// <remarks>
    /// Not redundant with the DTO's initializer: <c>System.Text.Json</c> constructs a <b>fresh</b> dictionary when
    /// it deserializes into a settable property, silently discarding the <c>OrdinalIgnoreCase</c> comparer the DTO
    /// declares. Two request keys that differ only in case are rejected rather than last-wins, because to SQL
    /// Server they are the same parameter and picking one arbitrarily is a coin flip over which filter applies.
    /// </remarks>
    private static Dictionary<string, ReportVariableValue> NormalizeKeys(
        Dictionary<string, ReportVariableValue>? source,
        List<string> errors) {
        var map = new Dictionary<string, ReportVariableValue>(StringComparer.OrdinalIgnoreCase);
        if (source is null) return map;

        foreach (KeyValuePair<string, ReportVariableValue> entry in source) {
            if (entry.Value is null) continue;
            if (!map.TryAdd(entry.Key, entry.Value))
                errors.Add($"Variable '{entry.Key}' was submitted more than once; names are case-insensitive.");
        }

        return map;
    }

    private static void BindVariable(
        ReportVariable variable,
        ReportVariableValue? submitted,
        List<SqlParameter> parameters,
        List<string> errors) {
        if (variable.Type == ReportVariableType.DateRange) {
            BindDateRange(variable, submitted, parameters, errors);
            return;
        }

        if (variable.AllowMultiple) {
            BindMultiple(variable, submitted, parameters, errors);
            return;
        }

        string? raw = Coalesce(submitted?.Value, variable.DefaultValue);
        SqlParameter parameter = CreateTypedParameter($"@{variable.Name}", variable);

        if (string.IsNullOrEmpty(raw)) {
            if (variable.Required) errors.Add($"'{variable.Label}' is required.");
            // Typed first, value second — never `new SqlParameter(name, null)`.
            parameter.Value = DBNull.Value;
        } else {
            object? parsed = ParseSingle(variable, raw, errors);
            parameter.Value = parsed ?? DBNull.Value;
        }

        parameters.Add(parameter);
    }

    /// <summary>
    /// Binds a multi-select as one CSV parameter.
    /// </summary>
    /// <remarks>
    /// Deliberately does <b>not</b> route the CSV through the single-value parser. Each element has already been
    /// validated individually by <see cref="BuildCsv"/> — including <c>SelectList</c> membership — and the joined
    /// <c>"O,C"</c> is not itself a declared option, so parsing it as a single value would reject every valid
    /// multi-select.
    /// </remarks>
    private static void BindMultiple(
        ReportVariable variable,
        ReportVariableValue? submitted,
        List<SqlParameter> parameters,
        List<string> errors) {
        string? csv = BuildCsv(variable, submitted, errors);
        SqlParameter parameter = CreateTypedParameter($"@{variable.Name}", variable);

        if (string.IsNullOrEmpty(csv)) {
            if (variable.Required) errors.Add($"'{variable.Label}' is required.");
            parameter.Value = DBNull.Value;
        } else {
            parameter.Value = csv;
        }

        parameters.Add(parameter);
    }

    /// <summary>
    /// Binds a date range as <b>two</b> <c>Date</c> parameters.
    /// </summary>
    /// <remarks>
    /// An open-ended range (only one bound submitted) is allowed for an optional variable — it pairs with the
    /// author's <c>(@XFrom IS NULL OR col &gt;= @XFrom)</c> pattern. The <c>From &lt;= To</c> rule is only checked
    /// when both bounds are present, since there is nothing to compare otherwise.
    /// </remarks>
    private static void BindDateRange(
        ReportVariable variable,
        ReportVariableValue? submitted,
        List<SqlParameter> parameters,
        List<string> errors) {
        string? rawFrom = submitted?.From;
        string? rawTo = submitted?.To;

        if (string.IsNullOrEmpty(rawFrom) && string.IsNullOrEmpty(rawTo) && !string.IsNullOrEmpty(variable.DefaultValue)) {
            string[] parts = variable.DefaultValue.Split(',', 2);
            rawFrom = parts[0].Trim();
            rawTo = parts.Length > 1 ? parts[1].Trim() : null;
        }

        if (variable.Required && (string.IsNullOrEmpty(rawFrom) || string.IsNullOrEmpty(rawTo)))
            errors.Add($"'{variable.Label}' is required: both a from and a to date must be supplied.");

        DateTime? from = ParseDateBound(variable, rawFrom, "from", errors);
        DateTime? to = ParseDateBound(variable, rawTo, "to", errors);

        if (from.HasValue && to.HasValue && from.Value > to.Value)
            errors.Add($"'{variable.Label}': the from date must not be after the to date.");

        parameters.Add(DateParameter($"@{variable.Name}From", from));
        parameters.Add(DateParameter($"@{variable.Name}To", to));
    }

    private static DateTime? ParseDateBound(ReportVariable variable, string? raw, string bound, List<string> errors) {
        if (string.IsNullOrEmpty(raw)) return null;

        if (!DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed)) {
            errors.Add($"'{variable.Label}': '{raw}' is not a valid {bound} date. Use ISO format (yyyy-MM-dd).");
            return null;
        }

        return parsed.Date;
    }

    private static SqlParameter DateParameter(string name, DateTime? value) =>
        new(name, SqlDbType.Date) { Value = value.HasValue ? value.Value : DBNull.Value };

    /// <summary>
    /// Collapses a multi-select into <b>one</b> CSV string for a single <c>NVarChar(4000)</c> parameter, which the
    /// author unpacks with <c>STRING_SPLIT(@X, ',')</c>.
    /// </summary>
    /// <remarks>
    /// Generating N parameters from N submitted values would make the SQL <b>text</b> vary at run time — breaking
    /// the invariant and voiding save-time validation, since the shape space would no longer be finite. A value
    /// containing a comma is rejected: it would silently split into two on the SQL side.
    /// </remarks>
    private static string? BuildCsv(ReportVariable variable, ReportVariableValue? submitted, List<string> errors) {
        List<string>? values = submitted?.Values;

        if (values is null && !string.IsNullOrEmpty(submitted?.Value)) values = [submitted.Value];
        if (values is null && !string.IsNullOrEmpty(variable.DefaultValue))
            values = [.. variable.DefaultValue.Split(',').Select(v => v.Trim())];
        if (values is null || values.Count == 0) return null;

        List<ReportLookupOption> options = variable.Type == ReportVariableType.SelectList
            ? ParseStaticOptions(variable, errors)
            : [];

        var accepted = new List<string>(values.Count);
        foreach (string value in values) {
            if (string.IsNullOrEmpty(value)) {
                errors.Add($"'{variable.Label}': an empty value cannot be selected.");
                continue;
            }

            if (value.Contains(',', StringComparison.Ordinal)) {
                errors.Add($"'{variable.Label}': the value '{value}' contains a comma, which cannot be sent in a "
                           + "comma-separated multi-select.");
                continue;
            }

            if (variable.Type == ReportVariableType.SelectList && !IsDeclaredOption(options, value)) {
                errors.Add($"'{variable.Label}': '{value}' is not one of the declared options.");
                continue;
            }

            accepted.Add(value);
        }

        if (accepted.Count == 0) return null;

        string csv = string.Join(',', accepted);
        if (csv.Length > TextParameterSize) {
            errors.Add($"'{variable.Label}': too many values selected (the list exceeds {TextParameterSize} characters).");
            return null;
        }

        return csv;
    }

    private static object? ParseSingle(ReportVariable variable, string raw, List<string> errors) {
        switch (variable.Type) {
            case ReportVariableType.Text:
            case ReportVariableType.SqlLookup:
                // SqlLookup is type-coerced only: re-running the lookup to verify membership is a wasted round
                // trip, not a security control. A bogus value returns zero rows, which is correct.
                return RequireLength(variable, raw, errors);

            case ReportVariableType.SelectList:
                if (!IsDeclaredOption(ParseStaticOptions(variable, errors), raw)) {
                    errors.Add($"'{variable.Label}': '{raw}' is not one of the declared options.");
                    return null;
                }
                return RequireLength(variable, raw, errors);

            case ReportVariableType.Integer:
                if (!long.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long integer)) {
                    errors.Add($"'{variable.Label}': '{raw}' is not a whole number.");
                    return null;
                }
                return integer;

            case ReportVariableType.Decimal:
                const NumberStyles decimalStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
                if (!decimal.TryParse(raw, decimalStyles, CultureInfo.InvariantCulture, out decimal number)) {
                    errors.Add($"'{variable.Label}': '{raw}' is not a valid number.");
                    return null;
                }
                // Parsing as a decimal is not the same as fitting the Decimal(19, Decimals) it binds to.
                return RequirePrecisionAndScale(variable, raw, number, errors);

            case ReportVariableType.Date:
                if (!DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)) {
                    errors.Add($"'{variable.Label}': '{raw}' is not a valid date. Use ISO format (yyyy-MM-dd).");
                    return null;
                }
                return date.Date;

            case ReportVariableType.DateTime:
                if (!DateTime.TryParseExact(raw, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp)) {
                    errors.Add($"'{variable.Label}': '{raw}' is not a valid date and time. Use ISO format.");
                    return null;
                }
                return timestamp;

            case ReportVariableType.Boolean: {
                bool? flag = ParseStrictBoolean(variable, raw, errors);
                return flag is null ? null : (object)flag.Value;
            }

            case ReportVariableType.YesNo: {
                // SAP B1 stores its flags as 'Y'/'N' in char(1); a Bit parameter simply will not match.
                bool? yesNo = ParseStrictBoolean(variable, raw, errors);
                return yesNo is null ? null : yesNo.Value ? "Y" : "N";
            }

            default:
                errors.Add($"'{variable.Label}': unsupported variable type '{variable.Type}'.");
                return null;
        }
    }

    /// <summary>
    /// Accepts only the canonical JSON literals <c>true</c> and <c>false</c>, ordinal and lower-case.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>bool.TryParse</c>, which accepts <c>"True"</c>, <c>"TRUE"</c> and surrounding
    /// whitespace, and deliberately not <c>1</c>/<c>0</c>/<c>Y</c>/<c>yes</c>. Anything else is a confused client,
    /// and a filter that silently means the opposite of what the user picked is worse than an error.
    /// </remarks>
    private static bool? ParseStrictBoolean(ReportVariable variable, string raw, List<string> errors) {
        switch (raw) {
            case "true": return true;
            case "false": return false;
            default:
                errors.Add($"'{variable.Label}': '{raw}' is not a boolean. Only true and false are accepted.");
                return null;
        }
    }

    private static string? RequireLength(ReportVariable variable, string raw, List<string> errors) {
        if (raw.Length <= TextParameterSize) return raw;

        // Letting this through would silently truncate at 4000 and quietly filter on the wrong value.
        errors.Add($"'{variable.Label}': the value is longer than {TextParameterSize} characters.");
        return null;
    }

    /// <summary>
    /// Range- and scale-checks a parsed decimal against the <c>Decimal(19, Decimals)</c> it is about to be bound
    /// at. The text-side counterpart of <see cref="RequireLength"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>decimal.TryParse</c> succeeding only proves the text is <i>a</i> decimal, not that it survives the
    /// declared parameter. Without this check the value is not rejected, it is <b>coerced</b> — precisely what this
    /// class exists to prevent — and both failure modes land past the point where errors can still be aggregated:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <b>Excess scale is silently rounded.</b> A <c>Decimals = 2</c> variable given <c>10.005</c> binds happily
    /// here, then SqlClient rescales it to <c>10.01</c> at execute time. A <c>WHERE DocTotal &gt;= @MinTotal</c>
    /// report then filters on a bound the user never asked for and a row at <c>10.008</c> vanishes — no error, no
    /// warning, the report just misreports the data.
    /// </item>
    /// <item>
    /// <b>Excess precision throws.</b> Twenty integral digits parse fine as a <c>decimal</c> but blow up inside
    /// SqlClient with <c>SqlTruncateException</c> at execute time — an unhandled 500 from deep in the stack
    /// instead of the aggregated 400 every other bad value produces.
    /// </item>
    /// </list>
    /// <para>
    /// Trailing zeros are deliberately <b>not</b> rejected. <c>decimal</c> preserves them, but <c>10.500</c> and
    /// <c>10.50</c> are numerically equal, so the test is "does rounding to the declared scale change the
    /// <i>value</i>", not "how many digits did the user type" — rejecting a lossless <c>10.500</c> would be
    /// pedantry, not strictness.
    /// </para>
    /// </remarks>
    private static object? RequirePrecisionAndScale(ReportVariable variable, string raw, decimal number, List<string> errors) {
        byte scale = ResolveScale(variable);

        if (decimal.Round(number, scale) != number) {
            errors.Add($"'{variable.Label}': '{raw}' has more than {scale} decimal place(s) and would be rounded "
                       + $"to fit this filter, silently changing which rows match.");
            return null;
        }

        // Decimal(19, scale) holds at most (19 - scale) integral digits. Compared without Math.Abs, which
        // overflows on decimal.MinValue.
        decimal limit = Pow10(DecimalPrecision - scale);
        if (number >= limit || number <= -limit) {
            errors.Add($"'{variable.Label}': '{raw}' is too large for this filter, which holds at most "
                       + $"{DecimalPrecision - scale} digit(s) before the decimal point.");
            return null;
        }

        return number;
    }

    /// <summary>10 raised to <paramref name="exponent"/>. Only ever called with 9–19, which fits a decimal.</summary>
    private static decimal Pow10(int exponent) {
        decimal result = 1m;
        for (int i = 0; i < exponent; i++) result *= 10m;
        return result;
    }

    /// <summary>
    /// The scale a <c>Decimal</c> variable binds at. Single source of truth shared by
    /// <see cref="RequirePrecisionAndScale"/> and <see cref="CreateTypedParameter"/>, so the value is always
    /// checked against exactly the scale it is declared with — the two drifting apart is how the rounding bug
    /// gets back in.
    /// </summary>
    private static byte ResolveScale(ReportVariable variable) => (byte)Math.Clamp(variable.Decimals, 0, MaxDecimals);

    private static bool IsDeclaredOption(List<ReportLookupOption> options, string value) =>
        options.Any(o => string.Equals(o.Value, value, StringComparison.Ordinal));

    private static List<ReportLookupOption> ParseStaticOptions(ReportVariable variable, List<string> errors) {
        if (string.IsNullOrWhiteSpace(variable.StaticOptions)) return [];

        try {
            return JsonSerializer.Deserialize<List<ReportLookupOption>>(variable.StaticOptions, StaticOptionsJson) ?? [];
        } catch (JsonException ex) {
            errors.Add($"Variable '{variable.Name}' has invalid options JSON: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Builds the parameter with its <c>SqlDbType</c> and size/precision set but <b>no value</b>. Every caller sets
    /// the value afterwards, which is the only way a null keeps its declared type.
    /// </summary>
    private static SqlParameter CreateTypedParameter(string name, ReportVariable variable) => variable.Type switch {
        ReportVariableType.Text => new SqlParameter(name, SqlDbType.NVarChar) { Size = TextParameterSize },
        ReportVariableType.Integer => new SqlParameter(name, SqlDbType.BigInt),
        ReportVariableType.Decimal => new SqlParameter(name, SqlDbType.Decimal) {
            Precision = DecimalPrecision,
            Scale = ResolveScale(variable)
        },
        ReportVariableType.Date => new SqlParameter(name, SqlDbType.Date),
        ReportVariableType.DateTime => new SqlParameter(name, SqlDbType.DateTime2) { Scale = 3 },
        ReportVariableType.Boolean => new SqlParameter(name, SqlDbType.Bit),
        ReportVariableType.YesNo => new SqlParameter(name, SqlDbType.Char) { Size = 1 },
        ReportVariableType.SelectList or ReportVariableType.SqlLookup =>
            new SqlParameter(name, SqlDbType.NVarChar) { Size = TextParameterSize },
        ReportVariableType.DateRange => new SqlParameter(name, SqlDbType.Date),
        _ => throw new ReportValidationException($"Unsupported variable type '{variable.Type}'.")
    };

    private static SqlParameter NullParameter(string name, ReportVariable variable) {
        SqlParameter parameter = CreateTypedParameter(name, variable);
        parameter.Value = DBNull.Value;
        return parameter;
    }

    private static string? Coalesce(string? submitted, string? fallback) =>
        string.IsNullOrEmpty(submitted) ? fallback : submitted;

    private static void ThrowIfInvalid(IReadOnlyList<string> errors) {
        if (errors.Count > 0) throw new ReportValidationException(errors);
    }
}
