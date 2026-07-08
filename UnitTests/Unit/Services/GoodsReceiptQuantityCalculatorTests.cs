using Core.Enums;
using Core.Services;

namespace UnitTests.Unit.Services;

[TestFixture]
public class GoodsReceiptQuantityCalculatorTests {
    // NumInBuy = pieces per dozen (12), PurPackUn = dozens per box/bulto (3).
    private const decimal NumInBuy = 12m;
    private const decimal PurPackUn = 3m;

    [Test]
    public void ToBaseQuantity_Pack_MultipliesByNumInBuyAndPurPackUn() {
        // 10 bultos × 3 docenas × 12 unidades = 360 pieces
        var result = GoodsReceiptQuantityCalculator.ToBaseQuantity(10m, UnitType.Pack, NumInBuy, PurPackUn);

        Assert.That(result, Is.EqualTo(360m));
    }

    [Test]
    public void ToBaseQuantity_Dozen_MultipliesByNumInBuyOnly() {
        // 10 docenas × 12 = 120 pieces
        var result = GoodsReceiptQuantityCalculator.ToBaseQuantity(10m, UnitType.Dozen, NumInBuy, PurPackUn);

        Assert.That(result, Is.EqualTo(120m));
    }

    [Test]
    public void ToBaseQuantity_Unit_ReturnsScannedCount() {
        var result = GoodsReceiptQuantityCalculator.ToBaseQuantity(10m, UnitType.Unit, NumInBuy, PurPackUn);

        Assert.That(result, Is.EqualTo(10m));
    }

    [Test]
    public void ToBaseQuantity_AppliesPurFactors() {
        // 10 bultos × 3 × 12 × factor2(2) = 720
        var result = GoodsReceiptQuantityCalculator.ToBaseQuantity(10m, UnitType.Pack, NumInBuy, PurPackUn, factor2: 2m);

        Assert.That(result, Is.EqualTo(720m));
    }

    [Test]
    public void ToBaseQuantity_SupportsDecimalFactor_NoTruncation() {
        // Corrected fractional conversion: 1.5 dozens per box. 10 × 1.5 × 12 = 180.
        var result = GoodsReceiptQuantityCalculator.ToBaseQuantity(10m, UnitType.Pack, NumInBuy, 1.5m);

        Assert.That(result, Is.EqualTo(180m));
    }

    [Test]
    public void ToBaseQuantity_ZeroFactor_TreatedAsOne() {
        // A 0/negative conversion factor must never wipe out the received quantity.
        var result = GoodsReceiptQuantityCalculator.ToBaseQuantity(10m, UnitType.Pack, 0m, 0m);

        Assert.That(result, Is.EqualTo(10m));
    }

    [Test]
    public void FactorCorrection_KeepsScannedBoxes_RecalculatesPieces() {
        // Operator scans 10 bultos while SAP says 3 docenas/box.
        const decimal scannedBoxes = 10m;
        var piecesBefore = GoodsReceiptQuantityCalculator.ToBaseQuantity(scannedBoxes, UnitType.Pack, NumInBuy, 3m);
        Assert.That(piecesBefore, Is.EqualTo(360m));

        // SAP conversion corrected to 4 docenas/box. Re-deriving from the SAME scanned count:
        var piecesAfter = GoodsReceiptQuantityCalculator.ToBaseQuantity(scannedBoxes, UnitType.Pack, NumInBuy, 4m);

        // Pieces recalculate (360 → 480) but the box count is unchanged — no phantom quantity.
        Assert.That(piecesAfter, Is.EqualTo(480m));
        Assert.That(GoodsReceiptQuantityCalculator.ToScannedQuantity(piecesAfter, UnitType.Pack, NumInBuy, 4m), Is.EqualTo(scannedBoxes));
    }

    [Test]
    public void ConfirmationRescale_FrozenSourceToCurrentFactor_NoPrecisionLoss() {
        // Reproduces the confirmation-adjustment bug: 5 bultos scanned while SAP said 3 doc/box →
        // frozen line/source base = 5 × 3 × 12 = 180 pieces. SAP corrected to 4 doc/box → the source
        // must rescale to 5 × 4 × 12 = 240 pieces (SAP otherwise receives 180 → 3.75 boxes).
        const decimal scannedBoxes = 5m;
        const decimal frozenBase = 5m * 3m * NumInBuy; // 180 (also the single source's quantity)

        decimal effectiveBase = GoodsReceiptQuantityCalculator.EffectiveBaseQuantity(scannedBoxes, UnitType.Pack, frozenBase, NumInBuy, 4m);
        decimal rescaledSource = GoodsReceiptQuantityCalculator.RescaleAllocation(frozenBase, frozenBase, effectiveBase);

        Assert.That(effectiveBase, Is.EqualTo(240m));
        Assert.That(rescaledSource, Is.EqualTo(240m)); // exact — multiply-then-divide, no repeating-decimal residual
    }

    [Test]
    public void RescaleAllocation_SplitSources_PreservesShareAndSum() {
        // Line base 180 split across two sources (120 + 60); factor 3→4 ⇒ effective base 240.
        decimal a = GoodsReceiptQuantityCalculator.RescaleAllocation(120m, 180m, 240m);
        decimal b = GoodsReceiptQuantityCalculator.RescaleAllocation(60m, 180m, 240m);

        Assert.That(a, Is.EqualTo(160m));
        Assert.That(b, Is.EqualTo(80m));
        Assert.That(a + b, Is.EqualTo(240m));
    }

    [Test]
    public void RescaleAllocation_ZeroStoredBase_ReturnsUnchanged() {
        Assert.That(GoodsReceiptQuantityCalculator.RescaleAllocation(5m, 0m, 99m), Is.EqualTo(5m));
    }

    [Test]
    public void EffectiveBaseQuantity_LegacyLine_FallsBackToStoredBase() {
        var result = GoodsReceiptQuantityCalculator.EffectiveBaseQuantity(0m, UnitType.Pack, 180m, NumInBuy, 4m);

        Assert.That(result, Is.EqualTo(180m));
    }

    [Test]
    public void ToScannedQuantity_IsInverseOfToBaseQuantity() {
        var baseQty = GoodsReceiptQuantityCalculator.ToBaseQuantity(7m, UnitType.Pack, NumInBuy, PurPackUn, 2m, 1m, 1m, 1m);
        var scanned = GoodsReceiptQuantityCalculator.ToScannedQuantity(baseQty, UnitType.Pack, NumInBuy, PurPackUn, 2m, 1m, 1m, 1m);

        Assert.That(scanned, Is.EqualTo(7m));
    }
}
