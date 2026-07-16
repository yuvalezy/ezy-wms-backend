using Adapters.Common.SBO.Repositories;
using Adapters.Common.SBO.Services;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.Items;

/// <summary>
/// Options.ForceDoz turns a degenerate middle rung (NumInBuy = 1 — one piece per piece, so the operator has
/// no dozen to pick in) into a real dozen of 12, and divides the pack to match so the pack keeps holding the
/// pieces it always held. The pack compensation is the whole point: raising NumInBuy alone multiplies every
/// box by twelve.
///
/// Modelled on the real ladder in this company. PZA_TEST is the shape being fixed: NumInBuy 1, PurPackUn 72
/// (a 72-piece Caja), which must become NumInBuy 12 / PurPackUn 6 — still a 72-piece Caja, now 6 dozens.
/// BOX1 is the shape that must never move: NumInBuy 12, PurPackUn 4, a 48-piece box of 4 dozens.
///
/// The substitution happens in SQL, so these run the real queries against SAP rather than asserting on a
/// generated string (UomQueryHelperTests covers that).
/// </summary>
public class ForceDozTest : BaseExternalTest {
    private const decimal SubstitutedFactor = 12m;

    private SboDatabaseService databaseService;

    /// NumInBuy 1, PurPackUn 72: a 72-piece pack, convertible to 12 x 6. The PZA_TEST shape.
    private string convertibleItem = string.Empty;
    private const decimal ConvertiblePack = 72m;

    /// NumInBuy 1, PurPackUn 4: a 4-piece pack, not a whole dozen — must stay exactly as SAP describes it.
    private string fractionalPackItem = string.Empty;

    /// NumInBuy 12, PurPackUn 4: already correct (the BOX1 shape) — must never move.
    private string realFactorItem = string.Empty;

    [OneTimeSetUp]
    public new void OneTimeSetUp() {
        base.OneTimeSetUp();

        using var scope = factory.Services.CreateScope();
        databaseService = new SboDatabaseService(scope.ServiceProvider.GetRequiredService<IConfiguration>());
    }

    /// A copy of the live settings with only ForceDoz flipped, so the queries keep their production shape
    /// (custom fields and filters included) and the flag is the single variable under test.
    private ISettings SettingsWith(bool forceDoz) => new Settings {
        Options = settings.Options with { ForceDoz = forceDoz },
        Filters = settings.Filters,
        CustomFields = settings.CustomFields,
        Item = settings.Item
    };

    private SboItemRepository RepositoryWith(bool forceDoz) => new(databaseService, SettingsWith(forceDoz));

    private async Task<Core.DTOs.Items.ItemCheckResponse> Read(string itemCode, bool forceDoz) =>
        (await RepositoryWith(forceDoz).ItemCheckAsync(itemCode, null)).Single();

    /// Base pieces in one pack — the physical quantity that must survive the substitution.
    private static decimal PackPieces(Core.DTOs.Items.ItemResponse item) => item.NumInBuy * item.PurPackUn;

    [Test, Order(0)]
    public async Task PrepareData() {
        var helper = new CreateTestItem(sboCompany);

        convertibleItem = (await helper.Execute(itemsPerUnit: 1, unit: "PZA", qtyPerPackUnit: 72)).ItemCode;
        fractionalPackItem = (await helper.Execute(itemsPerUnit: 1, unit: "PZA", qtyPerPackUnit: 4)).ItemCode;
        realFactorItem = (await helper.Execute(itemsPerUnit: 12, unit: "Doz", qtyPerPackUnit: 4)).ItemCode;

        await TestContext.Out.WriteLineAsync($"convertible   (1 x 72, PZA_TEST shape): {convertibleItem}");
        await TestContext.Out.WriteLineAsync($"fractional pack (1 x 4):                {fractionalPackItem}");
        await TestContext.Out.WriteLineAsync($"already correct (12 x 4, BOX1 shape):   {realFactorItem}");
    }

    // --- The conversion ---

    [Test, Order(1)]
    public async Task ForceDozOff_LeavesTheDegenerateLadderExactlyAsSapDescribesIt() {
        var item = await Read(convertibleItem, false);

        Assert.Multiple(() => {
            Assert.That(item.NumInBuy, Is.EqualTo(1m));
            Assert.That(item.PurPackUn, Is.EqualTo(ConvertiblePack));
            Assert.That(item.BuyUnitMsr, Is.EqualTo("PZA"));
        });
    }

    [Test, Order(2)]
    public async Task ForceDozOn_RaisesTheMiddleRungToADozen() {
        var item = await Read(convertibleItem, true);

        Assert.That(item.NumInBuy, Is.EqualTo(SubstitutedFactor));
    }

    [Test, Order(3)]
    public async Task ForceDozOn_DividesThePackSoTheBoxHoldsTheSamePieces() {
        var item = await Read(convertibleItem, true);

        Assert.Multiple(() => {
            Assert.That(item.PurPackUn, Is.EqualTo(6m), "72 pieces expressed in dozens is 6");
            Assert.That(PackPieces(item), Is.EqualTo(ConvertiblePack), "the box must still hold 72 pieces");
        });
    }

    [Test, Order(4)]
    public async Task ForceDozOn_PackPiecesAreIdenticalWithTheToggleOnAndOff() {
        // The invariant that was violated: a 72-piece box became 864 when only NumInBuy moved.
        var off = await Read(convertibleItem, false);
        var on = await Read(convertibleItem, true);

        Assert.That(PackPieces(on), Is.EqualTo(PackPieces(off)));
    }

    [Test, Order(5)]
    public async Task ForceDozOn_RelabelsTheConvertedRung() {
        var item = await Read(convertibleItem, true);

        Assert.That(item.BuyUnitMsr, Is.EqualTo("Doz"), "a rung that counts twelve must not read 'PZA'");
    }

    // --- What must not move ---

    [Test, Order(6)]
    public async Task ForceDozOn_LeavesAFractionalPackAlone() {
        // A 4-piece pack is not a third of a dozen; dividing would drift (12 x 0.333333 = 3.999996) and make
        // the rung larger than the pack containing it.
        var item = await Read(fractionalPackItem, true);

        Assert.Multiple(() => {
            Assert.That(item.NumInBuy, Is.EqualTo(1m));
            Assert.That(item.PurPackUn, Is.EqualTo(4m));
            Assert.That(item.BuyUnitMsr, Is.EqualTo("PZA"));
            Assert.That(PackPieces(item), Is.EqualTo(4m));
        });
    }

    [Test, Order(7)]
    public async Task ForceDozOn_LeavesACorrectlyConfiguredItemAlone() {
        var item = await Read(realFactorItem, true);

        Assert.Multiple(() => {
            Assert.That(item.NumInBuy, Is.EqualTo(12m));
            Assert.That(item.PurPackUn, Is.EqualTo(4m));
            Assert.That(item.BuyUnitMsr, Is.EqualTo("Doz"));
            Assert.That(PackPieces(item), Is.EqualTo(48m), "the BOX1 shape: a 48-piece box of 4 dozens");
        });
    }

    // --- The same rule through every other item read ---

    [Test, Order(8)]
    public async Task ByBarcode_AppliesTheSameRule() {
        var item = (await RepositoryWith(true).ItemCheckAsync(null, convertibleItem)).Single();

        Assert.Multiple(() => {
            Assert.That(item.NumInBuy, Is.EqualTo(SubstitutedFactor));
            Assert.That(item.PurPackUn, Is.EqualTo(6m));
            Assert.That(item.BuyUnitMsr, Is.EqualTo("Doz"));
        });
    }

    [Test, Order(9)]
    public async Task GetItemValidationInfo_AppliesTheSameRule() {
        // T1-qualified query; the read behind picking/counting/transfer stock and open-quantity checks.
        var converted = await RepositoryWith(true).GetItemValidationInfo(convertibleItem, null, TestConstants.Warehouse, null, false);
        var fractional = await RepositoryWith(true).GetItemValidationInfo(fractionalPackItem, null, TestConstants.Warehouse, null, false);

        Assert.Multiple(() => {
            Assert.That(converted.NumInBuy, Is.EqualTo(SubstitutedFactor));
            Assert.That(converted.NumInBuy * converted.PurPackUn, Is.EqualTo(ConvertiblePack));
            Assert.That(fractional.NumInBuy, Is.EqualTo(1m));
            Assert.That(fractional.PurPackUn, Is.EqualTo(4m));
        });
    }

    [Test, Order(10)]
    public async Task GetItemPurchaseUnits_AppliesTheSameRule() {
        var result = await RepositoryWith(true).GetItemPurchaseUnits(convertibleItem);

        Assert.Multiple(() => {
            Assert.That(result.QuantityInUnit, Is.EqualTo(SubstitutedFactor));
            Assert.That(result.QuantityInPack, Is.EqualTo(6m));
            Assert.That(result.QuantityInUnit * result.QuantityInPack, Is.EqualTo(ConvertiblePack));
            Assert.That(result.UnitMeasure, Is.EqualTo("Doz"));
        });
    }

    [Test, Order(11)]
    public async Task BulkItemCheck_AppliesTheSameRule() {
        var results = await RepositoryWith(true).BulkItemCheckAsync([convertibleItem, fractionalPackItem, realFactorItem]);

        Assert.Multiple(() => {
            Assert.That(PackPieces(results[convertibleItem]), Is.EqualTo(ConvertiblePack));
            Assert.That(results[convertibleItem].NumInBuy, Is.EqualTo(SubstitutedFactor));
            Assert.That(results[fractionalPackItem].NumInBuy, Is.EqualTo(1m));
            Assert.That(results[realFactorItem].NumInBuy, Is.EqualTo(12m));
        });
    }

    // --- What the operator actually sees ---

    [Test, Order(12)]
    public async Task ScanningTwoDozen_ForceDozOff_CountsTwoPieces() {
        var item = await Read(convertibleItem, false);

        // applyFactor: false isolates the ladder from the unrelated PurFactor1..4 multipliers.
        decimal quantity = GoodsReceiptQuantityCalculator.ToBaseQuantity(2m, UnitType.Dozen, item, applyFactor: false);

        Assert.That(quantity, Is.EqualTo(2m), "the bug being fixed: the middle rung is a single piece");
    }

    [Test, Order(13)]
    public async Task ScanningTwoDozen_ForceDozOn_CountsTwentyFourPieces() {
        var item = await Read(convertibleItem, true);

        decimal quantity = GoodsReceiptQuantityCalculator.ToBaseQuantity(2m, UnitType.Dozen, item, applyFactor: false);

        Assert.That(quantity, Is.EqualTo(24m));
    }

    [Test, Order(14)]
    public async Task ScanningOneBox_CountsTheSamePiecesWhicheverWayTheToggleIsSet() {
        var off = await Read(convertibleItem, false);
        var on = await Read(convertibleItem, true);

        decimal boxOff = GoodsReceiptQuantityCalculator.ToBaseQuantity(1m, UnitType.Pack, off, applyFactor: false);
        decimal boxOn = GoodsReceiptQuantityCalculator.ToBaseQuantity(1m, UnitType.Pack, on, applyFactor: false);

        Assert.Multiple(() => {
            Assert.That(boxOff, Is.EqualTo(ConvertiblePack));
            Assert.That(boxOn, Is.EqualTo(ConvertiblePack), "scanning a box must not change because dozens were enabled");
        });
    }

    [Test, Order(15)]
    public async Task ScanningABaseUnit_IsNeverAffected() {
        var item = await Read(convertibleItem, true);

        decimal quantity = GoodsReceiptQuantityCalculator.ToBaseQuantity(5m, UnitType.Unit, item, applyFactor: false);

        Assert.That(quantity, Is.EqualTo(5m));
    }

    // The screen that started this: SO 503 wants 96 pieces of a 72-piece-Caja item, and must read
    // "1 Caja, 2 Doz" — not "1 Caja, 24 PZA".
    [Test, Order(16)]
    public async Task NinetySixPieces_BreakDownAsOneBoxAndTwoDozen() {
        var item = await Read(convertibleItem, true);

        decimal packPieces = PackPieces(item);
        decimal boxes = Math.Floor(96m / packPieces);
        decimal remainder = 96m - boxes * packPieces;
        decimal dozens = Math.Floor(remainder / item.NumInBuy);
        decimal singles = remainder - dozens * item.NumInBuy;

        Assert.Multiple(() => {
            Assert.That(boxes, Is.EqualTo(1m));
            Assert.That(dozens, Is.EqualTo(2m));
            Assert.That(singles, Is.EqualTo(0m));
            Assert.That(item.BuyUnitMsr, Is.EqualTo("Doz"));
        });
    }
}
