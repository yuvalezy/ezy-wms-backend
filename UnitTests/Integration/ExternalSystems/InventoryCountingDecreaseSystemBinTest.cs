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
using System.Text.Json;
using System.Threading.Tasks;
using Adapters.Common.SBO.Repositories;
using Adapters.CrossPlatform.SBO.Services;
using Core.Models.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UnitTests.Integration.ExternalSystems.InventoryCountingDecreaseSystemBinTestHelpers;
using WebApi;

namespace UnitTests.Integration.ExternalSystems {
    [TestFixture]
    [Category("Integration")]
    [Category("ExternalSystem")]
    [Category("RequiresSapB1")]
    [Explicit("Requires SAP B1 test database connection")]
    public class InventoryCountingDecreaseSystemBinTest {
        protected WebApplicationFactory<Program> factory;

        private string     testItem      = $"TEST_ITEM_{Guid.NewGuid().ToString("N")[..8]}";
        private string     testWarehouse = "SM";
        private ISettings  settings;
        private SboCompany sboCompany;
        private Guid       countingId;

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
            using var scope = factory.Services.CreateScope();
            settings = scope.ServiceProvider.GetRequiredService<ISettings>();
            Assert.That(settings.SboSettings != null, "settings.SboSettings != null");
            sboCompany = new SboCompany(settings, factory.Services.GetRequiredService<ILogger<SboCompany>>());
        }

        [Test]
        [Order(1)]
        public async Task Test_01_CreateTestItem_ShouldSucceed() {
            var helper = new Test01CreateTestItem(sboCompany, testItem, testWarehouse);
            await helper.Execute();
        }

        [Test]
        [Order(2)]
        public async Task Test_02_CreateGoodsReceipt_ShouldAddItemToSystemBin() {
            if (!settings.Filters.InitialCountingBinEntry.HasValue) {
                throw new Exception("InitialCountingBinEntry is not set in appsettings.json filters");
            }

            var helper = new Test02CreateGoodsReceipt(sboCompany, testItem, testWarehouse, settings);
            await helper.Execute();
        }

        [Test]
        [Order(3)]
        public async Task Test_03_CreateInventoryCounting_ShouldInitializeCountingDocument() {
            var helper = new Test03CreateInventoryCounting(testItem, factory);
            countingId = await helper.Execute();
        }

        [Test]
        [Order(4)]
        public async Task Test_04_AddItemToInventoryCounting_ShouldIncludeTestItem() {
            var helper = new Test04AddItems(countingId, testItem, testWarehouse, factory, settings);
            await helper.Execute();
        }
        [Test]
        [Order(5)]
        public async Task Test_06_ProcessInventoryCounting_ShouldUploadToSapB1() {
            using var scope    = factory.Services.CreateScope();
            var       service  = scope.ServiceProvider.GetRequiredService<IInventoryCountingsService>();
            var       response = await service.ProcessCounting(countingId, TestConstants.SessionInfo);
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True, $"Processing failed: {response.ErrorMessage}");
            Assert.That(response.Status, Is.EqualTo(ResponseStatus.Ok));
            Assert.That(response.ErrorMessage, Is.Null.Or.Empty);
            Assert.That(response.ExternalEntry, Is.Not.Null, "External entry should be set");
            Assert.That(response.ExternalNumber, Is.Not.Null, "External number should be set");
        }
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