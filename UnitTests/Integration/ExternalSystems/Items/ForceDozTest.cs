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
/// Options.ForceDoz substitutes a dozen factor of 12 where SAP reports NumInBuy = 1 <b>on a unit that calls
/// itself a dozen</b>. The label is half the rule: a factor of 1 is correct for a piece unit (PZA, EA, blank),
/// and rewriting those inflates their stock twelvefold — including Pack scans, which multiply through NumInBuy.
///
/// Real master data in this company: 4 items are 'Doz' with factor 1 (the ones to fix), 29 are 'Doz' with a
/// real factor, and 83 carry factor 1 on a non-dozen label (PZA / 'PZA.' / blank / NULL) and must not move.
/// Each item below stands for one of those groups; the override is applied in SQL, so these run the real
/// queries against SAP rather than asserting on a generated string (UomQueryHelperTests covers that).
/// </summary>
public class ForceDozTest : BaseExternalTest {
    private const decimal BrokenFactor = 1m;
    private const decimal RealFactor = 6m;
    private const decimal SubstitutedFactor = 12m;

    private SboDatabaseService databaseService;

    /// 'Doz' + factor 1 — misconfigured; the item ForceDoz exists for.
    private string dozenBrokenItem = string.Empty;

    /// 'Doz' + factor 6 — a real conversion SAP already has.
    private string dozenRealItem = string.Empty;

    /// 'PZA' + factor 1 — a piece unit, where 1 is the *correct* factor and must survive untouched.
    private string pieceItem = string.Empty;

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

    [Test, Order(0)]
    public async Task PrepareData() {
        var helper = new CreateTestItem(sboCompany);

        dozenBrokenItem = (await helper.Execute(itemsPerUnit: (int)BrokenFactor, unit: "Doz")).ItemCode;
        dozenRealItem = (await helper.Execute(itemsPerUnit: (int)RealFactor, unit: "Doz")).ItemCode;
        pieceItem = (await helper.Execute(itemsPerUnit: (int)BrokenFactor, unit: "PZA")).ItemCode;

        await TestContext.Out.WriteLineAsync($"'Doz' factor 1 (misconfigured): {dozenBrokenItem}");
        await TestContext.Out.WriteLineAsync($"'Doz' factor 6 (correct):       {dozenRealItem}");
        await TestContext.Out.WriteLineAsync($"'PZA' factor 1 (correct):       {pieceItem}");
    }

    // --- The label guard: the regression for the bug this override originally shipped with ---

    [Test, Order(1)]
    public async Task ItemCheck_ForceDozOn_LeavesAPieceUnitAtOne() {
        var item = (await RepositoryWith(true).ItemCheckAsync(pieceItem, null)).Single();

        Assert.That(item.NumInBuy, Is.EqualTo(BrokenFactor),
            "a factor of 1 is correct for a piece unit; rewriting it to 12 inflates the item's stock twelvefold");
    }

    [Test, Order(2)]
    public async Task ScanningAPieceItem_ForceDozOn_IsCompletelyUnaffected() {
        var off = (await RepositoryWith(false).ItemCheckAsync(pieceItem, null)).Single();
        var on = (await RepositoryWith(true).ItemCheckAsync(pieceItem, null)).Single();

        foreach (var unit in new[] { UnitType.Unit, UnitType.Dozen, UnitType.Pack }) {
            decimal expected = GoodsReceiptQuantityCalculator.ToBaseQuantity(9m, unit, off, applyFactor: false);
            decimal actual = GoodsReceiptQuantityCalculator.ToBaseQuantity(9m, unit, on, applyFactor: false);

            Assert.That(actual, Is.EqualTo(expected), $"a {unit} scan of a piece item must not change when ForceDoz is on");
        }
    }

    // --- The intended substitution ---

    [Test, Order(3)]
    public async Task ItemCheck_ForceDozOff_LeavesTheBrokenFactorAtOne() {
        var item = (await RepositoryWith(false).ItemCheckAsync(dozenBrokenItem, null)).Single();

        Assert.That(item.NumInBuy, Is.EqualTo(BrokenFactor));
    }

    [Test, Order(4)]
    public async Task ItemCheck_ForceDozOn_SubstitutesTwelveForADozenLabelledBrokenFactor() {
        var item = (await RepositoryWith(true).ItemCheckAsync(dozenBrokenItem, null)).Single();

        Assert.That(item.NumInBuy, Is.EqualTo(SubstitutedFactor));
    }

    [Test, Order(5)]
    public async Task ItemCheck_ForceDozOn_LeavesARealFactorUntouched() {
        var item = (await RepositoryWith(true).ItemCheckAsync(dozenRealItem, null)).Single();

        Assert.That(item.NumInBuy, Is.EqualTo(RealFactor), "a factor SAP actually has must never be overridden");
    }

    [Test, Order(6)]
    public async Task ItemCheck_ByBarcode_ForceDozOn_AppliesTheSameRule() {
        // Different query (OBCD join, OITM-qualified columns) than the item-code lookup above.
        var dozen = (await RepositoryWith(true).ItemCheckAsync(null, dozenBrokenItem)).Single();
        var piece = (await RepositoryWith(true).ItemCheckAsync(null, pieceItem)).Single();

        Assert.That(dozen.NumInBuy, Is.EqualTo(SubstitutedFactor));
        Assert.That(piece.NumInBuy, Is.EqualTo(BrokenFactor));
    }

    // --- The other item reads, each a differently-shaped query ---

    [Test, Order(7)]
    public async Task GetItemValidationInfo_ForceDozOn_AppliesTheSameRule() {
        // T1-qualified query; the read behind picking/counting/transfer stock and open-quantity checks.
        var dozen = await RepositoryWith(true).GetItemValidationInfo(dozenBrokenItem, null, TestConstants.Warehouse, null, false);
        var piece = await RepositoryWith(true).GetItemValidationInfo(pieceItem, null, TestConstants.Warehouse, null, false);
        var real = await RepositoryWith(true).GetItemValidationInfo(dozenRealItem, null, TestConstants.Warehouse, null, false);

        Assert.That(dozen.NumInBuy, Is.EqualTo(SubstitutedFactor));
        Assert.That(piece.NumInBuy, Is.EqualTo(BrokenFactor));
        Assert.That(real.NumInBuy, Is.EqualTo(RealFactor));
    }

    [Test, Order(8)]
    public async Task GetItemPurchaseUnits_ForceDozOn_AppliesTheSameRule() {
        // Unqualified query.
        var dozen = await RepositoryWith(true).GetItemPurchaseUnits(dozenBrokenItem);
        var piece = await RepositoryWith(true).GetItemPurchaseUnits(pieceItem);

        Assert.That(dozen.QuantityInUnit, Is.EqualTo(SubstitutedFactor));
        Assert.That(piece.QuantityInUnit, Is.EqualTo(BrokenFactor));
    }

    [Test, Order(9)]
    public async Task BulkItemCheck_ForceDozOn_AppliesTheSameRule() {
        var results = await RepositoryWith(true).BulkItemCheckAsync([dozenBrokenItem, pieceItem, dozenRealItem]);

        Assert.That(results[dozenBrokenItem].NumInBuy, Is.EqualTo(SubstitutedFactor));
        Assert.That(results[pieceItem].NumInBuy, Is.EqualTo(BrokenFactor));
        Assert.That(results[dozenRealItem].NumInBuy, Is.EqualTo(RealFactor));
    }

    // NOTE: ItemsWarehouseStockAsync (the OITM x OITW query) is not value-asserted here — it inner-joins
    // OITW, so a freshly created item with no warehouse row returns nothing, and this environment cannot
    // post the documents that would create one. Its CASE expression is identical to the reads above and
    // ForceDozAllFlowsTest proves it executes; that is the honest limit of the coverage.

    // --- What the operator actually sees: the scan-to-base conversion ---

    [Test, Order(11)]
    public async Task ScanningTwoDozen_ForceDozOff_CountsTwoUnits() {
        var item = (await RepositoryWith(false).ItemCheckAsync(dozenBrokenItem, null)).Single();

        // applyFactor: false isolates the dozen conversion from the unrelated PurFactor1..4 multipliers.
        decimal baseQuantity = GoodsReceiptQuantityCalculator.ToBaseQuantity(2m, UnitType.Dozen, item, applyFactor: false);

        Assert.That(baseQuantity, Is.EqualTo(2m), "the bug being fixed: a dozen scan counts as a single unit");
    }

    [Test, Order(12)]
    public async Task ScanningTwoDozen_ForceDozOn_CountsTwentyFourUnits() {
        var item = (await RepositoryWith(true).ItemCheckAsync(dozenBrokenItem, null)).Single();

        decimal baseQuantity = GoodsReceiptQuantityCalculator.ToBaseQuantity(2m, UnitType.Dozen, item, applyFactor: false);

        Assert.That(baseQuantity, Is.EqualTo(24m));
    }

    [Test, Order(13)]
    public async Task ScanningAPack_ForceDozOn_MultipliesThroughTheSubstitutedFactor() {
        var item = (await RepositoryWith(true).ItemCheckAsync(dozenBrokenItem, null)).Single();

        decimal baseQuantity = GoodsReceiptQuantityCalculator.ToBaseQuantity(9m, UnitType.Pack, item, applyFactor: false);

        // Pack = qty x NumInBuy x PurPackUn, so the substitution reaches box scans too. Intended for a real
        // dozen item (a box holds PurPackUn dozens); catastrophic for a piece item, hence the label guard.
        Assert.That(baseQuantity, Is.EqualTo(9m * SubstitutedFactor * item.PurPackUn));
    }

    [Test, Order(14)]
    public async Task ScanningTwoUnits_ForceDozOn_IsUnaffected() {
        var item = (await RepositoryWith(true).ItemCheckAsync(dozenBrokenItem, null)).Single();

        decimal baseQuantity = GoodsReceiptQuantityCalculator.ToBaseQuantity(2m, UnitType.Unit, item, applyFactor: false);

        Assert.That(baseQuantity, Is.EqualTo(2m), "a base-unit scan never applies the dozen factor");
    }
}
