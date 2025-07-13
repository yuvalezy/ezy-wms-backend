using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;
using Core;
using Core.DTOs.Package;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.Shared;

public class CreateGoodsReceipt(SboCompany sboCompany, string testItem, ISettings settings, int series, WebApplicationFactory<Program> factory) {
    private readonly int    testBinLocation = settings.Filters.InitialCountingBinEntry!.Value;
    private readonly string testWarehouse   = TestConstants.SessionInfo.Warehouse;

    public bool       Package         { get; set; }
    public List<Guid> CreatedPackages { get; set; } = [];

    public async Task Execute() {
        Assert.That(await sboCompany.ConnectCompany(), "Connection to SAP failed");

        await CreateDocument();

        await ValidateWarehouseStock();

        await ValidateBinStock();

        if (!Package)
            return;
        await CreatePackages();

        await ValidatePackageContent();
    }


    private async Task CreateDocument() {
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
                    DocumentLinesBinAllocations = new[] {
                        new {
                            BinAbsEntry                   = testBinLocation,
                            Quantity                      = 80 * 12,
                            AllowNegativeQuantity         = "tNO",
                            SerialAndBatchNumbersBaseLine = -1,
                            BaseLineNumber                = 0
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
        using var scope   = factory.Services.CreateScope();
        var       service = scope.ServiceProvider.GetRequiredService<IExternalSystemAdapter>();

        try {
            var result = (await service.ItemStockAsync(testItem, testWarehouse)).ToArray();
            Assert.That(result, Is.Not.Null, "Item stock information should be retrievable");
            Assert.That(result.Length, Is.EqualTo(1), "Item stock information should contain only one record");
            var row = result[0];
            Assert.That(row.Quantity, Is.GreaterThan(0), $"Bin location {testBinLocation} should have stock");
            Assert.That(row.Quantity, Is.EqualTo(960m), $"Bin location {testBinLocation} should have 960 units (20 boxes * 4 units * 12 dozens)");
            Assert.That(row.BinEntry, Is.EqualTo(testBinLocation), $"Bin location {testBinLocation} should have correct bin entry");
            await TestContext.Out.WriteLineAsync($"Bin location {testBinLocation} stock: {row.Quantity}");
        }
        catch (Exception ex) {
            await TestContext.Out.WriteLineAsync($"SQL query failed: {ex.Message}");
            throw;
        }
    }

    private async Task CreatePackages() {
        using (var scope = factory.Services.CreateScope()) {
            var packageService = scope.ServiceProvider.GetRequiredService<IPackageService>();
            var request = new CreatePackageRequest {
                BinEntry            = testBinLocation,
                SourceOperationType = ObjectType.Package,
            };
            var package = await packageService.CreatePackageAsync(TestConstants.SessionInfo, request);
            CreatedPackages.Add(package.Id);

            var package2 = await packageService.CreatePackageAsync(TestConstants.SessionInfo, request);
            CreatedPackages.Add(package2.Id);
        }

        using (var scope = factory.Services.CreateScope()) {
            var packageService = scope.ServiceProvider.GetRequiredService<IPackageContentService>();
            foreach (var package in CreatedPackages) {
                var request = new AddItemToPackageRequest {
                    PackageId           = package,
                    ItemCode            = testItem,
                    Quantity            = 2,
                    UnitQuantity        = 24,
                    UnitType            = UnitType.Dozen,
                    BinEntry            = testBinLocation,
                    SourceOperationType = ObjectType.Package,
                };
                await packageService.AddItemToPackageAsync(request, TestConstants.SessionInfo);
            }
        }
    }
    private async Task ValidatePackageContent() {
        using var scope          = factory.Services.CreateScope();
        var       packageService = scope.ServiceProvider.GetRequiredService<IPackageService>();
        foreach (var id in CreatedPackages) {
            var package = await packageService.GetPackageAsync(id);
            Assert.That(package.Contents.Count == 1);
            var content = package.Contents.First();
            Assert.That(content.ItemCode, Is.EqualTo(testItem));
            Assert.That(content.Quantity, Is.EqualTo(24));;
        }
    }
}