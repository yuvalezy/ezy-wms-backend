using Core.DTOs.Items;
using Core.Entities;
using Core.Enums;
using Infrastructure.Services;

namespace UnitTests.Unit.Services;

/// <summary>
/// The transfer summary report is a supervisor's only view of what a transfer actually moved, so what it shows
/// has to agree with the lines. It collapses both sides of a transfer into one row per item: what left the
/// source bins, what reached the target bins, and what is still open between them.
/// </summary>
[TestFixture]
public class TransferReportServiceTests {
    private static readonly Guid TransferId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ItemCheckResponse Item(string itemCode, string itemName = "Widget") => new() {
        ItemCode = itemCode,
        ItemName = itemName,
        NumInBuy = 12m,
        BuyUnitMsr = "DOZ",
        PurPackUn = 1m,
        PurPackMsr = "BOX",
        Factor1 = 1m, Factor2 = 1m, Factor3 = 1m, Factor4 = 1m
    };

    private static Dictionary<string, ItemCheckResponse> Catalog(params ItemCheckResponse[] items) =>
        items.ToDictionary(i => i.ItemCode);

    private static TransferLine Line(SourceTarget side, decimal quantity, int? binEntry = null, string itemCode = "ITM-001") => new() {
        Id = Guid.NewGuid(),
        TransferId = TransferId,
        ItemCode = itemCode,
        Type = side,
        UnitType = UnitType.Pack,
        Quantity = quantity,
        BinEntry = binEntry,
        LineStatus = LineStatus.Open,
        Date = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    };

    private static List<Core.DTOs.Transfer.TransferSummaryReportLine> Build(
        IReadOnlyCollection<TransferLine> lines,
        Dictionary<string, ItemCheckResponse>? items = null,
        Dictionary<int, string>? bins = null) =>
        TransferReportService.BuildLines(lines, items ?? Catalog(Item("ITM-001")), bins ?? new Dictionary<int, string>());

    // Picked but not yet put away: the whole quantity is still open, and the target side is genuinely empty
    // rather than absent.
    [Test]
    public void BuildLines_SourceOnly_ReportsFullQuantityOpen() {
        var result = Build([Line(SourceTarget.Source, 10m, binEntry: 1)]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.Multiple(() => {
            Assert.That(result[0].SourceQuantity, Is.EqualTo(10m));
            Assert.That(result[0].TargetQuantity, Is.EqualTo(0m));
            Assert.That(result[0].OpenQuantity, Is.EqualTo(10m));
            Assert.That(result[0].TargetBins, Is.Empty);
        });
    }

    [Test]
    public void BuildLines_PartiallyPlaced_ReportsRemainderOpen() {
        var result = Build([
            Line(SourceTarget.Source, 10m, binEntry: 1),
            Line(SourceTarget.Target, 4m, binEntry: 2)
        ]);

        Assert.Multiple(() => {
            Assert.That(result[0].SourceQuantity, Is.EqualTo(10m));
            Assert.That(result[0].TargetQuantity, Is.EqualTo(4m));
            Assert.That(result[0].OpenQuantity, Is.EqualTo(6m));
        });
    }

    [Test]
    public void BuildLines_FullyPlaced_ReportsNothingOpen() {
        var result = Build([
            Line(SourceTarget.Source, 10m, binEntry: 1),
            Line(SourceTarget.Target, 10m, binEntry: 2)
        ]);

        Assert.That(result[0].OpenQuantity, Is.EqualTo(0m));
    }

    // An item scanned out of two bins and into two others: each side collapses per bin, and the two sides stay
    // separate. Getting this wrong would show stock as coming from where it went.
    [Test]
    public void BuildLines_MultipleBinsBothSides_AggregatesPerBinPerSide() {
        var bins = new Dictionary<int, string> {
            [1] = "01-A1-10-04", [2] = "01-A1-10-05", [3] = "02-B3-01-02", [4] = "02-B3-01-03"
        };

        var result = Build([
            Line(SourceTarget.Source, 6m, binEntry: 1),
            Line(SourceTarget.Source, 2m, binEntry: 1),
            Line(SourceTarget.Source, 4m, binEntry: 2),
            Line(SourceTarget.Target, 9m, binEntry: 3),
            Line(SourceTarget.Target, 3m, binEntry: 4)
        ], bins: bins);

        Assert.Multiple(() => {
            Assert.That(result[0].SourceQuantity, Is.EqualTo(12m));
            Assert.That(result[0].TargetQuantity, Is.EqualTo(12m));

            Assert.That(result[0].SourceBins.Select(b => b.Code), Is.EqualTo(new[] {"01-A1-10-04", "01-A1-10-05"}));
            Assert.That(result[0].SourceBins[0].Quantity, Is.EqualTo(8m), "the two lines in bin 1 collapse into one row");
            Assert.That(result[0].SourceBins[1].Quantity, Is.EqualTo(4m));

            Assert.That(result[0].TargetBins.Select(b => b.Code), Is.EqualTo(new[] {"02-B3-01-02", "02-B3-01-03"}));
            Assert.That(result[0].TargetBins[0].Quantity, Is.EqualTo(9m));
        });
    }

    [Test]
    public void BuildLines_MultipleItems_OrdersByItemCode() {
        var result = Build([
            Line(SourceTarget.Source, 1m, itemCode: "ITM-002"),
            Line(SourceTarget.Source, 1m, itemCode: "ITM-001")
        ], Catalog(Item("ITM-001"), Item("ITM-002")));

        Assert.That(result.Select(l => l.ItemCode), Is.EqualTo(new[] {"ITM-001", "ITM-002"}));
    }

    // Quantities are decimal precisely so fractional units survive. A report that rounded them would disagree
    // with the stock it claims to describe.
    [Test]
    public void BuildLines_FractionalQuantities_SumWithoutPrecisionLoss() {
        var result = Build([
            Line(SourceTarget.Source, 0.3m, binEntry: 1),
            Line(SourceTarget.Target, 0.1m, binEntry: 2),
            Line(SourceTarget.Target, 0.2m, binEntry: 2)
        ]);

        Assert.Multiple(() => {
            Assert.That(result[0].TargetQuantity, Is.EqualTo(0.3m));
            Assert.That(result[0].OpenQuantity, Is.EqualTo(0m), "0.1 + 0.2 must close out 0.3 exactly");
        });
    }

    // More reached the target than left the source. The report must show it rather than clamp at zero — a
    // negative open quantity is the only signal a supervisor gets that the sides disagree.
    [Test]
    public void BuildLines_OverPlaced_ReportsNegativeOpenQuantity() {
        var result = Build([
            Line(SourceTarget.Source, 5m, binEntry: 1),
            Line(SourceTarget.Target, 8m, binEntry: 2)
        ]);

        Assert.That(result[0].OpenQuantity, Is.EqualTo(-3m));
    }

    // Bins are optional on a line (warehouse-to-warehouse transfers without bin locations).
    [Test]
    public void BuildLines_LinesWithoutBins_ReportsQuantitiesAndNoBins() {
        var result = Build([
            Line(SourceTarget.Source, 7m),
            Line(SourceTarget.Target, 7m)
        ]);

        Assert.Multiple(() => {
            Assert.That(result[0].SourceQuantity, Is.EqualTo(7m));
            Assert.That(result[0].SourceBins, Is.Empty);
            Assert.That(result[0].TargetBins, Is.Empty);
        });
    }

    // An item pulled from SAP's catalog after it was scanned must not blank the whole report.
    [Test]
    public void BuildLines_ItemMissingFromCatalog_DegradesToItemCodeAndNeutralFactors() {
        var result = Build([Line(SourceTarget.Source, 3m)], Catalog());

        Assert.Multiple(() => {
            Assert.That(result[0].ItemName, Is.EqualTo("ITM-001"));
            Assert.That(result[0].SourceQuantity, Is.EqualTo(3m));
            Assert.That(result[0].NumInBuy, Is.EqualTo(1m));
            Assert.That(result[0].Factor1, Is.EqualTo(1m));
            Assert.That(result[0].Factor2, Is.EqualTo(1m));
            Assert.That(result[0].Factor3, Is.EqualTo(1m));
            Assert.That(result[0].Factor4, Is.EqualTo(1m));
        });
    }

    [Test]
    public void BuildLines_BinCodeMissingFromLookup_FallsBackToBinEntry() {
        var result = Build([Line(SourceTarget.Source, 3m, binEntry: 77)]);

        Assert.That(result[0].SourceBins[0].Code, Is.EqualTo("77"));
    }

    [Test]
    public void BuildLines_NoLines_ReturnsEmpty() {
        Assert.That(Build([]), Is.Empty);
    }
}

/// <summary>
/// Real bins are letter-based and inconsistently zero-padded (01-A1-10-04), so ordinal ordering puts
/// 01-A1-10-40 ahead of 01-A1-10-4 and a supervisor reads the bins in an order the warehouse doesn't have.
/// </summary>
[TestFixture]
public class NaturalStringComparerTests {
    private static int Compare(string a, string b) => NaturalStringComparer.Instance.Compare(a, b);

    [Test]
    public void Compare_EmbeddedNumbers_OrdersByValueNotDigit() {
        Assert.That(Compare("01-A1-10-4", "01-A1-10-40"), Is.LessThan(0));
    }

    [Test]
    public void Compare_ZeroPadded_OrdersByValue() {
        Assert.That(Compare("01-A1-10-04", "01-A1-10-10"), Is.LessThan(0));
    }

    [Test]
    public void Compare_DiffersByLetter_OrdersByLetter() {
        Assert.That(Compare("01-A1-10-04", "01-B1-10-04"), Is.LessThan(0));
    }

    [Test]
    public void Compare_Identical_ReturnsZero() {
        Assert.That(Compare("01-A1-10-04", "01-A1-10-04"), Is.EqualTo(0));
    }

    [Test]
    public void Compare_SortsAWholeAisleIntoWalkOrder() {
        string[] bins = ["01-A1-10-40", "01-A1-2-1", "01-A1-10-04", "01-A1-1-1"];

        Array.Sort(bins, NaturalStringComparer.Instance);

        Assert.That(bins, Is.EqualTo(new[] {"01-A1-1-1", "01-A1-2-1", "01-A1-10-04", "01-A1-10-40"}));
    }

    // Same bin value spelled two ways can't happen within one warehouse, but the comparer still owes callers a
    // total order rather than one that depends on the sort's stability.
    [Test]
    public void Compare_SameValueDifferentPadding_OrdersShorterRunFirst() {
        Assert.That(Compare("01-A1-10-4", "01-A1-10-04"), Is.LessThan(0));
    }

    [Test]
    public void Compare_Nulls_SortFirstAndCompareEqualToEachOther() {
        Assert.Multiple(() => {
            Assert.That(NaturalStringComparer.Instance.Compare(null, "01-A1"), Is.LessThan(0));
            Assert.That(NaturalStringComparer.Instance.Compare("01-A1", null), Is.GreaterThan(0));
            Assert.That(NaturalStringComparer.Instance.Compare(null, null), Is.EqualTo(0));
        });
    }
}
