using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.Items;

/// <summary>
/// Picking freezes the conversion factor into PickList.Quantity at scan time, so toggling Options.ForceDoz
/// (or a factor being corrected in SAP) leaves open lines holding a stale base. PickListService.GetPickList
/// repairs those lines when a pick list is opened, re-deriving each from its stored scanned count.
///
/// The item factors come from the real SAP database — that is the whole point, since the substitution
/// happens in SQL. The pick rows live in an in-memory database so the test never touches real pick data.
/// </summary>
public class PickListSelfHealTest : BaseExternalTest {
    private const int TestAbsEntry = -999;

    private IExternalSystemAdapter adapter;
    private ILoggerFactory loggerFactory;
    private Core.Models.Settings.Options originalOptions;

    /// Degenerate rung with a whole-dozen pack (1 x 72): ForceDoz converts it to 12 x 6.
    private string convertibleItem = string.Empty;

    /// Degenerate rung with a fractional pack (1 x 4): not a whole dozen, so ForceDoz leaves it alone.
    private string fractionalPackItem = string.Empty;

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

    private PickListQuantityRepairService RepairServiceFor(SystemDbContext db) =>
        new(db, adapter, loggerFactory.CreateLogger<PickListQuantityRepairService>());

    private static PickList Line(string itemCode, UnitType unit, decimal? scanned, decimal storedBase) => new() {
        Id = Guid.NewGuid(),
        AbsEntry = TestAbsEntry,
        PickEntry = 0,
        ItemCode = itemCode,
        Unit = unit,
        ScannedQuantity = scanned,
        Quantity = storedBase,
        Status = ObjectStatus.Open,
        SyncStatus = SyncStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    private async Task<PickList[]> Repair(bool forceDoz, params PickList[] lines) {
        SetForceDoz(forceDoz);

        await using var db = NewDb();
        await db.PickLists.AddRangeAsync(lines);
        await db.SaveChangesAsync();

        await RepairServiceFor(db).RepairAsync(lines);

        return lines;
    }

    [Test, Order(0)]
    public async Task PrepareData() {
        var helper = new CreateTestItem(sboCompany);
        convertibleItem = (await helper.Execute(itemsPerUnit: 1, unit: "PZA", qtyPerPackUnit: 72)).ItemCode;
        fractionalPackItem = (await helper.Execute(itemsPerUnit: 1, unit: "PZA", qtyPerPackUnit: 4)).ItemCode;
    }

    // --- The point of the feature: a line scanned before the toggle heals when the pick list is opened ---

    [Test, Order(1)]
    public async Task LineScannedBeforeForceDoz_IsRestatedWhenTheListIsOpened() {
        // 2 dozen scanned while the factor was 1, so the frozen base is 2.
        var line = Line(convertibleItem, UnitType.Dozen, scanned: 2m, storedBase: 2m);

        await Repair(forceDoz: true, line);

        Assert.That(line.Quantity, Is.EqualTo(24m), "2 dozen at the corrected factor is 24 base");
    }

    [Test, Order(2)]
    public async Task LineScannedUnderForceDoz_HealsBackWhenTheToggleIsTurnedOff() {
        // Restating must work in both directions, or turning the toggle off would strand inflated lines.
        var line = Line(convertibleItem, UnitType.Dozen, scanned: 2m, storedBase: 24m);

        await Repair(forceDoz: false, line);

        Assert.That(line.Quantity, Is.EqualTo(2m));
    }

    [Test, Order(3)]
    public async Task PackLine_IsNotRestated_BecauseTheBoxStillHoldsTheSamePieces() {
        // 3 boxes of a 72-piece box scanned with the toggle off: 3 x 1 x 72 = 216 base.
        var line = Line(convertibleItem, UnitType.Pack, scanned: 3m, storedBase: 216m);

        await Repair(forceDoz: true, line);

        // With the toggle on the ladder is 12 x 6, so the same 3 boxes are still 3 x 12 x 6 = 216. Enabling
        // dozens must never restate a box line — that restatement was the twelvefold inflation bug.
        Assert.That(line.Quantity, Is.EqualTo(216m));
    }

    // --- What must NOT move ---

    [Test, Order(4)]
    public async Task FractionalPackLine_IsNeverRestated() {
        var line = Line(fractionalPackItem, UnitType.Dozen, scanned: 9m, storedBase: 9m);

        await Repair(forceDoz: true, line);

        Assert.That(line.Quantity, Is.EqualTo(9m), "a 4-piece pack is not a whole dozen, so this ladder never converts");
    }

    [Test, Order(5)]
    public async Task LegacyLineWithNoScannedCount_IsLeftAlone() {
        // Rows created before the ScannedQuantity column existed have no truth to re-derive from.
        var line = Line(convertibleItem, UnitType.Dozen, scanned: null, storedBase: 2m);

        await Repair(forceDoz: true, line);

        Assert.That(line.Quantity, Is.EqualTo(2m));
    }

    [Test, Order(6)]
    public async Task BaseUnitLine_IsNeverRestated() {
        var line = Line(convertibleItem, UnitType.Unit, scanned: 5m, storedBase: 5m);

        await Repair(forceDoz: true, line);

        Assert.That(line.Quantity, Is.EqualTo(5m), "a base-unit scan never applies the dozen factor");
    }

    [Test, Order(7)]
    public async Task RepairIsIdempotent_AndReportsNothingToDoWhenTheFactorIsUnchanged() {
        SetForceDoz(true);
        var line = Line(convertibleItem, UnitType.Dozen, scanned: 2m, storedBase: 2m);

        await using var db = NewDb();
        await db.PickLists.AddAsync(line);
        await db.SaveChangesAsync();
        var service = RepairServiceFor(db);

        int first = await service.RepairAsync([line]);
        int second = await service.RepairAsync([line]);

        Assert.Multiple(() => {
            Assert.That(first, Is.EqualTo(1), "the first pass restates the stale line");
            Assert.That(second, Is.EqualTo(0), "a second pass must be a no-op, or every open would rewrite rows");
            Assert.That(line.Quantity, Is.EqualTo(24m));
        });
    }

    [Test, Order(8)]
    public async Task RepairPersistsTheCorrectedQuantity() {
        SetForceDoz(true);
        var line = Line(convertibleItem, UnitType.Dozen, scanned: 2m, storedBase: 2m);

        await using var db = NewDb();
        await db.PickLists.AddAsync(line);
        await db.SaveChangesAsync();

        await RepairServiceFor(db).RepairAsync([line]);

        // The database-side aggregates downstream read the stored column, not the in-memory instance.
        var reloaded = await db.PickLists.AsNoTracking().SingleAsync(p => p.Id == line.Id);
        Assert.That(reloaded.Quantity, Is.EqualTo(24m));
    }

    [Test, Order(9)]
    public async Task MixedLines_AreEachRestatedOnTheirOwnMerits() {
        var dozen = Line(convertibleItem, UnitType.Dozen, scanned: 1m, storedBase: 1m);
        var piece = Line(fractionalPackItem, UnitType.Dozen, scanned: 1m, storedBase: 1m);
        var legacy = Line(convertibleItem, UnitType.Dozen, scanned: null, storedBase: 1m);

        SetForceDoz(true);
        await using var db = NewDb();
        await db.PickLists.AddRangeAsync(dozen, piece, legacy);
        await db.SaveChangesAsync();

        int repaired = await RepairServiceFor(db).RepairAsync([dozen, piece, legacy]);

        Assert.Multiple(() => {
            Assert.That(repaired, Is.EqualTo(1), "only the dozen-labelled line with a scanned count changes");
            Assert.That(dozen.Quantity, Is.EqualTo(12m));
            Assert.That(piece.Quantity, Is.EqualTo(1m));
            Assert.That(legacy.Quantity, Is.EqualTo(1m));
        });
    }
}
