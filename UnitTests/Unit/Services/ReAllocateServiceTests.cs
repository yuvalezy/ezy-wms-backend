using System.Reflection;
using Core.DTOs.Items;
using Core.DTOs.ReAllocate;
using Core.DTOs.Transfer;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Unit.Services;

[TestFixture]
public class ReAllocateServiceTests {
    private const string Item = "Z0001";
    private const string Whs  = "01";
    private const int    BinA = 10, BinB = 11, Target1 = 20, Target2 = 21, ForeignBin = 99;

    private SystemDbContext db      = null!;
    private FakeAdapter     adapter = null!;

    [SetUp]
    public void SetUp() {
        db      = new SystemDbContext(new DbContextOptionsBuilder<SystemDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        adapter = FakeAdapter.Create();
        adapter.BinStock = [
            new ItemBinStockResponse { BinEntry = BinA, BinCode = "01-A1-01", Quantity = 10 },
            new ItemBinStockResponse { BinEntry = BinB, BinCode = "01-A1-02", Quantity = 4.5m }
        ];
        adapter.WarehouseBins = [BinA, BinB, Target1, Target2];
    }

    [TearDown]
    public void TearDown() => db.Dispose();

    private ReAllocateService NewService() =>
        new((IExternalSystemAdapter)(object)adapter, new PickListValidationService(db, (IExternalSystemAdapter)(object)adapter), NullLogger<ReAllocateService>.Instance);

    private static SessionInfo Session() => new() {
        UserId = Guid.NewGuid().ToString(), Name = "tester", Warehouse = Whs, Roles = [RoleType.ReAllocate], SuperUser = false, EnableBinLocations = true
    };

    private static ReAllocateRequest Request(IEnumerable<(int bin, decimal qty)> sources, IEnumerable<(int bin, decimal qty)> targets) => new() {
        ItemCode = Item,
        Sources  = sources.Select(s => new ReAllocateBinQuantity { BinEntry = s.bin, Quantity = s.qty }).ToList(),
        Targets  = targets.Select(t => new ReAllocateBinQuantity { BinEntry = t.bin, Quantity = t.qty }).ToList()
    };

    private void SeedPicked(int bin, decimal qty, ObjectStatus status = ObjectStatus.Open, SyncStatus sync = SyncStatus.Pending) {
        db.PickLists.Add(new PickList {
            Id = Guid.NewGuid(), AbsEntry = 1, PickEntry = 1, ItemCode = Item, BinEntry = bin, Quantity = qty,
            Unit = UnitType.Unit, Status = status, SyncStatus = sync
        });
        db.SaveChanges();
    }

    // --- sources -------------------------------------------------------------------------------

    [Test]
    public async Task GetSources_SubtractsWmsPickedAndOpenTransferLines() {
        SeedPicked(BinA, 3);
        SeedPicked(BinA, 100, ObjectStatus.Closed);                          // closed: already posted, ignored
        SeedPicked(BinA, 100, ObjectStatus.Open, SyncStatus.ExternalCancel); // cancelled in SAP, ignored
        db.TransferLines.Add(new TransferLine {
            Id = Guid.NewGuid(), TransferId = Guid.NewGuid(), Date = DateTime.UtcNow, ItemCode = Item, BinEntry = BinB, Quantity = 1.5m, LineStatus = LineStatus.Open
        });
        db.SaveChanges();

        var result = await NewService().GetSourcesAsync(Item, Session());

        var a = result.Bins.Single(b => b.BinEntry == BinA);
        var b = result.Bins.Single(b => b.BinEntry == BinB);
        Assert.Multiple(() => {
            Assert.That(a.Reserved, Is.EqualTo(3));
            Assert.That(a.Available, Is.EqualTo(7));
            Assert.That(b.Available, Is.EqualTo(3));
            Assert.That(result.TotalAvailable, Is.EqualTo(10));
        });
    }

    [Test]
    public async Task GetSources_FullyReservedBin_IsDropped() {
        SeedPicked(BinB, 4.5m);

        var result = await NewService().GetSourcesAsync(Item, Session());

        Assert.That(result.Bins.Select(b => b.BinEntry), Is.EquivalentTo(new[] { BinA }));
    }

    [Test]
    public async Task GetSources_OverReservedBin_NeverNegative() {
        SeedPicked(BinA, 12);

        var result = await NewService().GetSourcesAsync(Item, Session());

        Assert.That(result.Bins.Any(b => b.BinEntry == BinA), Is.False);
        Assert.That(result.TotalAvailable, Is.EqualTo(4.5m));
    }

    // --- shape validation ----------------------------------------------------------------------

    [Test]
    public void ValidateShape_SourceAndTargetTotalsDiffer_Rejected() =>
        Assert.That(ReAllocateService.ValidateShape(Request([(BinA, 10)], [(Target1, 9)])), Does.Contain("must equal"));

    [Test]
    public void ValidateShape_TotalsWithinTolerance_Accepted() =>
        Assert.That(ReAllocateService.ValidateShape(Request([(BinA, 3.33335m)], [(Target1, 3.33333m)])), Is.Null);

    [Test]
    public void ValidateShape_BinIsSourceAndTarget_Rejected() =>
        Assert.That(ReAllocateService.ValidateShape(Request([(BinA, 5), (BinB, 5)], [(BinA, 10)])), Does.Contain("both a source and a target"));

    [Test]
    public void ValidateShape_MoreThanThreeTargets_Rejected() =>
        Assert.That(ReAllocateService.ValidateShape(Request([(BinA, 4)], [(20, 1), (21, 1), (22, 1), (23, 1)])), Does.Contain("target bins"));

    [Test]
    public void ValidateShape_NoTargets_Rejected() =>
        Assert.That(ReAllocateService.ValidateShape(Request([(BinA, 4)], [])), Does.Contain("target bins"));

    [TestCase(0)]
    [TestCase(-1)]
    public void ValidateShape_NonPositiveQuantity_Rejected(decimal qty) =>
        Assert.That(ReAllocateService.ValidateShape(Request([(BinA, qty), (BinB, 2)], [(Target1, 2 + qty)])), Does.Contain("greater than zero"));

    [Test]
    public void ValidateShape_DuplicateTarget_Rejected() =>
        Assert.That(ReAllocateService.ValidateShape(Request([(BinA, 2)], [(Target1, 1), (Target1, 1)])), Does.Contain("more than once"));

    // --- execute -------------------------------------------------------------------------------

    [Test]
    public async Task Execute_HappyPath_PostsOneLineWithAllBins() {
        adapter.Transfer = new ProcessTransferResponse { Success = true, ExternalEntry = 500, ExternalNumber = 1500 };

        var result = await NewService().ExecuteAsync(Request([(BinA, 10), (BinB, 4.5m)], [(Target1, 8), (Target2, 6.5m)]), Session());

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.SapDocNumber, Is.EqualTo(1500));
        Assert.That(adapter.TransferCalls, Has.Count.EqualTo(1));
        var line = adapter.TransferCalls[0].Single().Value;
        Assert.Multiple(() => {
            Assert.That(line.ItemCode, Is.EqualTo(Item));
            Assert.That(line.Quantity, Is.EqualTo(14.5m));
            Assert.That(line.SourceBins.Select(b => (b.BinEntry, b.Quantity)), Is.EquivalentTo(new[] { (BinA, 10m), (BinB, 4.5m) }));
            Assert.That(line.TargetBins.Select(b => (b.BinEntry, b.Quantity)), Is.EquivalentTo(new[] { (Target1, 8m), (Target2, 6.5m) }));
        });
    }

    [Test]
    public async Task Execute_SourceAbovePickAdjustedAvailable_RejectedWithoutPosting() {
        SeedPicked(BinA, 2);

        var result = await NewService().ExecuteAsync(Request([(BinA, 10)], [(Target1, 10)]), Session());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("only 8"));
        Assert.That(adapter.TransferCalls, Is.Empty);
    }

    [Test]
    public async Task Execute_SourceBinNoLongerHasStock_RejectedWithoutPosting() {
        var result = await NewService().ExecuteAsync(Request([(12345, 1)], [(Target1, 1)]), Session());

        Assert.That(result.Success, Is.False);
        Assert.That(adapter.TransferCalls, Is.Empty);
    }

    [Test]
    public async Task Execute_BatchManagedItem_Rejected() {
        adapter.Item = new ItemCheckResponse { ItemCode = Item, IsBatchManaged = true };

        var result = await NewService().ExecuteAsync(Request([(BinA, 1)], [(Target1, 1)]), Session());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("batch or serial"));
        Assert.That(adapter.TransferCalls, Is.Empty);
    }

    [Test]
    public async Task Execute_TargetInOtherWarehouse_Rejected() {
        var result = await NewService().ExecuteAsync(Request([(BinA, 1)], [(ForeignBin, 1)]), Session());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("does not belong"));
        Assert.That(adapter.TransferCalls, Is.Empty);
    }

    [Test]
    public async Task Execute_SapFailure_ReturnsError() {
        adapter.Transfer = new ProcessTransferResponse { Success = false, ErrorMessage = "Bin is blocked" };

        var result = await NewService().ExecuteAsync(Request([(BinA, 1)], [(Target1, 1)]), Session());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Bin is blocked"));
    }

    /// <summary>Implements only the adapter members re-allocate uses; anything else throws.</summary>
    public class FakeAdapter : DispatchProxy {
        public ItemBinStockResponse[] BinStock      { get; set; } = [];
        public int[]                  WarehouseBins { get; set; } = [];
        public ItemCheckResponse      Item          { get; set; } = new() { ItemCode = ReAllocateServiceTests.Item };
        public ProcessTransferResponse Transfer     { get; set; } = new() { Success = true, ExternalEntry = 1, ExternalNumber = 1 };
        public List<Dictionary<string, TransferCreationDataResponse>> TransferCalls { get; } = [];

        public static FakeAdapter Create() => (FakeAdapter)(object)Create<IExternalSystemAdapter, FakeAdapter>();

        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch {
            nameof(IExternalSystemAdapter.ItemBinStockAsync) => Task.FromResult<IEnumerable<ItemBinStockResponse>>(BinStock),
            nameof(IExternalSystemAdapter.ItemCheckAsync)    => Task.FromResult<IEnumerable<ItemCheckResponse>>([Item]),
            nameof(IExternalSystemAdapter.GetBinsAsync)      => Task.FromResult<IEnumerable<BinLocationResponse>>(
                WarehouseBins.Select(b => new BinLocationResponse { Entry = b, Code = b.ToString() }).ToArray()),
            nameof(IExternalSystemAdapter.ProcessTransfer) => Record((Dictionary<string, TransferCreationDataResponse>)args![4]!),
            _ => throw new NotSupportedException(method.Name)
        };

        private Task<ProcessTransferResponse> Record(Dictionary<string, TransferCreationDataResponse> data) {
            TransferCalls.Add(data);
            return Task.FromResult(Transfer);
        }
    }
}
