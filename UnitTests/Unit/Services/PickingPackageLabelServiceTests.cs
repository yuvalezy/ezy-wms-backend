using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Unit.Services;

/// <summary>
/// A picking package label reports what is physically in the box. Post-pick repack runs entirely on rows
/// that picking has already closed, so a label response that counted only open rows described every label on
/// a finished pick list as empty.
/// </summary>
[TestFixture]
public class PickingPackageLabelServiceTests {
    private static SystemDbContext NewDb() =>
        new(new DbContextOptionsBuilder<SystemDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ISettings Settings() =>
        new Settings { Options = new Options { EnablePickingPackageLabels = true } };

    private static PickList Row(Guid labelId, UnitType unit, decimal scanned, decimal baseQty, ObjectStatus status,
        SyncStatus sync = SyncStatus.Synced) => new() {
        Id = Guid.NewGuid(),
        AbsEntry = 13,
        PickEntry = 0,
        ItemCode = "PZA_TEST",
        Unit = unit,
        ScannedQuantity = scanned,
        Quantity = baseQty,
        Status = status,
        SyncStatus = sync,
        PickingPackageLabelId = labelId,
        CreatedAt = DateTime.UtcNow
    };

    private static async Task<PickingPackageLabelResponse> ListSingle(params PickList[] rows) {
        var labelId = rows[0].PickingPackageLabelId!.Value;
        await using var db = NewDb();

        await db.PickingPackageLabels.AddAsync(new PickingPackageLabel {
            Id = labelId, AbsEntry = 13, WhsCode = "01", Code = "R1", Sequence = 1, CreatedAt = DateTime.UtcNow
        });
        await db.PickLists.AddRangeAsync(rows);
        await db.SaveChangesAsync();

        var service = new PickingPackageLabelService(db, Settings());
        return (await service.ListAsync(13, "01")).Single();
    }

    // The repack bug: a completed pick list's rows are all Closed, so the label read as empty.
    [Test]
    public async Task ListAsync_ClosedRows_AreStillCountedInTheLabel() {
        var labelId = Guid.NewGuid();

        var response = await ListSingle(
            Row(labelId, UnitType.Dozen, scanned: 1m, baseQty: 12m, ObjectStatus.Closed),
            Row(labelId, UnitType.Dozen, scanned: 1m, baseQty: 12m, ObjectStatus.Closed),
            Row(labelId, UnitType.Unit, scanned: 1m, baseQty: 1m, ObjectStatus.Closed),
            Row(labelId, UnitType.Unit, scanned: 1m, baseQty: 1m, ObjectStatus.Closed));

        Assert.Multiple(() => {
            Assert.That(response.LineCount, Is.EqualTo(4), "picked stock is still in the box after the line closes");
            Assert.That(response.Items, Has.Count.EqualTo(2), "one group per unit");
        });
    }

    // 2 dozens and 2 units must stay two distinct groups — collapsing them into one scalar (2 + 2 = 4) is
    // what made the old cross-unit total meaningless.
    [Test]
    public async Task ListAsync_GroupsEachUnitSeparately_WithItsOwnScannedAndBaseQuantity() {
        var labelId = Guid.NewGuid();

        var response = await ListSingle(
            Row(labelId, UnitType.Dozen, scanned: 1m, baseQty: 12m, ObjectStatus.Closed),
            Row(labelId, UnitType.Dozen, scanned: 1m, baseQty: 12m, ObjectStatus.Closed),
            Row(labelId, UnitType.Unit, scanned: 1m, baseQty: 1m, ObjectStatus.Closed),
            Row(labelId, UnitType.Unit, scanned: 1m, baseQty: 1m, ObjectStatus.Closed));

        var dozen = response.Items.Single(i => i.Unit == UnitType.Dozen);
        var unit = response.Items.Single(i => i.Unit == UnitType.Unit);

        Assert.Multiple(() => {
            Assert.That(dozen.ScannedQuantity, Is.EqualTo(2m), "2 dozens scanned");
            Assert.That(dozen.BaseQuantity, Is.EqualTo(24m), "which is 24 pieces");
            Assert.That(unit.ScannedQuantity, Is.EqualTo(2m), "2 singles scanned");
            Assert.That(unit.BaseQuantity, Is.EqualTo(2m));
        });
    }

    [Test]
    public async Task ListAsync_OpenAndProcessingRows_AreStillCounted() {
        var labelId = Guid.NewGuid();

        var response = await ListSingle(
            Row(labelId, UnitType.Dozen, scanned: 1m, baseQty: 12m, ObjectStatus.Open),
            Row(labelId, UnitType.Dozen, scanned: 1m, baseQty: 12m, ObjectStatus.Processing));

        Assert.That(response.LineCount, Is.EqualTo(2));
    }

    [Test]
    public async Task ListAsync_ExternallyCancelledRows_AreExcluded() {
        var labelId = Guid.NewGuid();

        var response = await ListSingle(
            Row(labelId, UnitType.Dozen, scanned: 1m, baseQty: 12m, ObjectStatus.Closed),
            Row(labelId, UnitType.Dozen, scanned: 1m, baseQty: 12m, ObjectStatus.Closed, SyncStatus.ExternalCancel));

        Assert.That(response.LineCount, Is.EqualTo(1), "a line cancelled in SAP is no longer in the box");
    }
}
