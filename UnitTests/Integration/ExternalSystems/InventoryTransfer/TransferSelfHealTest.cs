using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.InventoryTransfer;

/// <summary>
/// Transfer multiplies the scanned count by the conversion factor at scan time and stores only the base in
/// TransferLine.Quantity, so toggling Options.ForceDoz (or a factor being corrected in SAP) leaves open lines
/// holding a stale base. TransferQuantityRepairService restates those lines from their stored scanned count
/// when a transfer is opened — but never at the cost of the per-item source/target balance the posting path
/// enforces. An item scanned in packs on one side and dozens on the other cannot both restate and stay
/// balanced, so the repair reverts that item and leaves it processable on its stored numbers.
///
/// The item factors come from the real SAP database — that is the whole point, since the substitution happens
/// in SQL. The transfer rows live in an in-memory database so the test never touches real transfer data.
/// </summary>
public class TransferSelfHealTest : BaseExternalTest {
    private IExternalSystemAdapter adapter;
    private ILoggerFactory loggerFactory;
    private Core.Models.Settings.Options originalOptions;

    /// Degenerate rung with a whole-dozen pack (1 x 72): ForceDoz converts it to 12 x 6.
    private string convertibleItem = string.Empty;

    /// A second convertible item, so the mixed test can have two independent item groups.
    private string convertibleItemB = string.Empty;

    [OneTimeSetUp]
    public new void OneTimeSetUp() {
        base.OneTimeSetUp();

        using var scope = factory.Services.CreateScope();
        adapter = scope.ServiceProvider.GetRequiredService<IExternalSystemAdapter>();
        loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        originalOptions = settings.Options;
    }

    [OneTimeTearDown]
    public new async Task OneTimeTearDown() {
        settings.Options = originalOptions;
        await base.OneTimeTearDown();
        loggerFactory?.Dispose();
    }

    private void SetForceDoz(bool value) => settings.Options = settings.Options with { ForceDoz = value };

    private static SystemDbContext NewDb() =>
        new(new DbContextOptionsBuilder<SystemDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private TransferQuantityRepairService RepairServiceFor(SystemDbContext db) =>
        new(db, adapter, loggerFactory.CreateLogger<TransferQuantityRepairService>());

    private static TransferLine Line(string itemCode, SourceTarget type, UnitType unit, decimal? scanned, decimal storedBase) => new() {
        Id = Guid.NewGuid(),
        ItemCode = itemCode,
        Type = type,
        UnitType = unit,
        ScannedQuantity = scanned,
        Quantity = storedBase,
        Date = DateTime.UtcNow,
        LineStatus = LineStatus.Open,
        CreatedAt = DateTime.UtcNow
    };

    /// Seeds a transfer and its lines into the given context, wiring the FK, and returns the transfer id.
    private static async Task<Guid> Seed(SystemDbContext db, params TransferLine[] lines) {
        var transfer = new Transfer {
            Id = Guid.NewGuid(),
            WhsCode = "01",
            TargetWhsCode = "02",
            Date = DateTime.UtcNow,
            Status = ObjectStatus.InProgress
        };
        foreach (var line in lines)
            line.TransferId = transfer.Id;

        await db.Transfers.AddAsync(transfer);
        await db.TransferLines.AddRangeAsync(lines);
        await db.SaveChangesAsync();
        return transfer.Id;
    }

    /// Seeds the lines under the given toggle, opens the transfer (repairs), and returns the number changed.
    /// The passed-in line instances are the ones the repair mutates, so callers assert on them directly.
    private async Task<int> Repair(bool forceDoz, params TransferLine[] lines) {
        SetForceDoz(forceDoz);

        await using var db = NewDb();
        var id = await Seed(db, lines);

        return await RepairServiceFor(db).RepairAsync(id);
    }

    [Test, Order(0)]
    public async Task PrepareData() {
        var helper = new CreateTestItem(sboCompany);
        convertibleItem = (await helper.Execute(itemsPerUnit: 1, unit: "PZA", qtyPerPackUnit: 72)).ItemCode;
        convertibleItemB = (await helper.Execute(itemsPerUnit: 1, unit: "PZA", qtyPerPackUnit: 72)).ItemCode;
    }

    // --- The point of the feature: both sides restate together and stay balanced ---

    [Test, Order(1)]
    public async Task BothSidesScannedBeforeForceDoz_AreBothRestated() {
        // 2 dozen scanned on each side while the factor was 1, so both frozen bases are 2. Balanced: 2 == 2.
        var source = Line(convertibleItem, SourceTarget.Source, UnitType.Dozen, scanned: 2m, storedBase: 2m);
        var target = Line(convertibleItem, SourceTarget.Target, UnitType.Dozen, scanned: 2m, storedBase: 2m);

        int repaired = await Repair(forceDoz: true, source, target);

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(2), "both dozen lines restate");
            Assert.That(source.Quantity, Is.EqualTo(24m));
            Assert.That(target.Quantity, Is.EqualTo(24m), "the item stays balanced at the corrected factor");
        });
    }

    // --- The interesting failure mode: a mixed-unit item cannot restate and stay balanced ---

    [Test, Order(2)]
    public async Task PackSourceDozenTarget_IsRevertedToKeepTheItemBalanced() {
        // Scanned while the toggle was off: 1 box out of the source (1 x 1 x 72 = 72 pieces), 72 "dozens" of a
        // degenerate factor-1 item into the target (72 x 1 = 72 pieces). Balanced: 72 == 72.
        var source = Line(convertibleItem, SourceTarget.Source, UnitType.Pack, scanned: 1m, storedBase: 72m);
        var target = Line(convertibleItem, SourceTarget.Target, UnitType.Dozen, scanned: 72m, storedBase: 72m);

        int repaired = await Repair(forceDoz: true, source, target);

        // With the toggle on the box is still 72 (1 x 12 x 6) but the dozen side jumps to 72 x 12 = 864.
        // Restating would leave 72 vs 864, so the whole item is reverted and left on its stored numbers.
        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(0), "nothing is committed — the item is skipped");
            Assert.That(source.Quantity, Is.EqualTo(72m));
            Assert.That(target.Quantity, Is.EqualTo(72m), "reverted, so the transfer stays balanced and processable");
        });
    }

    [Test, Order(3)]
    public async Task SourceOnlyLine_IsRestated_BecauseItWasNeverBalancedAgainstATarget() {
        // A cross-warehouse leg can hold a source line with no matching target. It was not balanced to begin
        // with, so the guard does not apply and the line restates freely.
        var source = Line(convertibleItem, SourceTarget.Source, UnitType.Dozen, scanned: 2m, storedBase: 2m);

        int repaired = await Repair(forceDoz: true, source);

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(1));
            Assert.That(source.Quantity, Is.EqualTo(24m));
        });
    }

    // --- What must NOT move ---

    [Test, Order(4)]
    public async Task LegacyLinesWithNoScannedCount_AreLeftAlone() {
        var source = Line(convertibleItem, SourceTarget.Source, UnitType.Dozen, scanned: null, storedBase: 2m);
        var target = Line(convertibleItem, SourceTarget.Target, UnitType.Dozen, scanned: null, storedBase: 2m);

        int repaired = await Repair(forceDoz: true, source, target);

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(0), "no scanned count means no truth to re-derive from");
            Assert.That(source.Quantity, Is.EqualTo(2m));
            Assert.That(target.Quantity, Is.EqualTo(2m));
        });
    }

    [Test, Order(5)]
    public async Task OneItemReverts_WhileAnIndependentItemRestates() {
        // Item A is the mixed-unit pair that must revert; item B is a clean dozen/dozen pair that must restate.
        // Grouping is per ItemCode, so the revert of A must not hold back B.
        var aSource = Line(convertibleItem, SourceTarget.Source, UnitType.Pack, scanned: 1m, storedBase: 72m);
        var aTarget = Line(convertibleItem, SourceTarget.Target, UnitType.Dozen, scanned: 72m, storedBase: 72m);
        var bSource = Line(convertibleItemB, SourceTarget.Source, UnitType.Dozen, scanned: 2m, storedBase: 2m);
        var bTarget = Line(convertibleItemB, SourceTarget.Target, UnitType.Dozen, scanned: 2m, storedBase: 2m);

        int repaired = await Repair(forceDoz: true, aSource, aTarget, bSource, bTarget);

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(2), "only item B's two lines commit");
            Assert.That(aSource.Quantity, Is.EqualTo(72m), "item A reverted");
            Assert.That(aTarget.Quantity, Is.EqualTo(72m), "item A reverted");
            Assert.That(bSource.Quantity, Is.EqualTo(24m));
            Assert.That(bTarget.Quantity, Is.EqualTo(24m));
        });
    }

    [Test, Order(6)]
    public async Task RepairIsIdempotent_AndReportsNothingToDoWhenTheFactorIsUnchanged() {
        SetForceDoz(true);
        var source = Line(convertibleItem, SourceTarget.Source, UnitType.Dozen, scanned: 2m, storedBase: 2m);
        var target = Line(convertibleItem, SourceTarget.Target, UnitType.Dozen, scanned: 2m, storedBase: 2m);

        await using var db = NewDb();
        var id = await Seed(db, source, target);
        var service = RepairServiceFor(db);

        int first = await service.RepairAsync(id);
        int second = await service.RepairAsync(id);

        Assert.Multiple(() => {
            Assert.That(first, Is.EqualTo(2), "the first pass restates both stale lines");
            Assert.That(second, Is.EqualTo(0), "a second pass must be a no-op, or every open would rewrite rows");
            Assert.That(source.Quantity, Is.EqualTo(24m));
            Assert.That(target.Quantity, Is.EqualTo(24m));
        });
    }

    [Test, Order(7)]
    public async Task RepairPersistsBothSides() {
        SetForceDoz(true);
        var source = Line(convertibleItem, SourceTarget.Source, UnitType.Dozen, scanned: 2m, storedBase: 2m);
        var target = Line(convertibleItem, SourceTarget.Target, UnitType.Dozen, scanned: 2m, storedBase: 2m);

        await using var db = NewDb();
        var id = await Seed(db, source, target);

        await RepairServiceFor(db).RepairAsync(id);

        // The SQL invariant check and the picking bin-on-hand unions read the stored column, not the instances.
        var reloaded = await db.TransferLines.AsNoTracking().Where(l => l.TransferId == id).ToArrayAsync();
        Assert.That(reloaded.Select(l => l.Quantity), Is.EquivalentTo(new[] { 24m, 24m }));
    }
}
