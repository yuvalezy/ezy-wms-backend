using Core.DTOs.Items;
using Core.Entities;
using Core.Enums;
using Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Unit.Services;

/// <summary>
/// A scanned count is the truth a line was created from; its base quantity is only a derivation. When SAP's
/// conversion factor moves — or Options.ForceDoz is toggled — the derivation has to be redone, and redone the
/// same way the flow originally scanned it.
/// </summary>
[TestFixture]
public class QuantityRestatementTests {
    private static ItemCheckResponse Item(
        decimal numInBuy = 12m, decimal purPackUn = 6m,
        decimal factor1 = 1m, decimal factor2 = 1m, decimal factor3 = 1m, decimal factor4 = 1m) => new() {
        ItemCode = "PZA_TEST",
        NumInBuy = numInBuy,
        PurPackUn = purPackUn,
        Factor1 = factor1,
        Factor2 = factor2,
        Factor3 = factor3,
        Factor4 = factor4
    };

    private static Dictionary<string, ItemCheckResponse> Catalog(ItemCheckResponse item) =>
        new() { [item.ItemCode] = item };

    private static PickList Line(UnitType unit, decimal? scanned, decimal baseQty) => new() {
        Id = Guid.NewGuid(),
        AbsEntry = 13,
        PickEntry = 0,
        ItemCode = "PZA_TEST",
        Unit = unit,
        ScannedQuantity = scanned,
        Quantity = baseQty,
        CreatedAt = DateTime.UtcNow
    };

    private static int Restate(PickList line, ItemCheckResponse item, bool applyFactor = true) =>
        QuantityRestatement.Restate([line], Catalog(item), applyFactor, NullLogger.Instance);

    // The whole point: the count stays put, the pieces are recomputed.
    [Test]
    public void Restate_FactorChanged_RederivesTheBaseAndLeavesTheScannedCountAlone() {
        // 2 dozens scanned back when a dozen meant 1 piece; the factor is now a real 12.
        var line = Line(UnitType.Dozen, scanned: 2m, baseQty: 2m);

        int changed = Restate(line, Item(numInBuy: 12m));

        Assert.Multiple(() => {
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(line.Quantity, Is.EqualTo(24m), "2 dozens at a factor of 12 is 24 pieces");
            Assert.That(line.ScannedQuantity, Is.EqualTo(2m), "the operator still scanned 2 dozens");
        });
    }

    [Test]
    public void Restate_FactorUnchanged_WritesNothing() {
        var line = Line(UnitType.Dozen, scanned: 2m, baseQty: 24m);

        int changed = Restate(line, Item(numInBuy: 12m));

        Assert.Multiple(() => {
            Assert.That(changed, Is.EqualTo(0), "nothing moved, so nothing should be saved");
            Assert.That(line.Quantity, Is.EqualTo(24m));
        });
    }

    // Rows created before the ScannedQuantity column existed have no truth to re-derive from.
    [Test]
    public void Restate_NoScannedCount_LeavesTheLineFrozen() {
        var line = Line(UnitType.Dozen, scanned: null, baseQty: 2m);

        int changed = Restate(line, Item(numInBuy: 12m));

        Assert.Multiple(() => {
            Assert.That(changed, Is.EqualTo(0));
            Assert.That(line.Quantity, Is.EqualTo(2m), "a legacy line keeps its stored base");
        });
    }

    [Test]
    public void Restate_ItemMissingFromSap_LeavesTheLineAlone() {
        var line = Line(UnitType.Dozen, scanned: 2m, baseQty: 2m);

        int changed = QuantityRestatement.Restate(
            [line], new Dictionary<string, ItemCheckResponse>(), applyFactor: true, NullLogger.Instance);

        Assert.Multiple(() => {
            Assert.That(changed, Is.EqualTo(0));
            Assert.That(line.Quantity, Is.EqualTo(2m));
        });
    }

    // --- The unit ladder ---

    [Test]
    public void Restate_UnitLine_IsNeverMultiplied() {
        var line = Line(UnitType.Unit, scanned: 5m, baseQty: 5m);

        int changed = Restate(line, Item(numInBuy: 12m, purPackUn: 6m));

        Assert.Multiple(() => {
            Assert.That(changed, Is.EqualTo(0));
            Assert.That(line.Quantity, Is.EqualTo(5m), "a piece is a piece at any factor");
        });
    }

    [Test]
    public void Restate_PackLine_MultipliesThroughBothRungs() {
        var line = Line(UnitType.Pack, scanned: 1m, baseQty: 1m);

        Restate(line, Item(numInBuy: 12m, purPackUn: 6m));

        Assert.That(line.Quantity, Is.EqualTo(72m), "a pack holds PurPackUn dozens of NumInBuy pieces");
    }

    // --- The factor policy, which must mirror each flow's own scan ---

    [Test]
    public void Restate_ApplyFactorFalse_IgnoresThePurFactors() {
        // Counting has never applied PurFactor1..4 — ValidateAddItemResult carries none. Restating with them
        // would multiply every counted line on the first read after this shipped.
        var line = Line(UnitType.Dozen, scanned: 2m, baseQty: 24m);

        int changed = Restate(line, Item(numInBuy: 12m, factor1: 3m), applyFactor: false);

        Assert.Multiple(() => {
            Assert.That(changed, Is.EqualTo(0), "counting's own formula already produced this base");
            Assert.That(line.Quantity, Is.EqualTo(24m));
        });
    }

    [Test]
    public void Restate_ApplyFactorTrue_MultipliesThroughThePurFactors() {
        var line = Line(UnitType.Dozen, scanned: 2m, baseQty: 24m);

        Restate(line, Item(numInBuy: 12m, factor1: 3m), applyFactor: true);

        Assert.That(line.Quantity, Is.EqualTo(72m), "picking and transfer scan with the factors applied");
    }

    // --- Bad master data ---

    [Test]
    public void Restate_ZeroFactor_DoesNotWipeTheLineOut() {
        var line = Line(UnitType.Dozen, scanned: 2m, baseQty: 24m);

        Restate(line, Item(numInBuy: 0m));

        Assert.That(line.Quantity, Is.EqualTo(2m), "a zero factor is meaningless master data, not a reason to zero stock");
    }

    // --- Convergence ---

    // The column holds decimal(18,2). Restating to full precision and comparing against a value SQL Server
    // rounded on write would never match, so the line would be "repaired" and re-saved on every single load.
    [Test]
    public void Restate_ResultNeedingMoreDecimalsThanTheColumnHolds_ConvergesInsteadOfRepairingForever() {
        var item = Item(numInBuy: 0.3333m);
        var line = Line(UnitType.Dozen, scanned: 1m, baseQty: 0m);

        int first = Restate(line, item);
        decimal afterFirst = line.Quantity;
        int second = Restate(line, item);

        Assert.Multiple(() => {
            Assert.That(first, Is.EqualTo(1));
            Assert.That(afterFirst, Is.EqualTo(0.33m), "rounded to what the column can actually store");
            Assert.That(second, Is.EqualTo(0), "a second read must not restate the same line again");
        });
    }
}
