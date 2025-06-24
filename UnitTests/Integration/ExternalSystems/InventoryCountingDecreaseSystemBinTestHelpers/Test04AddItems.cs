using Core.DTOs.InventoryCounting;
using Core.DTOs.Items;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.InventoryCountingDecreaseSystemBinTestHelpers;

public class Test03CreateInventoryCounting(string testItem, string testWarehouse, WebApplicationFactory<Program> factory, ISettings settings) {
    private          Guid                                              id              = Guid.Empty;
    private readonly int                                               testBinLocation = settings.Filters.InitialCountingBinEntry!.Value;
    private          List<(int binEntry, int quantity, UnitType unit)> binEntries      = [];

    public async Task<Guid> Execute() {
        var response = await CreateCounting();
        await ValidateGetCountings();
        await LoadBins();
        await AddItem();
        return response.Id;
    }


    private async Task<InventoryCountingResponse> CreateCounting() {
        using var scope                     = factory.Services.CreateScope();
        var       inventoryCountingsService = scope.ServiceProvider.GetRequiredService<IInventoryCountingsService>();
        var request = new CreateInventoryCountingRequest {
            Name = $"Test {testItem}"
        };
        var response = await inventoryCountingsService.CreateCounting(request, TestConstants.SessionInfo);
        Assert.That(response, Is.Not.Null);
        Assert.That(!response.Error);
        Assert.That(response.Status == ObjectStatus.Open);
        id = response.Id;
        return response;
    }

    private async Task ValidateGetCountings() {
        using var scope                     = factory.Services.CreateScope();
        var       inventoryCountingsService = scope.ServiceProvider.GetRequiredService<IInventoryCountingsService>();
        var response = await inventoryCountingsService.GetCountings(new InventoryCountingsRequest {
            Statuses = [
                ObjectStatus.Open, ObjectStatus.InProgress
            ]
        }, TestConstants.SessionInfo.Warehouse);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Any(v => v.Id == id));
    }

    private async Task LoadBins() {
        string connectionString = settings.ConnectionStrings.ExternalAdapterConnection;
        string query            = $"select top 4 \"BinCode\" from OBIN where \"WhsCode\" = '{testWarehouse}' and \"AbsEntry\" <> {testBinLocation} order by NEWID()";
        try {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);

            await using var dr = await command.ExecuteReaderAsync();

            // First bin 1 box
            int binLocation = dr.GetInt32(0);
            binEntries.Add((binLocation, 1, UnitType.Pack));

            // Second bin 2 dozens
            await dr.ReadAsync();
            binLocation = dr.GetInt32(0);
            binEntries.Add((binLocation, 2, UnitType.Dozen));

            // Third bin 6 units
            await dr.ReadAsync();
            binLocation = dr.GetInt32(0);
            binEntries.Add((binLocation, 6, UnitType.Unit));

            // Fourth bin 2 boxes
            await dr.ReadAsync();
            binLocation = dr.GetInt32(0);
            binEntries.Add((binLocation, 2, UnitType.Pack));

            await TestContext.Out.WriteLineAsync($"Target bin locations loaded");
        }
        catch (Exception ex) {
            await TestContext.Out.WriteLineAsync($"SQL query failed: {ex.Message}");
            throw;
        }
    }

    private async Task AddItem() {
        using var scope                     = factory.Services.CreateScope();
        var       inventoryCountingsService = scope.ServiceProvider.GetRequiredService<IInventoryCountingsService>();
        await inventoryCountingsService.AddItem(TestConstants.SessionInfo, new InventoryCountingAddItemRequest() {
            BarCode  = testItem,
            ID       = id,
            ItemCode = testItem,
            Quantity = 1,
            Unit     = UnitType.Pack
        });
    }
}