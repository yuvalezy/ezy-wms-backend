using Adapters.Common.Utils;
using Core.Models.Settings;

namespace UnitTests.Unit.Utils;

[TestFixture]
public class UomQueryHelperTests {
    private static Settings SettingsWith(bool forceDoz) => new() { Options = new Options { ForceDoz = forceDoz } };

    [Test]
    public void NumInBuy_ForceDozOff_ReturnsPlainCoalesce() {
        var result = UomQueryHelper.NumInBuy(SettingsWith(false));

        Assert.That(result, Is.EqualTo("""COALESCE("NumInBuy", 1)"""));
    }

    [Test]
    public void NumInBuy_ForceDozOff_WithQualifier_PrefixesTheTableAlias() {
        var result = UomQueryHelper.NumInBuy(SettingsWith(false), "OITM");

        Assert.That(result, Is.EqualTo("""COALESCE(OITM."NumInBuy", 1)"""));
    }

    [Test]
    public void NumInBuy_ForceDozOn_SubstitutesTwelveOnlyForADozenLabelledUnit() {
        var result = UomQueryHelper.NumInBuy(SettingsWith(true));

        Assert.That(result, Is.EqualTo(
            """CASE WHEN COALESCE("NumInBuy", 1) = 1 AND UPPER(LTRIM(RTRIM(COALESCE("BuyUnitMsr", '')))) = 'DOZ' THEN 12 ELSE COALESCE("NumInBuy", 1) END"""));
    }

    [Test]
    public void NumInBuy_ForceDozOn_WithQualifier_PrefixesBothColumns() {
        var result = UomQueryHelper.NumInBuy(SettingsWith(true), "T4");

        Assert.That(result, Is.EqualTo(
            """CASE WHEN COALESCE(T4."NumInBuy", 1) = 1 AND UPPER(LTRIM(RTRIM(COALESCE(T4."BuyUnitMsr", '')))) = 'DOZ' THEN 12 ELSE COALESCE(T4."NumInBuy", 1) END"""));
    }

    // The guard is the whole point: a factor of 1 is correct for a piece-based unit (PZA, EA, blank), so the
    // label must be part of the condition. Without it the override rewrites every piece item's stock by 12x.
    [Test]
    public void NumInBuy_ForceDozOn_GatesTheSubstitutionOnTheUnitLabel() {
        var result = UomQueryHelper.NumInBuy(SettingsWith(true), "OITM");

        Assert.That(result, Does.Contain("""OITM."BuyUnitMsr"""), "the substitution must be conditional on the unit label");
        Assert.That(result, Does.Contain("'DOZ'"));
    }

    [Test]
    public void NumInBuy_ForceDozOn_MatchesTheLabelCaseInsensitivelyAndTrimmed() {
        var result = UomQueryHelper.NumInBuy(SettingsWith(true));

        // Real master data uses 'Doz'; UPPER + trim keeps 'Doz'/'DOZ'/' doz ' all matching.
        Assert.That(result, Does.Contain("UPPER(LTRIM(RTRIM("));
    }

    [Test]
    public void NumInBuy_ForceDozOn_TreatsANullLabelAsNotADozen() {
        var result = UomQueryHelper.NumInBuy(SettingsWith(true));

        // 78 items in real data have a NULL label with NumInBuy = 1 — none may be rewritten.
        Assert.That(result, Does.Contain("""COALESCE("BuyUnitMsr", '')"""));
    }
}
