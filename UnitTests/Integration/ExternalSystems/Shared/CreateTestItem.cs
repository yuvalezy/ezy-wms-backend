using Adapters.CrossPlatform.SBO.Services;

namespace UnitTests.Integration.ExternalSystems.Shared;

public class CreateTestItem(SboCompany sboCompany) {
    private readonly string testItem      = $"TEST_ITEM_{Guid.NewGuid().ToString("N")[..8]}";
    private readonly string testWarehouse = TestConstants.SessionInfo.Warehouse;
    public async Task<ItemData> Execute() {
        Assert.That(await sboCompany.ConnectCompany(), "Connection to SAP failed");

        // Create item in SBO using service layer
        var itemData = new ItemData {
            ItemCode    = testItem,
            BarCode     = testItem,
            ItemName    = $"Test Item {testItem}",
            ItemType    = "itItems",
            ForeignName = "Test Foreign Name",

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
            ItemsGroupCode = await GetRandomItemGroup(),
            Manufacturer   = await GetRandomManufacturer(),
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
        return itemData;
    }

    private async Task<int> GetRandomItemGroup() {
        var response = await sboCompany.GetAsync<ItemGroups>("ItemGroups?$select=Number,GroupName&$filter=Number ne 100");
        if (response?.Value == null || response.Value.Length == 0)
            throw new Exception("Failed to get item groups");
        var itemGroup = response.Value[new Random().Next(0, response.Value.Length)];
        return itemGroup.Number;
    }

    private async Task<int> GetRandomManufacturer() {
        var response = await sboCompany.GetAsync<Manufacturers>("Manufacturers");
        if (response?.Value == null || response.Value.Length == 0)
            throw new Exception("Failed to get manufacturers");
        var itemGroup = response.Value[new Random().Next(0, response.Value.Length)];
        return itemGroup.Code;
    }
}

public record ItemGroups(ItemGroup[] Value);

public record ItemGroup(int Number, string GroupName);

public record Manufacturers(Manufacturer[] Value);

public record Manufacturer(int Code, string ManufacturerName);

public class ItemData {
    public string ItemCode               { get; init; }
    public string BarCode                { get; init; }
    public string ItemName               { get; init; }
    public string ItemType               { get; init; }
    public string ForeignName            { get; init; }
    public string PurchaseItem           { get; init; }
    public string PurchasePackagingUnit  { get; init; }
    public int    PurchaseQtyPerPackUnit { get; init; }
    public string PurchaseUnit           { get; init; }
    public int    PurchaseItemsPerUnit   { get; init; }
    public string SalesItem              { get; init; }
    public string SalesPackagingUnit     { get; init; }
    public int    SalesQtyPerPackUnit    { get; init; }
    public string SalesUnit              { get; init; }
    public int    SalesItemsPerUnit      { get; init; }
    public string ManageStockByWarehouse { get; init; }
    public string InventoryItem          { get; init; }
    public string DefaultWarehouse       { get; init; }
    public int    ItemsGroupCode         { get; init; }
    public int    Manufacturer           { get; init; }
    public string Valid                  { get; init; }
    public string Frozen                 { get; init; }
}