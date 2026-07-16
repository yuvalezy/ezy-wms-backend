using Core.DTOs.DirectTransfer;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Integration.ExternalSystems.Shared;

namespace UnitTests.Integration.ExternalSystems.InventoryTransfer;

/// <summary>
/// Direct transfer moves stock straight from a source bin to a target bin in one shot, without a saved
/// document. The operator scans a count in a unit (Unit/Dozen/Pack); the service must convert that to base
/// pieces before posting, exactly as the document-based transfer does — otherwise a one-dozen scan moves a
/// single piece.
///
/// This is a full round trip against the real SAP database: it receives stock into a source bin, posts a
/// direct transfer via the real service, and reads the bin stock back to prove the right number of base
/// pieces actually moved. The test item is a correctly-configured dozen (NumInBuy 12, Box of 4 dozens), so a
/// dozen is 12 base and a box is 48.
/// </summary>
[TestFixture]
[Category("Integration")]
[Category("ExternalSystem")]
[Category("RequiresSapB1")]
public class DirectTransferTest : BaseExternalTest {
    private const string TestWarehouse = "SM";

    private IExternalSystemAdapter adapter;
    private string testItem = string.Empty;
    private int sourceBin;
    private int targetBin;

    [OneTimeSetUp]
    public new void OneTimeSetUp() {
        base.OneTimeSetUp();

        using var scope = factory.Services.CreateScope();
        adapter = scope.ServiceProvider.GetRequiredService<IExternalSystemAdapter>();
    }

    /// The quantity of the test item currently sitting in a given bin, or zero if it holds none.
    private async Task<decimal> BinQty(int binEntry) {
        var stocks = await adapter.ItemBinStockAsync(testItem, TestWarehouse);
        return stocks.FirstOrDefault(s => s.BinEntry == binEntry)?.Quantity ?? 0m;
    }

    private async Task<DirectTransferResponse> Transfer(decimal quantity, UnitType unit) {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDirectTransferService>();
        // UnitCode carries the unit the way the frontend sends it — the enum name, e.g. "Dozen".
        return await service.ExecuteAsync(new DirectTransferRequest {
            SourceBinEntry = sourceBin,
            TargetBinEntry = targetBin,
            ItemCode = testItem,
            Quantity = quantity,
            UnitCode = unit.ToString()
        }, TestConstants.SessionInfo);
    }

    private async Task<DirectTransferResponse> TransferAll() {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDirectTransferService>();
        return await service.ExecuteAsync(new DirectTransferRequest {
            SourceBinEntry = sourceBin,
            TargetBinEntry = targetBin,
            ItemCode = testItem,
            Quantity = 0,
            UnitCode = UnitType.Dozen.ToString(), // ignored for a transfer-all, and deliberately not Unit
            TransferAll = true
        }, TestConstants.SessionInfo);
    }

    /// A real bin in the warehouse other than the source, to receive the transferred stock.
    private async Task<int> LoadTargetBin() {
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        string connectionString = configuration.GetConnectionString("ExternalAdapterConnection")
            ?? throw new InvalidOperationException("ExternalAdapterConnection not found in configuration");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            $"select top 1 \"AbsEntry\" from OBIN where \"WhsCode\" = '{TestWarehouse}' and \"AbsEntry\" <> {sourceBin} order by NEWID()",
            connection);
        var result = await command.ExecuteScalarAsync();
        Assert.That(result, Is.Not.Null, "the warehouse must have a second bin to transfer into");
        return Convert.ToInt32(result);
    }

    [Test, Order(0)]
    public async Task PrepareData() {
        if (settings.GetInitialCountingBinEntry(TestWarehouse) == null)
            throw new Exception("InitialCountingBinEntry is not set in appsettings.json filters");

        sourceBin = settings.GetInitialCountingBinEntry(TestWarehouse)!.Value;
        testItem = (await new CreateTestItem(sboCompany).Execute()).ItemCode;

        // Seeds 960 base pieces into the source bin (80 dozen-boxes).
        await new CreateGoodsReceipt(sboCompany, settings, goodsReceiptSeries, factory, testItem).Execute();

        targetBin = await LoadTargetBin();
    }

    // --- The bug: a unit scan must be converted to base pieces before posting ---

    [Test, Order(1)]
    public async Task OneDozen_MovesTwelveBasePieces() {
        var srcBefore = await BinQty(sourceBin);
        var tgtBefore = await BinQty(targetBin);

        var result = await Transfer(1m, UnitType.Dozen);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        decimal movedIn = await BinQty(targetBin) - tgtBefore;
        decimal movedOut = srcBefore - await BinQty(sourceBin);
        Assert.Multiple(() => {
            Assert.That(movedIn, Is.EqualTo(12m), "one dozen lands as 12 base pieces in the target bin");
            Assert.That(movedOut, Is.EqualTo(12m), "and 12 leave the source bin");
        });
    }

    [Test, Order(2)]
    public async Task OneBox_MovesFortyEightBasePieces() {
        var srcBefore = await BinQty(sourceBin);
        var tgtBefore = await BinQty(targetBin);

        var result = await Transfer(1m, UnitType.Pack);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        decimal movedIn = await BinQty(targetBin) - tgtBefore;
        decimal movedOut = srcBefore - await BinQty(sourceBin);
        Assert.Multiple(() => {
            Assert.That(movedIn, Is.EqualTo(48m), "a box is 4 dozens = 48 base pieces");
            Assert.That(movedOut, Is.EqualTo(48m));
        });
    }

    [Test, Order(3)]
    public async Task BaseUnits_MoveTheExactCount() {
        var srcBefore = await BinQty(sourceBin);
        var tgtBefore = await BinQty(targetBin);

        var result = await Transfer(5m, UnitType.Unit);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        decimal movedIn = await BinQty(targetBin) - tgtBefore;
        decimal movedOut = srcBefore - await BinQty(sourceBin);
        Assert.Multiple(() => {
            Assert.That(movedIn, Is.EqualTo(5m), "a base-unit scan is never multiplied");
            Assert.That(movedOut, Is.EqualTo(5m));
        });
    }

    // --- The inverse bug: transfer-all works off bin stock, which is already base — it must NOT reconvert ---

    [Test, Order(4)]
    public async Task TransferAll_MovesTheWholeSourceBinInBaseUnits() {
        var srcBefore = await BinQty(sourceBin);
        Assert.That(srcBefore, Is.GreaterThan(0m), "there is stock left in the source bin to sweep");
        var tgtBefore = await BinQty(targetBin);

        var result = await TransferAll();

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        decimal srcAfter = await BinQty(sourceBin);
        decimal movedIn = await BinQty(targetBin) - tgtBefore;
        Assert.Multiple(() => {
            Assert.That(srcAfter, Is.EqualTo(0m), "the source bin is emptied");
            Assert.That(movedIn, Is.EqualTo(srcBefore),
                "bin stock is already in base units, so it moves as-is — reconverting it would be the inverse bug");
        });
    }
}
