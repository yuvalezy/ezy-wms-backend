using Core.DTOs.InventoryCounting;
using Core.Entities;
using Core.Enums;
using Core.Models;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace UnitTests.Unit.Services;

/// <summary>
/// Guards the "own lines only" boundary for the CountingCorrection role: an operator who reaches
/// updateAll solely via CountingCorrection must never edit/remove another operator's scanned line.
/// This is the security boundary — the UI gate is convenience only.
/// </summary>
[TestFixture]
public class InventoryCountingReportServiceOwnLinesTests {
    private static readonly Guid OperatorId   = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId  = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CountingId   = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static SystemDbContext NewDb() =>
        new(new DbContextOptionsBuilder<SystemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    // Adapter is only touched for quantity changes; the remove-row cases below never reach it.
    private static InventoryCountingReportService NewService(SystemDbContext db) => new(db, null!);

    private static Guid SeedLine(SystemDbContext db, Guid createdBy) {
        var line = new InventoryCountingLine {
            Id                  = Guid.NewGuid(),
            InventoryCountingId = CountingId,
            ItemCode            = "A0001",
            Quantity            = 5,
            LineStatus          = LineStatus.Open,
            Unit                = UnitType.Unit,
            Date                = DateTime.UtcNow,
            CreatedByUserId     = createdBy,
        };
        db.InventoryCountingLines.Add(line);
        db.SaveChanges();
        return line.Id;
    }

    private static void SeedCounting(SystemDbContext db) {
        db.InventoryCountings.Add(new InventoryCounting {
            Id      = CountingId,
            Date    = DateTime.UtcNow,
            WhsCode = "01",
            Status  = ObjectStatus.InProgress,
        });
        db.SaveChanges();
    }

    private static SessionInfo Session(Guid userId, params RoleType[] roles) => new() {
        UserId    = userId.ToString(),
        Name      = "Test",
        Warehouse = "01",
        Roles     = roles,
    };

    private static UpdateInventoryCountingAllRequest RemoveRequest(params Guid[] lineIds) => new() {
        Id              = CountingId,
        QuantityChanges = new Dictionary<Guid, decimal>(),
        RemoveRows      = lineIds,
    };

    [Test]
    public async Task UpdateCountingAll_CorrectionUserRemovesOwnLine_Succeeds() {
        using var db = NewDb();
        SeedCounting(db);
        var ownLine = SeedLine(db, OperatorId);

        var result = await NewService(db).UpdateCountingAll(
            RemoveRequest(ownLine),
            Session(OperatorId, RoleType.Counting, RoleType.CountingCorrection));

        Assert.That(result, Is.Null); // null == success
        var stored = await db.InventoryCountingLines.FindAsync(ownLine);
        Assert.That(stored!.Deleted, Is.True);
    }

    [Test]
    public async Task UpdateCountingAll_CorrectionUserRemovesForeignLine_IsRejected() {
        using var db = NewDb();
        SeedCounting(db);
        var foreignLine = SeedLine(db, OtherUserId);

        var result = await NewService(db).UpdateCountingAll(
            RemoveRequest(foreignLine),
            Session(OperatorId, RoleType.Counting, RoleType.CountingCorrection));

        Assert.That(result, Is.Not.Null); // error message returned
        var stored = await db.InventoryCountingLines.FindAsync(foreignLine);
        Assert.That(stored!.Deleted, Is.False); // untouched
    }

    [Test]
    public async Task UpdateCountingAll_SupervisorRemovesForeignLine_Succeeds() {
        using var db = NewDb();
        SeedCounting(db);
        var foreignLine = SeedLine(db, OtherUserId);

        var result = await NewService(db).UpdateCountingAll(
            RemoveRequest(foreignLine),
            Session(OperatorId, RoleType.CountingSupervisor));

        Assert.That(result, Is.Null);
        var stored = await db.InventoryCountingLines.FindAsync(foreignLine);
        Assert.That(stored!.Deleted, Is.True);
    }
}
