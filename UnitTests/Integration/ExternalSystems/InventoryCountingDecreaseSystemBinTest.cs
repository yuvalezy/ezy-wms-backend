using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Core.Interfaces;
using Core.DTOs.Items;
using Core.DTOs.GoodsReceipt;
using Core.DTOs.InventoryCounting;
using Core.Enums;
using Infrastructure.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Adapters.CrossPlatform.SBO.Services;
using Core.Models.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApi;

namespace UnitTests.Integration.ExternalSystems {
    [TestFixture]
    [Category("Integration")]
    [Category("ExternalSystem")]
    [Category("RequiresSapB1")]
    [Explicit("Requires SAP B1 test database connection")]
    public class InventoryCountingDecreaseSystemBinTest {
        protected WebApplicationFactory<Program> factory;
        protected IServiceScope                  factoryScope;

        private string      testItem      = $"TEST_ITEM_{Guid.NewGuid().ToString("N")[..8]}";
        private string      testWarehouse = "SM";
        private ISettings   settings;
        private SboSettings sboServiceLayerConnection;
        private SboCompany  sboCompany;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => {
                    builder.UseEnvironment("IntegrationTests");

                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .Build();

                    // builder.ConfigureServices(services => {
                    // });
                });

            // Create a service scope and resolve the service from it
            factoryScope = factory.Services.CreateScope();
            settings     = factoryScope.ServiceProvider.GetRequiredService<ISettings>();
            Assert.That(settings.SboSettings != null, "settings.SboSettings != null");
            sboServiceLayerConnection = settings.SboSettings!;
            sboCompany                = new SboCompany(settings, factory.Services.GetRequiredService<ILogger<SboCompany>>());
        }

        [Test]
        [Order(1)]
        public async Task Test_01_CreateTestItem_ShouldSucceed() {
            Assert.That(await sboCompany.ConnectCompany(), "Connection to SAP failed");

            // Create item in SBO using service layer
            var itemData = new {
                ItemCode = testItem,
                ItemName = $"Test Item {testItem}",
                ItemType = "itItems",

                // Purchase settings
                PurchaseItem           = "tYES",
                PurchasePackagingUnit  = "Box",
                PurchaseQtyPerPackUnit = 4,
                PurchaseUnit           = "Doz",
                PurchaseItemsPerUnit   = 12,

                // Sales settings  
                SalesItem           = "tYES",
                SalesPackagingUnit  = "Box",
                SalesQtyPerPackUnit = 4,
                SalesUnit           = "Doz",
                SalesItemsPerUnit   = 12,

                // Inventory settings
                ManageStockByWarehouse = "tNO",
                InventoryItem          = "tYES",

                // Default warehouse
                DefaultWarehouse = testWarehouse,

                // Additional required fields
                ItemsGroupCode = 100, // Default item group
                Valid          = "tYES",
                Frozen         = "tNO"
            };

            (bool success, string? errorMessage, object? result) = await sboCompany.PostAsync<object>("Items", itemData);

            Assert.That(success, Is.True, $"Item creation should succeed. Error: {errorMessage}");
            Assert.That(result, Is.Not.Null, "Item creation result should not be null");

            // Verify item was created by querying it back
            object? createdItem = await sboCompany.GetAsync<object>($"Items('{testItem}')");
            Assert.That(createdItem, Is.Not.Null, $"Created item {testItem} should be retrievable from SAP B1");
            await TestContext.Out.WriteLineAsync($"Created item: {createdItem}");
        }

        // [Test]
        // [Order(2)]
        // public async Task Test_02_CreateGoodsReceipt_ShouldAddItemToSystemBin() {
        //     // Arrange
        //     var goodsReceiptRequest = new CreateGoodsReceiptRequest {
        //         // Set up your goods receipt request based on your DTO structure
        //         // This is a placeholder - adjust according to your actual DTO
        //     };
        //
        //     // Act
        //     var result = await _goodsReceiptService.CreateGoodsReceiptAsync(goodsReceiptRequest, _testEmployeeId);
        //
        //     // Assert
        //     Assert.That(result, Is.Not.Null);
        //     Assert.That(result.Success, Is.True, "Goods receipt creation should succeed");
        //
        //     // Verify item is in system bin location
        //     // You might need to query your repository or SAP B1 to verify the stock location
        // }
        //
        // [Test]
        // [Order(3)]
        // public async Task Test_03_CreateInventoryCounting_ShouldInitializeCountingDocument() {
        //     // Arrange
        //     var createCountingRequest = new CreateInventoryCountingRequest {
        //         // Set up your inventory counting request
        //         // This is a placeholder - adjust according to your actual DTO
        //     };
        //
        //     // Act
        //     var result = await _inventoryCountingService.CreateInventoryCountingAsync(createCountingRequest, _testEmployeeId);
        //
        //     // Assert
        //     Assert.That(result, Is.Not.Null);
        //     Assert.That(result.Success, Is.True, "Inventory counting creation should succeed");
        //
        //     // Store the counting document ID for subsequent tests
        //     // You'll need to adapt this based on your response structure
        // }
        //
        // [Test]
        // [Order(4)]
        // public async Task Test_04_AddItemToInventoryCounting_ShouldIncludeTestItem() {
        //     // Arrange
        //     var addItemRequest = new InventoryCountingAddItemRequest {
        //         // Set up request to add the test item to counting
        //         // ItemCode = _testItemCode,
        //         // BinLocation = _testBinCode,
        //         // etc.
        //     };
        //
        //     // Act
        //     var result = await _inventoryCountingService.AddItemToInventoryCountingAsync(addItemRequest, _testEmployeeId);
        //
        //     // Assert
        //     Assert.That(result, Is.Not.Null);
        //     Assert.That(result.Success, Is.True, "Adding item to inventory counting should succeed");
        // }
        //
        // [Test]
        // [Order(5)]
        // public async Task Test_05_ExecuteCountingInDifferentBinLocations_ShouldRecordCounts() {
        //     // Arrange - This test simulates counting the item in different bin locations
        //     var binLocations = new[] { "01-A-02", "01-B-01", "01-C-01" }; // Different bins
        //
        //     foreach (var binLocation in binLocations) {
        //         var updateRequest = new InventoryCountingUpdateLineRequest {
        //             // Set up the counting update for each bin location
        //             // BinLocation = binLocation,
        //             // CountedQuantity = some_value,
        //             // etc.
        //         };
        //
        //         // Act
        //         // var result = await _inventoryCountingService.UpdateInventoryCountingLineAsync(updateRequest, _testEmployeeId);
        //
        //         // Assert
        //         Assert.That(result, Is.Not.Null);
        //         Assert.That(result.Success, Is.True, $"Updating count for bin {binLocation} should succeed");
        //     }
        // }
        //
        // [Test]
        // [Order(6)]
        // public async Task Test_06_ProcessInventoryCounting_ShouldUploadToSapB1() {
        //     // Arrange
        //     var processRequest = new ProcessInventoryCountingRequest {
        //         // Set up the process request to finalize and upload to SAP B1
        //     };
        //
        //     // Act
        //     // var result = await _inventoryCountingService.ProcessInventoryCountingAsync(processRequest, _testEmployeeId);
        //     //
        //     // // Assert
        //     // Assert.That(result, Is.Not.Null);
        //     // Assert.That(result.Success, Is.True, "Processing inventory counting should succeed");
        //
        //     // Verify the counting document was created in SAP B1
        //     // You might need to query SAP B1 directly to verify the document exists
        // }
        //
        // [Test]
        // [Order(7)]
        // public async Task Test_07_VerifySystemBinLocationDecrease_ShouldReflectCountingResults() {
        //     // Arrange & Act
        //     // Query the current stock in the original system bin location
        //     // This would require querying your stock/bin content service or SAP B1 directly
        //
        //     // Assert
        //     // Verify that the system bin location quantity has decreased
        //     // based on the inventory counting results
        //
        //     // Example assertion structure:
        //     // var currentStock = await GetBinStock(_testItemCode, _testBinCode);
        //     // Assert.That(currentStock, Is.LessThan(originalStock), 
        //     //     "System bin stock should decrease after inventory counting");
        //
        //     Assert.Pass("Implement verification based on your stock querying mechanism");
        // }
        //
        // [Test]
        // [Order(8)]
        // public async Task Test_08_VerifyInventoryCountingDocumentInSapB1_ShouldExistWithCorrectData() {
        //     // Arrange & Act
        //     // Query SAP B1 to verify the inventory counting document was created
        //     // and contains the correct data
        //
        //     // Assert
        //     // Verify document exists in SAP B1
        //     // Verify document contains correct item codes, quantities, bin locations
        //     // Verify document status is correct
        //
        //     Assert.Pass("Implement SAP B1 document verification based on your adapter");
        // }

        [OneTimeTearDown]
        public async Task OneTimeTearDown() {
            try {
                // Cleanup test data
                // 1. Cancel or reverse the inventory counting document if needed
                // 2. Remove test goods receipt if needed
                // 3. Delete test item from SAP B1 if created during test

                // Cleanup test data
                await CleanupTestItem(testItem);
                await CleanupTestDocuments();

                await factory.DisposeAsync();
                factoryScope.Dispose();
            }
            catch (Exception ex) {
                TestContext.WriteLine($"Cleanup failed: {ex.Message}");
                // Log but don't fail the test due to cleanup issues
            }
        }

        #region Helper Methods

        private async Task<decimal> GetBinStock(string itemCode, string binCode) {
            // Implement method to query current stock in specific bin location
            // This might use your repository or query SAP B1 directly
            throw new NotImplementedException("Implement based on your stock querying approach");
        }

        private async Task CleanupTestItem(string itemCode) {
            try {
                if (await sboCompany.ConnectCompany()) {
                    // Mark item as inactive instead of deleting (safer approach)
                    var updateData = new {
                        Valid  = "tNO",
                        Frozen = "tYES"
                    };

                    (bool success, string? errorMessage) = await sboCompany.PatchAsync($"Items('{itemCode}')", updateData);
                    if (success) {
                        TestContext.WriteLine($"Test item {itemCode} marked as inactive");
                    }
                    else {
                        TestContext.WriteLine($"Failed to deactivate test item {itemCode}: {errorMessage}");
                    }
                }
            }
            catch (Exception ex) {
                TestContext.WriteLine($"Error during test item cleanup: {ex.Message}");
            }
        }

        private async Task CleanupTestDocuments() {
            // Implement cleanup logic for test documents created during the test
            // This might involve canceling documents or marking them for deletion
            throw new NotImplementedException("Implement based on your document management approach");
        }

        #endregion
    }
}