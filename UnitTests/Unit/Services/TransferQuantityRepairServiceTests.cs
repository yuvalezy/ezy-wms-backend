using Core.DTOs.Items;
using Core.Entities;
using Core.Enums;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Unit.Services;

/// <summary>
/// A transfer only processes when, per item, what left the source equals what reached the target — checked in
/// SQL with exact equality. Under ForceDoz a Dozen line is raised twelvefold while a Pack line stays put (the
/// pack is divided to compensate), so restating each line on its own can pull the two sides apart. A transfer
/// that cannot process is worse than one holding stale numbers, so the guard restates an item only when the
/// balance survives.
/// </summary>
[TestFixture]
public class TransferQuantityRepairServiceTests {
    private static readonly Guid TransferId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static ItemCheckResponse Item(string itemCode, decimal numInBuy, decimal purPackUn) => new() {
        ItemCode = itemCode,
        NumInBuy = numInBuy,
        PurPackUn = purPackUn,
        Factor1 = 1m, Factor2 = 1m, Factor3 = 1m, Factor4 = 1m
    };

    private static Dictionary<string, ItemCheckResponse> Catalog(params ItemCheckResponse[] items) =>
        items.ToDictionary(i => i.ItemCode);

    private static TransferLine Line(SourceTarget side, UnitType unit, decimal? scanned, decimal baseQty, string itemCode = "PZA_TEST") => new() {
        Id = Guid.NewGuid(),
        TransferId = TransferId,
        ItemCode = itemCode,
        Type = side,
        UnitType = unit,
        ScannedQuantity = scanned,
        Quantity = baseQty,
        LineStatus = LineStatus.Open,
        Date = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    };

    private static int Restate(IReadOnlyCollection<TransferLine> lines, Dictionary<string, ItemCheckResponse> items) =>
        TransferQuantityRepairService.RestateBalanced(TransferId, lines, items, NullLogger.Instance);

    // Both sides scanned the same way scale together, so the balance survives and the restatement stands.
    [Test]
    public void RestateBalanced_BothSidesSameUnit_RestatesBothAndStaysBalanced() {
        // 2 dozens out, 2 dozens in, recorded back when a dozen meant 1 piece.
        var source = Line(SourceTarget.Source, UnitType.Dozen, scanned: 2m, baseQty: 2m);
        var target = Line(SourceTarget.Target, UnitType.Dozen, scanned: 2m, baseQty: 2m);

        int repaired = Restate([source, target], Catalog(Item("PZA_TEST", numInBuy: 12m, purPackUn: 1m)));

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(2));
            Assert.That(source.Quantity, Is.EqualTo(24m));
            Assert.That(target.Quantity, Is.EqualTo(24m));
            Assert.That(source.Quantity, Is.EqualTo(target.Quantity), "the transfer must still be processable");
        });
    }

    // The ForceDoz asymmetry, which is the whole reason the guard exists: the pack is compensated so it does
    // not move, but the dozen is raised twelvefold. Restating per line would leave 12 out and 144 in.
    [Test]
    public void RestateBalanced_PackSourceAndDozenTarget_RevertsTheItemRatherThanBreakingTheBalance() {
        // Under the old degenerate ladder (NumInBuy 1, PurPackUn 12) both sides came to 12 pieces.
        var source = Line(SourceTarget.Source, UnitType.Pack, scanned: 1m, baseQty: 12m);
        var target = Line(SourceTarget.Target, UnitType.Dozen, scanned: 12m, baseQty: 12m);

        // ForceDoz: the rung becomes 12 and the pack is divided to compensate.
        int repaired = Restate([source, target], Catalog(Item("PZA_TEST", numInBuy: 12m, purPackUn: 1m)));

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(0), "nothing may be persisted for an item the guard rejected");
            Assert.That(source.Quantity, Is.EqualTo(12m), "reverted to its stored value");
            Assert.That(target.Quantity, Is.EqualTo(12m), "reverted to its stored value");
            Assert.That(source.Quantity, Is.EqualTo(target.Quantity), "the transfer stays processable");
        });
    }

    // A cross-warehouse transfer is shipped, not put away, so only the source is ever scanned. There is no
    // balance to protect and the restatement must go through.
    [Test]
    public void RestateBalanced_SourceOnly_RestatesFreely() {
        var source = Line(SourceTarget.Source, UnitType.Dozen, scanned: 2m, baseQty: 2m);

        int repaired = Restate([source], Catalog(Item("PZA_TEST", numInBuy: 12m, purPackUn: 1m)));

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(1));
            Assert.That(source.Quantity, Is.EqualTo(24m));
        });
    }

    // Mid-scan, the operator has taken stock but not put it away yet. The sides are already unequal, so the
    // guard has no balance to preserve and must not block the restatement.
    [Test]
    public void RestateBalanced_AlreadyOutOfBalance_RestatesAnyway() {
        var source = Line(SourceTarget.Source, UnitType.Dozen, scanned: 2m, baseQty: 2m);
        var target = Line(SourceTarget.Target, UnitType.Dozen, scanned: 1m, baseQty: 1m);

        int repaired = Restate([source, target], Catalog(Item("PZA_TEST", numInBuy: 12m, purPackUn: 1m)));

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(2));
            Assert.That(source.Quantity, Is.EqualTo(24m));
            Assert.That(target.Quantity, Is.EqualTo(12m));
        });
    }

    // One bad item must not hold up the rest of the transfer.
    [Test]
    public void RestateBalanced_OneItemRejected_TheOtherItemStillRestates() {
        var mixedSource = Line(SourceTarget.Source, UnitType.Pack, scanned: 1m, baseQty: 12m, itemCode: "MIXED");
        var mixedTarget = Line(SourceTarget.Target, UnitType.Dozen, scanned: 12m, baseQty: 12m, itemCode: "MIXED");
        var cleanSource = Line(SourceTarget.Source, UnitType.Dozen, scanned: 2m, baseQty: 2m, itemCode: "CLEAN");
        var cleanTarget = Line(SourceTarget.Target, UnitType.Dozen, scanned: 2m, baseQty: 2m, itemCode: "CLEAN");

        int repaired = Restate(
            [mixedSource, mixedTarget, cleanSource, cleanTarget],
            Catalog(Item("MIXED", numInBuy: 12m, purPackUn: 1m), Item("CLEAN", numInBuy: 12m, purPackUn: 1m)));

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(2), "only CLEAN's two lines");
            Assert.That(mixedSource.Quantity, Is.EqualTo(12m), "MIXED reverted");
            Assert.That(mixedTarget.Quantity, Is.EqualTo(12m), "MIXED reverted");
            Assert.That(cleanSource.Quantity, Is.EqualTo(24m));
            Assert.That(cleanTarget.Quantity, Is.EqualTo(24m));
        });
    }

    // A line whose base was typed into the target-detail screen has no scan behind it, but it is still stock
    // sitting in a bin — it has to count toward the balance the guard checks.
    [Test]
    public void RestateBalanced_HandEditedTargetLine_CountsTowardTheBalanceButIsNotRestated() {
        var source = Line(SourceTarget.Source, UnitType.Dozen, scanned: 2m, baseQty: 2m);
        var target = Line(SourceTarget.Target, UnitType.Dozen, scanned: null, baseQty: 2m);

        int repaired = Restate([source, target], Catalog(Item("PZA_TEST", numInBuy: 12m, purPackUn: 1m)));

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(0), "restating the source alone would unbalance the item");
            Assert.That(source.Quantity, Is.EqualTo(2m), "reverted");
            Assert.That(target.Quantity, Is.EqualTo(2m), "never had a scanned count to re-derive from");
        });
    }

    [Test]
    public void RestateBalanced_NothingChanged_ReportsNoRepair() {
        var source = Line(SourceTarget.Source, UnitType.Dozen, scanned: 2m, baseQty: 24m);
        var target = Line(SourceTarget.Target, UnitType.Dozen, scanned: 2m, baseQty: 24m);

        int repaired = Restate([source, target], Catalog(Item("PZA_TEST", numInBuy: 12m, purPackUn: 1m)));

        Assert.That(repaired, Is.EqualTo(0), "the factor never moved");
    }
}
