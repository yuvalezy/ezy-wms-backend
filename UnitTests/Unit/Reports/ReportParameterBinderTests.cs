using System.Data;
using Core.DTOs.Reports;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Infrastructure.Services;
using Microsoft.Data.SqlClient;

namespace UnitTests.Unit.Reports;

/// <summary>
/// The binder is where untrusted values meet the database. Everything here is about two properties: values are
/// always typed parameters (never text), and they are rejected rather than loosely coerced.
/// </summary>
[TestFixture]
public class ReportParameterBinderTests {
    private static ReportVariable Variable(
        string name,
        ReportVariableType type = ReportVariableType.Text,
        bool required = false,
        bool allowMultiple = false,
        int decimals = 2,
        string? defaultValue = null,
        string? staticOptions = null,
        string? lookupSql = null) =>
        new() {
            Name = name,
            Label = name,
            Type = type,
            Required = required,
            AllowMultiple = allowMultiple,
            Decimals = decimals,
            DefaultValue = defaultValue,
            StaticOptions = staticOptions,
            LookupSql = lookupSql
        };

    private static ReportExecutionRequest Request(string name, ReportVariableValue value) =>
        new() { Variables = new Dictionary<string, ReportVariableValue> { [name] = value } };

    private static ReportExecutionRequest Request(string name, string? value) =>
        Request(name, new ReportVariableValue { Value = value });

    private static SqlParameter Single(ReportVariable variable, ReportExecutionRequest request) =>
        ReportParameterBinder.Bind([variable], request).Single();

    // ---------------------------------------------------------------- name validation

    [Test]
    public void ValidateDeclarations_ValidName_ReturnsNoErrors() {
        Assert.That(ReportParameterBinder.ValidateDeclarations([Variable("FromDate")]), Is.Empty);
    }

    [Test]
    public void ValidateDeclarations_LeadingUnderscoreName_IsAccepted() {
        // A single underscore is legal; only the doubled reserved prefix is not.
        Assert.That(ReportParameterBinder.ValidateDeclarations([Variable("_private")]), Is.Empty);
    }

    [Test]
    public void ValidateDeclarations_ReservedPrefixLimit_IsRejected() {
        // The naive regex matches __limit, so an admin could otherwise hijack the engine's own paging parameter.
        IReadOnlyList<string> errors = ReportParameterBinder.ValidateDeclarations([Variable("__limit")]);

        Assert.That(errors, Is.Not.Empty);
        Assert.That(errors.Any(e => e.Contains("reserved", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateDeclarations_ReservedPrefixOffset_IsRejected() {
        Assert.That(ReportParameterBinder.ValidateDeclarations([Variable("__offset")]), Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_ReservedPrefixAnyName_IsRejected() {
        Assert.That(ReportParameterBinder.ValidateDeclarations([Variable("__anything")]), Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_NameStartingWithDigit_IsRejected() {
        Assert.That(ReportParameterBinder.ValidateDeclarations([Variable("1st")]), Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_NameWithSpace_IsRejected() {
        Assert.That(ReportParameterBinder.ValidateDeclarations([Variable("From Date")]), Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_NameWithInjectionAttempt_IsRejected() {
        Assert.That(ReportParameterBinder.ValidateDeclarations([Variable("x; DROP TABLE t")]), Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_Name60Chars_IsAccepted() {
        Assert.That(ReportParameterBinder.ValidateDeclarations([Variable(new string('a', 60))]), Is.Empty);
    }

    [Test]
    public void ValidateDeclarations_Name61Chars_IsRejected() {
        // 60, not 64: DateRange still has to append From/To and stay a legal parameter name.
        Assert.That(ReportParameterBinder.ValidateDeclarations([Variable(new string('a', 61))]), Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_DuplicateNamesDifferingOnlyInCase_IsRejected() {
        // SqlCommand's parameter lookup is case-insensitive, so @foo and @Foo genuinely collide.
        IReadOnlyList<string> errors =
            ReportParameterBinder.ValidateDeclarations([Variable("foo"), Variable("Foo")]);

        Assert.That(errors, Is.Not.Empty);
        Assert.That(errors.Any(e => e.Contains("Duplicate", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ValidateDeclarations_DateRangeSuffixCollidingWithAnotherVariable_IsRejected() {
        // A DateRange named Doc emits @DocFrom/@DocTo, which collides with a Date literally named DocFrom.
        IReadOnlyList<string> errors = ReportParameterBinder.ValidateDeclarations([
            Variable("Doc", ReportVariableType.DateRange),
            Variable("DocFrom", ReportVariableType.Date)
        ]);

        Assert.That(errors, Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_DecimalsAboveMax_IsRejected() {
        Assert.That(
            ReportParameterBinder.ValidateDeclarations([Variable("x", ReportVariableType.Decimal, decimals: 11)]),
            Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_AllowMultipleOnText_IsRejected() {
        Assert.That(
            ReportParameterBinder.ValidateDeclarations([Variable("x", ReportVariableType.Text, allowMultiple: true)]),
            Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_SelectListWithoutOptions_IsRejected() {
        Assert.That(
            ReportParameterBinder.ValidateDeclarations([Variable("x", ReportVariableType.SelectList)]),
            Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_SelectListWithInvalidOptionsJson_IsRejected() {
        Assert.That(
            ReportParameterBinder.ValidateDeclarations([
                Variable("x", ReportVariableType.SelectList, staticOptions: "not json")
            ]),
            Is.Not.Empty);
    }

    [Test]
    public void ValidateDeclarations_SqlLookupWithoutQuery_IsRejected() {
        Assert.That(
            ReportParameterBinder.ValidateDeclarations([Variable("x", ReportVariableType.SqlLookup)]),
            Is.Not.Empty);
    }

    // ---------------------------------------------------------------- type mapping

    [Test]
    public void Bind_Text_BindsNVarCharWithFixedSize4000() {
        // Fixed, not inferred: an inferred size varies the sp_executesql declaration per input length, which
        // bloats the plan cache and breaks the "only values vary" invariant.
        SqlParameter parameter = Single(Variable("Name"), Request("Name", "short"));

        Assert.Multiple(() => {
            Assert.That(parameter.ParameterName, Is.EqualTo("@Name"));
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.NVarChar));
            Assert.That(parameter.Size, Is.EqualTo(4000));
            Assert.That(parameter.Value, Is.EqualTo("short"));
        });
    }

    [Test]
    public void Bind_TextOfDifferentLengths_KeepsTheSameDeclaredSize() {
        SqlParameter shortValue = Single(Variable("Name"), Request("Name", "a"));
        SqlParameter longValue = Single(Variable("Name"), Request("Name", new string('a', 3000)));

        Assert.That(shortValue.Size, Is.EqualTo(longValue.Size));
    }

    [Test]
    public void Bind_TextLongerThan4000_IsRejected() {
        // Letting it through would silently truncate and quietly filter on the wrong value.
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("Name"), Request("Name", new string('a', 4001))));
    }

    [Test]
    public void Bind_Integer_BindsBigInt() {
        // Split from Decimal on purpose: binding a SAP int column against Decimal(19,6) forces an implicit
        // conversion that can lose the index seek.
        SqlParameter parameter = Single(Variable("DocEntry", ReportVariableType.Integer), Request("DocEntry", "42"));

        Assert.Multiple(() => {
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.BigInt));
            Assert.That(parameter.Value, Is.EqualTo(42L));
        });
    }

    [Test]
    public void Bind_IntegerNegative_IsAccepted() {
        Assert.That(Single(Variable("N", ReportVariableType.Integer), Request("N", "-7")).Value, Is.EqualTo(-7L));
    }

    [Test]
    public void Bind_IntegerWithDecimalPoint_IsRejected() {
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("N", ReportVariableType.Integer), Request("N", "1.5")));
    }

    [Test]
    public void Bind_IntegerNonNumeric_IsRejected() {
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("N", ReportVariableType.Integer), Request("N", "abc")));
    }

    [Test]
    public void Bind_Decimal_BindsDecimalWithPrecision19AndDeclaredScale() {
        SqlParameter parameter = Single(
            Variable("Total", ReportVariableType.Decimal, decimals: 6), Request("Total", "12.345"));

        Assert.Multiple(() => {
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.Decimal));
            Assert.That(parameter.Precision, Is.EqualTo((byte)19));
            Assert.That(parameter.Scale, Is.EqualTo((byte)6));
            Assert.That(parameter.Value, Is.EqualTo(12.345m));
        });
    }

    [Test]
    public void Bind_DecimalWithThousandsSeparator_IsRejected() {
        // Strict parsing: "1,234" is ambiguous across cultures and is not silently reinterpreted.
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("Total", ReportVariableType.Decimal), Request("Total", "1,234")));
    }

    [Test]
    public void Bind_Date_BindsSqlDate() {
        SqlParameter parameter = Single(Variable("From", ReportVariableType.Date), Request("From", "2026-07-16"));

        Assert.Multiple(() => {
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.Date));
            Assert.That(parameter.Value, Is.EqualTo(new DateTime(2026, 7, 16)));
        });
    }

    [Test]
    public void Bind_DateInNonIsoFormat_IsRejected() {
        // TryParseExact, ISO only — 07/16/2026 vs 16/07/2026 is a silent off-by-months waiting to happen.
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("From", ReportVariableType.Date), Request("From", "16/07/2026")));
    }

    [Test]
    public void Bind_DateTime_BindsDateTime2WithScale3() {
        SqlParameter parameter = Single(
            Variable("At", ReportVariableType.DateTime), Request("At", "2026-07-16T10:30:00"));

        Assert.Multiple(() => {
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.DateTime2));
            Assert.That(parameter.Scale, Is.EqualTo((byte)3));
            Assert.That(parameter.Value, Is.EqualTo(new DateTime(2026, 7, 16, 10, 30, 0)));
        });
    }

    [Test]
    public void Bind_DateTimeWithTrailingZ_IsAcceptedAsTheSameWallClock() {
        // The axios layer serialises Date objects with a trailing Z; SAP's columns hold wall-clock, so no shift.
        SqlParameter parameter = Single(
            Variable("At", ReportVariableType.DateTime), Request("At", "2026-07-16T10:30:00.000Z"));

        Assert.That(parameter.Value, Is.EqualTo(new DateTime(2026, 7, 16, 10, 30, 0)));
    }

    [Test]
    public void Bind_DateTimeWithNumericOffset_IsRejected() {
        // Which wall clock did the author mean? Guessing is exactly the loose coercion this class forbids.
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("At", ReportVariableType.DateTime), Request("At", "2026-07-16T10:30:00+02:00")));
    }

    [Test]
    public void Bind_Boolean_BindsBit() {
        SqlParameter parameter = Single(Variable("Open", ReportVariableType.Boolean), Request("Open", "true"));

        Assert.Multiple(() => {
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.Bit));
            Assert.That(parameter.Value, Is.EqualTo(true));
        });
    }

    [Test]
    public void Bind_BooleanFalse_BindsFalse() {
        Assert.That(Single(Variable("Open", ReportVariableType.Boolean), Request("Open", "false")).Value, Is.EqualTo(false));
    }

    [Test]
    public void Bind_BooleanCapitalisedTrue_IsRejected() {
        // Deliberately not bool.TryParse, which would accept "True"/"TRUE"/" true ". Only the canonical JSON
        // literal spelling is accepted.
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("Open", ReportVariableType.Boolean), Request("Open", "True")));
    }

    [Test]
    public void Bind_BooleanNumericOne_IsRejected() {
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("Open", ReportVariableType.Boolean), Request("Open", "1")));
    }

    [Test]
    public void Bind_BooleanYesLiteral_IsRejected() {
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("Open", ReportVariableType.Boolean), Request("Open", "Y")));
    }

    [Test]
    public void Bind_YesNoTrue_BindsCharYNotBit() {
        // SAP B1 stores its flags as 'Y'/'N' in char(1); a Bit parameter simply will not match, which would
        // force every author into a CASE expression.
        SqlParameter parameter = Single(Variable("Active", ReportVariableType.YesNo), Request("Active", "true"));

        Assert.Multiple(() => {
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.Char));
            Assert.That(parameter.Size, Is.EqualTo(1));
            Assert.That(parameter.Value, Is.EqualTo("Y"));
        });
    }

    [Test]
    public void Bind_YesNoFalse_BindsCharN() {
        Assert.That(Single(Variable("Active", ReportVariableType.YesNo), Request("Active", "false")).Value, Is.EqualTo("N"));
    }

    [Test]
    public void Bind_YesNoLiteralY_IsRejected() {
        // The declared input domain for YesNo is a boolean; 'Y' is the storage encoding, not the input.
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("Active", ReportVariableType.YesNo), Request("Active", "Y")));
    }

    // ---------------------------------------------------------------- omitted / optional / required

    [Test]
    public void Bind_OmittedOptionalText_BindsTypedDbNullNotUntypedNull() {
        // The type must survive the null: `new SqlParameter(name, null)` changes the sp_executesql declaration
        // and breaks the invariant.
        SqlParameter parameter = Single(Variable("Name"), new ReportExecutionRequest());

        Assert.Multiple(() => {
            Assert.That(parameter.Value, Is.EqualTo(DBNull.Value));
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.NVarChar));
            Assert.That(parameter.Size, Is.EqualTo(4000));
        });
    }

    [Test]
    public void Bind_OmittedOptionalDecimal_KeepsPrecisionAndScaleOnTheNull() {
        SqlParameter parameter = Single(
            Variable("Total", ReportVariableType.Decimal, decimals: 4), new ReportExecutionRequest());

        Assert.Multiple(() => {
            Assert.That(parameter.Value, Is.EqualTo(DBNull.Value));
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.Decimal));
            Assert.That(parameter.Precision, Is.EqualTo((byte)19));
            Assert.That(parameter.Scale, Is.EqualTo((byte)4));
        });
    }

    [Test]
    public void Bind_OmittedOptionalDate_BindsTypedDbNull() {
        SqlParameter parameter = Single(Variable("From", ReportVariableType.Date), new ReportExecutionRequest());

        Assert.Multiple(() => {
            Assert.That(parameter.Value, Is.EqualTo(DBNull.Value));
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.Date));
        });
    }

    [Test]
    public void Bind_OmittedRequired_IsRejected() {
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("Name", required: true), new ReportExecutionRequest()));
    }

    [Test]
    public void Bind_EmptyStringForRequired_IsRejected() {
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("Name", required: true), Request("Name", "")));
    }

    [Test]
    public void Bind_OmittedWithDefaultValue_UsesTheDefault() {
        SqlParameter parameter = Single(
            Variable("From", ReportVariableType.Date, defaultValue: "2026-01-01"), new ReportExecutionRequest());

        Assert.That(parameter.Value, Is.EqualTo(new DateTime(2026, 1, 1)));
    }

    [Test]
    public void Bind_DefaultValueIsParsedAsStrictlyAsASubmittedOne() {
        Assert.Throws<ReportValidationException>(() =>
            Single(Variable("From", ReportVariableType.Date, defaultValue: "01/01/2026"), new ReportExecutionRequest()));
    }

    [Test]
    public void Bind_SubmittedValueOverridesTheDefault() {
        SqlParameter parameter = Single(
            Variable("From", ReportVariableType.Date, defaultValue: "2026-01-01"), Request("From", "2026-07-16"));

        Assert.That(parameter.Value, Is.EqualTo(new DateTime(2026, 7, 16)));
    }

    // ---------------------------------------------------------------- request key handling

    [Test]
    public void Bind_RequestKeyDifferingInCase_StillMatchesTheDeclaration() {
        // System.Text.Json rebuilds the dictionary with a default comparer, discarding the DTO's
        // OrdinalIgnoreCase initializer — so the binder must not rely on the incoming dictionary's comparer.
        var request = new ReportExecutionRequest {
            Variables = new Dictionary<string, ReportVariableValue>(StringComparer.Ordinal) {
                ["name"] = new() { Value = "x" }
            }
        };

        Assert.That(Single(Variable("Name"), request).Value, Is.EqualTo("x"));
    }

    [Test]
    public void Bind_SameVariableSubmittedTwiceInDifferentCase_IsRejected() {
        var request = new ReportExecutionRequest {
            Variables = new Dictionary<string, ReportVariableValue>(StringComparer.Ordinal) {
                ["name"] = new() { Value = "a" },
                ["Name"] = new() { Value = "b" }
            }
        };

        Assert.Throws<ReportValidationException>(() => Single(Variable("Name"), request));
    }

    [Test]
    public void Bind_UndeclaredRequestKey_IsIgnored() {
        // Declarations drive binding, so a stale key from a bookmarked filter cannot affect the SQL.
        var request = new ReportExecutionRequest {
            Variables = new Dictionary<string, ReportVariableValue> { ["Removed"] = new() { Value = "x" } }
        };

        Assert.That(ReportParameterBinder.Bind([Variable("Name")], request).Single().ParameterName, Is.EqualTo("@Name"));
    }

    // ---------------------------------------------------------------- select list

    [Test]
    public void Bind_SelectListDeclaredValue_IsAccepted() {
        ReportVariable variable = Variable("Status", ReportVariableType.SelectList,
            staticOptions: """[{"value":"O","label":"Open"},{"value":"C","label":"Closed"}]""");

        Assert.That(Single(variable, Request("Status", "O")).Value, Is.EqualTo("O"));
    }

    [Test]
    public void Bind_SelectListPascalCaseOptionsJson_IsAlsoUnderstood() {
        // Tolerates either serializer casing at the StaticOptions boundary.
        ReportVariable variable = Variable("Status", ReportVariableType.SelectList,
            staticOptions: """[{"Value":"O","Label":"Open"}]""");

        Assert.That(Single(variable, Request("Status", "O")).Value, Is.EqualTo("O"));
    }

    [Test]
    public void Bind_SelectListValueNotInDeclaredOptions_IsRejected() {
        ReportVariable variable = Variable("Status", ReportVariableType.SelectList,
            staticOptions: """[{"value":"O","label":"Open"}]""");

        Assert.Throws<ReportValidationException>(() => Single(variable, Request("Status", "X")));
    }

    [Test]
    public void Bind_SelectListValueWithWrongCase_IsRejected() {
        // Ordinal match: the options are the whitelist, and 'o' is not 'O'.
        ReportVariable variable = Variable("Status", ReportVariableType.SelectList,
            staticOptions: """[{"value":"O","label":"Open"}]""");

        Assert.Throws<ReportValidationException>(() => Single(variable, Request("Status", "o")));
    }

    // ---------------------------------------------------------------- sql lookup

    [Test]
    public void Bind_SqlLookupValue_IsTypeCoercedWithoutMembershipCheck() {
        // Re-running the lookup to verify membership is a wasted round trip, not a security control: a bogus
        // value returns zero rows, which is correct.
        ReportVariable variable = Variable("Whs", ReportVariableType.SqlLookup,
            lookupSql: "SELECT \"WhsCode\" AS Value, \"WhsName\" AS Label FROM OWHS");

        SqlParameter parameter = Single(variable, Request("Whs", "NEVER-EXISTED"));

        Assert.Multiple(() => {
            Assert.That(parameter.Value, Is.EqualTo("NEVER-EXISTED"));
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.NVarChar));
        });
    }

    // ---------------------------------------------------------------- multi-select

    [Test]
    public void Bind_AllowMultiple_BindsOneCsvParameterNotManyParameters() {
        // N parameters from N submitted values would make the SQL text vary at run time, breaking the invariant
        // and voiding save-time validation. The author unpacks this with STRING_SPLIT(@X, ',').
        ReportVariable variable = Variable("Whs", ReportVariableType.SqlLookup, allowMultiple: true,
            lookupSql: "SELECT 1 AS Value, 'x' AS Label");

        IReadOnlyList<SqlParameter> parameters =
            ReportParameterBinder.Bind([variable], Request("Whs", new ReportVariableValue { Values = ["01", "02", "03"] }));

        Assert.Multiple(() => {
            Assert.That(parameters, Has.Count.EqualTo(1));
            Assert.That(parameters[0].ParameterName, Is.EqualTo("@Whs"));
            Assert.That(parameters[0].SqlDbType, Is.EqualTo(SqlDbType.NVarChar));
            Assert.That(parameters[0].Size, Is.EqualTo(4000));
            Assert.That(parameters[0].Value, Is.EqualTo("01,02,03"));
        });
    }

    [Test]
    public void Bind_AllowMultipleSelectListWithDeclaredValues_BindsTheCsv() {
        // Regression: the joined "O,C" is not itself a declared option, so routing the CSV through the
        // single-value parser rejected every valid multi-select SelectList. Each element is checked on its own.
        ReportVariable variable = Variable("Status", ReportVariableType.SelectList, allowMultiple: true,
            staticOptions: """[{"value":"O","label":"Open"},{"value":"C","label":"Closed"}]""");

        SqlParameter parameter = Single(variable, Request("Status", new ReportVariableValue { Values = ["O", "C"] }));

        Assert.Multiple(() => {
            Assert.That(parameter.Value, Is.EqualTo("O,C"));
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.NVarChar));
            Assert.That(parameter.Size, Is.EqualTo(4000));
        });
    }

    [Test]
    public void Bind_AllowMultipleSelectListWithASingleDeclaredValue_BindsThatValue() {
        ReportVariable variable = Variable("Status", ReportVariableType.SelectList, allowMultiple: true,
            staticOptions: """[{"value":"O","label":"Open"}]""");

        Assert.That(Single(variable, Request("Status", new ReportVariableValue { Values = ["O"] })).Value,
            Is.EqualTo("O"));
    }

    [Test]
    public void Bind_AllowMultipleWithDefaultCsv_UsesTheDefault() {
        ReportVariable variable = Variable("Status", ReportVariableType.SelectList, allowMultiple: true,
            defaultValue: "O,C",
            staticOptions: """[{"value":"O","label":"Open"},{"value":"C","label":"Closed"}]""");

        Assert.That(Single(variable, new ReportExecutionRequest()).Value, Is.EqualTo("O,C"));
    }

    [Test]
    public void Bind_AllowMultipleWithValueContainingAComma_IsRejected() {
        // It would silently split into two values inside STRING_SPLIT.
        ReportVariable variable = Variable("Whs", ReportVariableType.SqlLookup, allowMultiple: true,
            lookupSql: "SELECT 1 AS Value, 'x' AS Label");

        Assert.Throws<ReportValidationException>(() =>
            ReportParameterBinder.Bind([variable], Request("Whs", new ReportVariableValue { Values = ["a,b"] })));
    }

    [Test]
    public void Bind_AllowMultipleSelectListWithUndeclaredValue_IsRejected() {
        ReportVariable variable = Variable("Status", ReportVariableType.SelectList, allowMultiple: true,
            staticOptions: """[{"value":"O","label":"Open"}]""");

        Assert.Throws<ReportValidationException>(() =>
            ReportParameterBinder.Bind([variable], Request("Status", new ReportVariableValue { Values = ["O", "X"] })));
    }

    [Test]
    public void Bind_AllowMultipleOmitted_BindsTypedDbNull() {
        ReportVariable variable = Variable("Whs", ReportVariableType.SqlLookup, allowMultiple: true,
            lookupSql: "SELECT 1 AS Value, 'x' AS Label");

        SqlParameter parameter = Single(variable, new ReportExecutionRequest());

        Assert.Multiple(() => {
            Assert.That(parameter.Value, Is.EqualTo(DBNull.Value));
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.NVarChar));
            Assert.That(parameter.Size, Is.EqualTo(4000));
        });
    }

    // ---------------------------------------------------------------- date range

    [Test]
    public void Bind_DateRange_BindsTwoDateParameters() {
        // A date range is two parameters, not one.
        ReportVariable variable = Variable("Doc", ReportVariableType.DateRange);

        IReadOnlyList<SqlParameter> parameters = ReportParameterBinder.Bind([variable],
            Request("Doc", new ReportVariableValue { From = "2026-01-01", To = "2026-12-31" }));

        Assert.Multiple(() => {
            Assert.That(parameters, Has.Count.EqualTo(2));
            Assert.That(parameters[0].ParameterName, Is.EqualTo("@DocFrom"));
            Assert.That(parameters[0].SqlDbType, Is.EqualTo(SqlDbType.Date));
            Assert.That(parameters[0].Value, Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(parameters[1].ParameterName, Is.EqualTo("@DocTo"));
            Assert.That(parameters[1].SqlDbType, Is.EqualTo(SqlDbType.Date));
            Assert.That(parameters[1].Value, Is.EqualTo(new DateTime(2026, 12, 31)));
        });
    }

    [Test]
    public void Bind_DateRangeFromAfterTo_IsRejected() {
        ReportVariable variable = Variable("Doc", ReportVariableType.DateRange);

        Assert.Throws<ReportValidationException>(() => ReportParameterBinder.Bind([variable],
            Request("Doc", new ReportVariableValue { From = "2026-12-31", To = "2026-01-01" })));
    }

    [Test]
    public void Bind_DateRangeSameDay_IsAccepted() {
        // The bound is inclusive: From == To is a valid single-day range.
        ReportVariable variable = Variable("Doc", ReportVariableType.DateRange);

        IReadOnlyList<SqlParameter> parameters = ReportParameterBinder.Bind([variable],
            Request("Doc", new ReportVariableValue { From = "2026-07-16", To = "2026-07-16" }));

        Assert.That(parameters, Has.Count.EqualTo(2));
    }

    [Test]
    public void Bind_DateRangeOmittedOptional_BindsTwoTypedDbNulls() {
        ReportVariable variable = Variable("Doc", ReportVariableType.DateRange);

        IReadOnlyList<SqlParameter> parameters = ReportParameterBinder.Bind([variable], new ReportExecutionRequest());

        Assert.Multiple(() => {
            Assert.That(parameters, Has.Count.EqualTo(2));
            Assert.That(parameters[0].Value, Is.EqualTo(DBNull.Value));
            Assert.That(parameters[0].SqlDbType, Is.EqualTo(SqlDbType.Date));
            Assert.That(parameters[1].Value, Is.EqualTo(DBNull.Value));
            Assert.That(parameters[1].SqlDbType, Is.EqualTo(SqlDbType.Date));
        });
    }

    [Test]
    public void Bind_DateRangeOpenEndedOptional_IsAccepted() {
        // Pairs with the author's (@DocFrom IS NULL OR col >= @DocFrom) pattern.
        ReportVariable variable = Variable("Doc", ReportVariableType.DateRange);

        IReadOnlyList<SqlParameter> parameters = ReportParameterBinder.Bind([variable],
            Request("Doc", new ReportVariableValue { From = "2026-01-01" }));

        Assert.Multiple(() => {
            Assert.That(parameters[0].Value, Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(parameters[1].Value, Is.EqualTo(DBNull.Value));
        });
    }

    [Test]
    public void Bind_DateRangeRequiredWithOnlyOneBound_IsRejected() {
        ReportVariable variable = Variable("Doc", ReportVariableType.DateRange, required: true);

        Assert.Throws<ReportValidationException>(() => ReportParameterBinder.Bind([variable],
            Request("Doc", new ReportVariableValue { From = "2026-01-01" })));
    }

    [Test]
    public void Bind_DateRangeInvalidBound_IsRejected() {
        ReportVariable variable = Variable("Doc", ReportVariableType.DateRange);

        Assert.Throws<ReportValidationException>(() => ReportParameterBinder.Bind([variable],
            Request("Doc", new ReportVariableValue { From = "yesterday", To = "2026-01-01" })));
    }

    // ---------------------------------------------------------------- error aggregation

    [Test]
    public void Bind_MultipleBadValues_ReportsEveryErrorAtOnce() {
        var request = new ReportExecutionRequest {
            Variables = new Dictionary<string, ReportVariableValue> {
                ["A"] = new() { Value = "abc" },
                ["B"] = new() { Value = "nope" }
            }
        };

        var exception = Assert.Throws<ReportValidationException>(() => ReportParameterBinder.Bind(
            [Variable("A", ReportVariableType.Integer), Variable("B", ReportVariableType.Date)], request));

        Assert.That(exception!.Errors, Has.Count.EqualTo(2));
    }

    // ---------------------------------------------------------------- bind-all-null (discovery)

    [Test]
    public void BindAllNull_EveryVariable_BindsTypedNulls() {
        IReadOnlyList<SqlParameter> parameters = ReportParameterBinder.BindAllNull([
            Variable("Name"),
            Variable("DocEntry", ReportVariableType.Integer),
            Variable("At", ReportVariableType.DateTime)
        ]);

        Assert.Multiple(() => {
            Assert.That(parameters, Has.Count.EqualTo(3));
            Assert.That(parameters.All(p => p.Value == DBNull.Value), Is.True);
            Assert.That(parameters[0].SqlDbType, Is.EqualTo(SqlDbType.NVarChar));
            Assert.That(parameters[1].SqlDbType, Is.EqualTo(SqlDbType.BigInt));
            Assert.That(parameters[2].SqlDbType, Is.EqualTo(SqlDbType.DateTime2));
        });
    }

    [Test]
    public void BindAllNull_DateRange_BindsBothBoundsAsNull() {
        IReadOnlyList<SqlParameter> parameters =
            ReportParameterBinder.BindAllNull([Variable("Doc", ReportVariableType.DateRange)]);

        Assert.Multiple(() => {
            Assert.That(parameters.Select(p => p.ParameterName), Is.EqualTo(new[] { "@DocFrom", "@DocTo" }));
            Assert.That(parameters.All(p => p.Value == DBNull.Value), Is.True);
        });
    }

    [Test]
    public void BindAllNull_RequiredVariable_IsStillBoundAsNull() {
        // Discovery is not a run: the shape must compile, and (@X IS NULL OR col = @X) is a valid shape.
        IReadOnlyList<SqlParameter> parameters =
            ReportParameterBinder.BindAllNull([Variable("Name", required: true)]);

        Assert.That(parameters.Single().Value, Is.EqualTo(DBNull.Value));
    }

    [Test]
    public void BindAllNull_InvalidDeclaration_Throws() {
        Assert.Throws<ReportValidationException>(() => ReportParameterBinder.BindAllNull([Variable("__limit")]));
    }

    // ---------------------------------------------------------------- paging

    [Test]
    public void CreateOffsetParameter_ValidOffset_BindsTypedInt() {
        SqlParameter parameter = ReportParameterBinder.CreateOffsetParameter(100);

        Assert.Multiple(() => {
            Assert.That(parameter.ParameterName, Is.EqualTo("@__offset"));
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.Int));
            Assert.That(parameter.Value, Is.EqualTo(100));
        });
    }

    [Test]
    public void CreateLimitParameter_BindsTypedInt() {
        SqlParameter parameter = ReportParameterBinder.CreateLimitParameter(51);

        Assert.Multiple(() => {
            Assert.That(parameter.ParameterName, Is.EqualTo("@__limit"));
            Assert.That(parameter.SqlDbType, Is.EqualTo(SqlDbType.Int));
            Assert.That(parameter.Value, Is.EqualTo(51));
        });
    }

    [Test]
    public void ValidateOffset_Negative_Throws() {
        Assert.Throws<ReportValidationException>(() => ReportParameterBinder.ValidateOffset(-1));
    }

    [Test]
    public void ValidateOffset_AboveHardCap_Throws() {
        // OFFSET is O(n): a half-million offset rescans half a million rows on every page.
        Assert.Throws<ReportValidationException>(() => ReportParameterBinder.ValidateOffset(100_001));
    }

    [Test]
    public void ValidateOffset_AtHardCap_IsAccepted() {
        Assert.That(ReportParameterBinder.ValidateOffset(100_000), Is.EqualTo(100_000));
    }

    [Test]
    public void ClampLimit_AboveMax_IsClampedToMax() {
        Assert.That(ReportParameterBinder.ClampLimit(5000, 50), Is.EqualTo(200));
    }

    [Test]
    public void ClampLimit_Zero_IsClampedToOne() {
        Assert.That(ReportParameterBinder.ClampLimit(0, 50), Is.EqualTo(1));
    }

    [Test]
    public void ClampLimit_Negative_IsClampedToOne() {
        Assert.That(ReportParameterBinder.ClampLimit(-10, 50), Is.EqualTo(1));
    }

    [Test]
    public void ClampLimit_NotRequested_UsesTheReportsDefaultPageSize() {
        Assert.That(ReportParameterBinder.ClampLimit(null, 25), Is.EqualTo(25));
    }

    [Test]
    public void ClampLimit_NotRequestedWithOversizedDefault_IsStillClamped() {
        Assert.That(ReportParameterBinder.ClampLimit(null, 100_000), Is.EqualTo(200));
    }

    [Test]
    public void ClampLimit_ExportCeiling_AllowsUpToTheHardMax() {
        Assert.That(ReportParameterBinder.ClampLimit(500, 50, maxLimit: 500), Is.EqualTo(500));
    }

    [Test]
    public void ClampLimit_CeilingAboveHardMax_Throws() {
        Assert.Throws<ReportValidationException>(() => ReportParameterBinder.ClampLimit(50, 50, maxLimit: 501));
    }
}
