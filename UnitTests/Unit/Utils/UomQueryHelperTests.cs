using Adapters.Common.Utils;
using Core.Models.Settings;

namespace UnitTests.Unit.Utils;

[TestFixture]
public class UomQueryHelperTests {
    private static Settings SettingsWith(bool forceDoz) => new() { Options = new Options { ForceDoz = forceDoz } };

    private const string Condition = """COALESCE(OITM."NumInBuy", 1) = 1 AND COALESCE(OITM."PurPackUn", 1) % 12 = 0""";

    // --- Off: byte-identical to the pre-ForceDoz queries ---

    [Test]
    public void NumInBuy_ForceDozOff_ReturnsPlainCoalesce() {
        Assert.That(UomQueryHelper.NumInBuy(SettingsWith(false), "OITM"), Is.EqualTo("""COALESCE(OITM."NumInBuy", 1)"""));
    }

    [Test]
    public void PurPackUn_ForceDozOff_ReturnsPlainCoalesce() {
        Assert.That(UomQueryHelper.PurPackUn(SettingsWith(false), "OITM"), Is.EqualTo("""COALESCE(OITM."PurPackUn", 1)"""));
    }

    [Test]
    public void BuyUnitMsr_ForceDozOff_ReturnsThePlainColumn() {
        Assert.That(UomQueryHelper.BuyUnitMsr(SettingsWith(false), "OITM"), Is.EqualTo("OITM.\"BuyUnitMsr\""));
    }

    [Test]
    public void ForceDozOff_EmitsNoCaseAtAll() {
        var settings = SettingsWith(false);

        Assert.Multiple(() => {
            Assert.That(UomQueryHelper.NumInBuy(settings), Does.Not.Contain("CASE"));
            Assert.That(UomQueryHelper.PurPackUn(settings), Does.Not.Contain("CASE"));
            Assert.That(UomQueryHelper.BuyUnitMsr(settings), Does.Not.Contain("CASE"));
        });
    }

    // --- On: the three substitutions ---

    [Test]
    public void NumInBuy_ForceDozOn_RaisesADegenerateRungToTwelve() {
        Assert.That(UomQueryHelper.NumInBuy(SettingsWith(true), "OITM"),
            Is.EqualTo($"""CASE WHEN {Condition} THEN 12 ELSE COALESCE(OITM."NumInBuy", 1) END"""));
    }

    [Test]
    public void PurPackUn_ForceDozOn_DividesThePackByTwelve() {
        Assert.That(UomQueryHelper.PurPackUn(SettingsWith(true), "OITM"),
            Is.EqualTo($"""CASE WHEN {Condition} THEN COALESCE(OITM."PurPackUn", 1) / 12 ELSE COALESCE(OITM."PurPackUn", 1) END"""));
    }

    [Test]
    public void BuyUnitMsr_ForceDozOn_RelabelsAConvertedRung() {
        Assert.That(UomQueryHelper.BuyUnitMsr(SettingsWith(true), "OITM"),
            Is.EqualTo($"""CASE WHEN {Condition} THEN 'Doz' ELSE OITM."BuyUnitMsr" END"""));
    }

    // --- The invariants that make the substitution safe ---

    // NumInBuy x PurPackUn is the pack's base pieces. Raising NumInBuy without dividing PurPackUn multiplies
    // every pack by twelve — the defect that inflated a 72-piece box to 864.
    [Test]
    public void ForceDozOn_NumInBuyAndPurPackUn_ShareTheExactSameCondition() {
        var settings = SettingsWith(true);

        string numInBuy = UomQueryHelper.NumInBuy(settings, "OITM");
        string purPackUn = UomQueryHelper.PurPackUn(settings, "OITM");

        Assert.Multiple(() => {
            Assert.That(numInBuy, Does.Contain(Condition), "NumInBuy must convert on exactly the pack-compensated condition");
            Assert.That(purPackUn, Does.Contain(Condition), "PurPackUn must divide on exactly the same rows NumInBuy is raised on");
            Assert.That(UomQueryHelper.BuyUnitMsr(settings, "OITM"), Does.Contain(Condition));
        });
    }

    // A pack of 4 is not a third of a dozen: 4/12 re-multiplies to 3.999996, and the rung would exceed the
    // pack containing it. Only whole-dozen packs convert.
    [Test]
    public void ForceDozOn_OnlyConvertsPacksThatAreAWholeNumberOfDozens() {
        Assert.That(UomQueryHelper.NumInBuy(SettingsWith(true)), Does.Contain("""COALESCE("PurPackUn", 1) % 12 = 0"""));
    }

    [Test]
    public void ForceDozOn_LeavesARealFactorAlone() {
        // The condition requires NumInBuy = 1, so an item SAP already describes correctly is never touched.
        Assert.That(UomQueryHelper.NumInBuy(SettingsWith(true)), Does.Contain("""COALESCE("NumInBuy", 1) = 1"""));
    }

    [Test]
    public void ForceDozOn_TreatsNullsAsTheirSapDefaults() {
        var settings = SettingsWith(true);

        // A NULL NumInBuy is a degenerate 1; a NULL PurPackUn is 1, which is not a whole dozen and so never converts.
        Assert.Multiple(() => {
            Assert.That(UomQueryHelper.NumInBuy(settings), Does.Contain("""COALESCE("NumInBuy", 1)"""));
            Assert.That(UomQueryHelper.PurPackUn(settings), Does.Contain("""COALESCE("PurPackUn", 1)"""));
        });
    }

    [Test]
    public void WithQualifier_EveryColumnReferenceIsPrefixed() {
        var settings = SettingsWith(true);

        Assert.Multiple(() => {
            Assert.That(UomQueryHelper.NumInBuy(settings, "T4"), Does.Not.Contain("\"NumInBuy\"").IgnoreCase.Or.Contain("T4.\"NumInBuy\""));
            Assert.That(UomQueryHelper.PurPackUn(settings, "T4"), Does.Contain("T4.\"PurPackUn\""));
            Assert.That(UomQueryHelper.BuyUnitMsr(settings, "T4"), Does.Contain("T4.\"BuyUnitMsr\""));
        });
    }
}
