using Core.DTOs.InventoryCounting;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.InventoryCounting.InventoryCountingDecreaseSystemBinTestHelpers;

public class AddItems(Guid id, string testItem, string testWarehouse, WebApplicationFactory<Program> factory, ISettings settings) {
    private readonly int testBinLocation = settings.GetInitialCountingBinEntry(testWarehouse)!.Value;

    private readonly List<(int binEntry, string binCode, int quantity, UnitType unit)> binEntries = [];

    private Guid lastLineId = Guid.Empty;
    private int  lastBinEntry;

    public async Task<List<(int binEntry, string binCode, int quantity, UnitType unit)>> Execute() {
        await LoadBins();
        await AddItem();
        await ValidateCountingContent();
        await UpdateItem();
        await ValidateCountingContent(true);
        return binEntries;
    }

    private async Task LoadBins() {
        string connectionString = settings.ConnectionStrings.ExternalAdapterConnection;
        string query            = $"select top 4 \"AbsEntry\", \"BinCode\" from OBIN where \"WhsCode\" = '{testWarehouse}' and \"AbsEntry\" <> {testBinLocation} order by NEWID()";
        try {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);

            await using var dr = await command.ExecuteReaderAsync();

            // First bin 1 box
            await dr.ReadAsync();
            binEntries.Add((dr.GetInt32(0), dr.GetString(1), 1, UnitType.Pack));

            // Second bin 2 dozens
            await dr.ReadAsync();
            binEntries.Add((dr.GetInt32(0), dr.GetString(1), 2, UnitType.Dozen));

            // Third bin 6 units
            await dr.ReadAsync();
            binEntries.Add((dr.GetInt32(0), dr.GetString(1), 6, UnitType.Unit));

            // Fourth bin 2 boxes
            await dr.ReadAsync();
            binEntries.Add((dr.GetInt32(0), dr.GetString(1), 2, UnitType.Pack));

            await TestContext.Out.WriteLineAsync($"Target bin locations loaded");
        }
        catch (Exception ex) {
            await TestContext.Out.WriteLineAsync($"SQL query failed: {ex.Message}");
            throw;
        }
    }

    private async Task AddItem() {
        foreach (var entry in binEntries) {
            using var scope   = factory.Services.CreateScope();
            var       service = scope.ServiceProvider.GetRequiredService<IInventoryCountingsLineService>();
            var response = await service.AddItem(TestConstants.SessionInfo, new InventoryCountingAddItemRequest() {
                BarCode  = testItem,
                BinEntry = entry.binEntry,
                ID       = id,
                ItemCode = testItem,
                Quantity = entry.quantity,
                Unit     = entry.unit
            });
            Assert.That(response, Is.Not.Null);
            Assert.That(response.LineId, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo(ResponseStatus.Ok));
            lastLineId = response.LineId.Value;
            lastBinEntry = entry.binEntry;
        }
    }

    private async Task ValidateCountingContent(bool afterUpdate = false) {
        using var scope   = factory.Services.CreateScope();
        var       service = scope.ServiceProvider.GetRequiredService<IInventoryCountingsService>();
        var request = new InventoryCountingContentRequest {
            ID = id,
        };
        var       responses        = await service.GetCountingContent(request);
        foreach (var row in responses) {
            var entry         = binEntries.First(b => b.binEntry == row.BinEntry);
            // Assert.That(row.BinCode, Is.EqualTo(entry.binCode)); TODO: Bin Code is not loaded yet
            Assert.That(row.ItemCode, Is.EqualTo(testItem));
            Assert.That(row.ItemName, Is.EqualTo($"Test Item {testItem}"));
            Assert.That(row.BuyUnitMsr, Is.EqualTo("Doz"));
            Assert.That(row.NumInBuy, Is.EqualTo(12));
            Assert.That(row.PurPackMsr, Is.EqualTo("Box"));
            Assert.That(row.PurPackUn, Is.EqualTo(4));
            
            int entryQuantity = entry.quantity;
            if (afterUpdate && entry.binEntry == lastBinEntry)
                entryQuantity = 1;
            if (entry.unit != UnitType.Unit)
                entryQuantity *= row.NumInBuy;
            if (entry.unit == UnitType.Pack)
                entryQuantity *= row.PurPackUn;
            
            Assert.That(row.CountedQuantity, Is.EqualTo(entryQuantity));
            Assert.That(row.SystemQuantity, Is.Zero);
            Assert.That(row.Variance, Is.EqualTo(entryQuantity));
        }
    }

    private async Task UpdateItem() {
        using var scope   = factory.Services.CreateScope();
        var       service = scope.ServiceProvider.GetRequiredService<IInventoryCountingsLineService>();
        var request = new InventoryCountingUpdateLineRequest {
            Id       = id,
            LineId   = lastLineId,
            Quantity = 1,
            Comment  = "Changed Quantity",
        };
        var response = await service.UpdateLine(TestConstants.SessionInfo, request);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.ReturnValue, Is.EqualTo(UpdateLineReturnValue.Ok));

        var lastEntry = binEntries.Last();
        binEntries.Remove(lastEntry);
        binEntries.Add((lastEntry.binEntry, lastEntry.binCode, 1, lastEntry.unit));
    }
}