using Adapters.CrossPlatform.SBO.Services;

namespace UnitTests.Integration.ExternalSystems.InventoryCountingDecreaseSystemBinTestHelpers;

public class Test01CreateTestItem(SboCompany sboCompany, string testItem, string testWarehouse) {
    public async Task Execute() {
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
}