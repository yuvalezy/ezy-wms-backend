using Core.Entities;
using Core.Enums;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Unit.Services;

[TestFixture]
public class PickListValidationServiceGoodsReceiptTests {
    private const string Item = "A0001";
    private const string W1 = "01";
    private const string W2 = "02";

    private static SystemDbContext NewDb() =>
        new(new DbContextOptionsBuilder<SystemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static void SeedReceipt(SystemDbContext db, string itemCode, string whsCode, ObjectStatus status) {
        var receipt = new GoodsReceipt {
            Id = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            WhsCode = whsCode,
            Status = status,
            Lines = {
                new GoodsReceiptLine {
                    Id = Guid.NewGuid(),
                    Date = DateTime.UtcNow,
                    ItemCode = itemCode,
                    LineStatus = LineStatus.Open,
                    Quantity = 5
                }
            }
        };
        db.GoodsReceipts.Add(receipt);
        db.SaveChanges();
    }

    // Service under test only needs the DbContext for this method; adapter is unused here.
    private static PickListValidationService NewService(SystemDbContext db) => new(db, null!);

    [Test]
    public async Task IsItemInInProgressGoodsReceipt_InProgressSameWarehouse_ReturnsTrue() {
        using var db = NewDb();
        SeedReceipt(db, Item, W1, ObjectStatus.InProgress);

        var result = await NewService(db).IsItemInInProgressGoodsReceipt(Item, W1);

        Assert.That(result, Is.True);
    }

    [TestCase(ObjectStatus.Open, true)]
    [TestCase(ObjectStatus.InProgress, true)]
    [TestCase(ObjectStatus.Processing, true)]
    [TestCase(ObjectStatus.PartiallyProcessed, true)]
    [TestCase(ObjectStatus.Finished, false)]
    [TestCase(ObjectStatus.Cancelled, false)]
    [TestCase(ObjectStatus.Closed, false)]
    [TestCase(ObjectStatus.Rejected, false)]
    public async Task IsItemInInProgressGoodsReceipt_BlocksOnlyNonTerminalStatuses(ObjectStatus status, bool expected) {
        using var db = NewDb();
        SeedReceipt(db, Item, W1, status);

        var result = await NewService(db).IsItemInInProgressGoodsReceipt(Item, W1);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task IsItemInInProgressGoodsReceipt_DifferentWarehouse_ReturnsFalse() {
        using var db = NewDb();
        SeedReceipt(db, Item, W2, ObjectStatus.InProgress);

        var result = await NewService(db).IsItemInInProgressGoodsReceipt(Item, W1);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsItemInInProgressGoodsReceipt_ItemNotReceived_ReturnsFalse() {
        using var db = NewDb();
        SeedReceipt(db, "OTHER", W1, ObjectStatus.InProgress);

        var result = await NewService(db).IsItemInInProgressGoodsReceipt(Item, W1);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task FindItemInInProgressGoodsReceipt_ReturnsFirstBlockedItem() {
        using var db = NewDb();
        SeedReceipt(db, Item, W1, ObjectStatus.InProgress);
        SeedReceipt(db, "CLEAN", W1, ObjectStatus.Finished);

        var result = await NewService(db).FindItemInInProgressGoodsReceipt([Item, "CLEAN"], [W1]);

        Assert.That(result, Is.EqualTo(Item));
    }

    [Test]
    public async Task FindItemInInProgressGoodsReceipt_NoneBlocked_ReturnsNull() {
        using var db = NewDb();
        SeedReceipt(db, Item, W1, ObjectStatus.Finished);

        var result = await NewService(db).FindItemInInProgressGoodsReceipt([Item], [W1]);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task FindItemInInProgressGoodsReceipt_EmptyInputs_ReturnsNull() {
        using var db = NewDb();
        SeedReceipt(db, Item, W1, ObjectStatus.InProgress);

        Assert.Multiple(async () => {
            Assert.That(await NewService(db).FindItemInInProgressGoodsReceipt([], [W1]), Is.Null);
            Assert.That(await NewService(db).FindItemInInProgressGoodsReceipt([Item], []), Is.Null);
        });
    }
}
