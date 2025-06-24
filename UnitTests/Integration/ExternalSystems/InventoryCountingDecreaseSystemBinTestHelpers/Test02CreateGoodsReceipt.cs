using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;
using Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace UnitTests.Integration.ExternalSystems.InventoryCountingDecreaseSystemBinTestHelpers;

public class Test02CreateGoodsReceipt(SboCompany sboCompany, string testItem, string testWarehouse, ISettings settings) {
    private readonly int testBinLocation = settings.Filters.InitialCountingBinEntry!.Value;

    public async Task Execute() {
        Assert.That(await sboCompany.ConnectCompany(), "Connection to SAP failed");

        await CreateGoodsReceipt();

        await ValidateWarehouseStock();

        await ValidateBinStock();
    }

    private async Task CreateGoodsReceipt() {
        // Get document series for current period for new Inventory Goods Receipt
        const int series = 129;

        // Generate an Inventory Goods Receipt: 20 boxes into testWarehouse with testItem into testBinLocation
        var goodsReceiptData = new {
            Series     = series,
            DocDate    = DateTime.Now.ToString("yyyy-MM-dd"),
            DocDueDate = DateTime.Now.ToString("yyyy-MM-dd"),
            Comments   = "Test Goods Receipt for Inventory Counting Unit Test",

            DocumentLines = new[] {
                new {
                    ItemCode      = testItem,
                    Quantity      = 80, 
                    WarehouseCode = testWarehouse,
                    UnitPrice     = 10.0, 
                    UseBaseUnits  = "tNO",
                    BinAllocations = new[] {
                        new {
                            BinAbsEntry                   = testBinLocation,
                            Quantity                      = 969,
                            AllowNegativeQuantity         = "tNO",
                            SerialAndBatchNumbersBaseLine = 0
                        }
                    }
                }
            }
        };

        (bool success, string? errorMessage, var result) = await sboCompany.PostAsync<JsonDocument>("InventoryGenEntries", goodsReceiptData);
        Assert.That(success, Is.True, $"Goods receipt creation should succeed. Error: {errorMessage}");
        Assert.That(result, Is.Not.Null, "Goods receipt creation result should not be null");

        // Extract DocEntry from result for verification 
        string? docEntry = result?.RootElement.GetProperty("DocEntry").ToString();
        Assert.That(docEntry, Is.Not.Null.And.Not.Empty, "DocEntry should be returned from goods receipt creation");
        await TestContext.Out.WriteLineAsync($"Created Goods Receipt with DocEntry: {docEntry}");
    }

    private async Task ValidateWarehouseStock() {
        // Assert that stock of warehouse testWarehouse for item testItem has correct value
        var warehouseStockResponse = await sboCompany.GetAsync<JsonDocument>($"Items('{testItem}')");
        Assert.That(warehouseStockResponse, Is.Not.Null, "Warehouse stock information should be retrievable");

        // Extract warehouse stock quantity using JsonDocument
        Assert.That(warehouseStockResponse.RootElement.TryGetProperty("ItemWarehouseInfoCollection", out var warehouseCollection),
            Is.True, "ItemWarehouseInfoCollection should exist");

        // Find stock for our test warehouse
        bool foundWarehouseStock = false;
        foreach (var warehouseInfo in warehouseCollection.EnumerateArray()) {
            if (!warehouseInfo.TryGetProperty("WarehouseCode", out var warehouseCode) || warehouseCode.GetString() != testWarehouse ||
                !warehouseInfo.TryGetProperty("InStock", out var inStockProperty))
                continue;
            decimal inStock = inStockProperty.GetDecimal();
            Assert.That(inStock, Is.EqualTo(960.0m), $"Warehouse {testWarehouse} should have 960 units in stock");
            foundWarehouseStock = true;
            await TestContext.Out.WriteLineAsync($"Warehouse {testWarehouse} stock: {inStock}");
            break;
        }

        Assert.That(foundWarehouseStock, Is.True, $"Should find stock record for warehouse {testWarehouse}");
    }

    private async Task ValidateBinStock() {
        string connectionString = settings.ConnectionStrings.ExternalAdapterConnection;
        string query            = $"select \"OnHandQty\" from OIBQ where \"BinAbs\" = {testBinLocation} and \"ItemCode\" = '{testItem}'";

        try {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command     = new SqlCommand(query, connection);
            decimal         binQuantity = (decimal)(await command.ExecuteScalarAsync() ?? -1);
            Assert.That(binQuantity, Is.GreaterThan(0), $"Bin location {testBinLocation} should have stock");
            Assert.That(binQuantity, Is.EqualTo(960m), $"Bin location {testBinLocation} should have 960 units (20 boxes * 4 units * 12 dozens)");
            await TestContext.Out.WriteLineAsync($"Bin location {testBinLocation} stock: {binQuantity}");
        }
        catch (Exception ex) {
            await TestContext.Out.WriteLineAsync($"SQL query failed: {ex.Message}");
            throw;
        }
    }
}