using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;

namespace UnitTests.Integration.ExternalSystems.Shared;

public class CreateSalesOrder(SboCompany sboCompany, string testItem, int series, string customerCode) {
    public int SalesEntry { get; private set; } = -1;

    public int PickEntry { get; private set; } = -1;

    public async Task Execute() {
        Assert.That(await sboCompany.ConnectCompany(), "Connection to SAP failed");
        SalesEntry = await CreateDocument();
        PickEntry = await ReleaseToPickList();
    }

    private async Task<int> CreateDocument() {
        // Generate an Inventory Sales Order: 20 boxes into testWarehouse with testItem into testBinLocation
        var data = new {
            CardCode   = customerCode,
            Series     = series,
            DocDate    = DateTime.Now.ToString("yyyy-MM-dd"),
            DocDueDate = DateTime.Now.ToString("yyyy-MM-dd"),
            Comments   = "Test Sales Order for Inventory Counting Unit Test",

            DocumentLines = new[] {
                new {
                    ItemCode      = testItem,
                    Quantity      = 80,
                    WarehouseCode = TestConstants.SessionInfo.Warehouse,
                    UnitPrice     = 10.0,
                    UseBaseUnits  = "tNO",
                }
            }
        };

        (bool success, string? errorMessage, var result) = await sboCompany.PostAsync<JsonDocument>("Orders", data);
        Assert.That(success, Is.True, $"Sales Order creation should succeed. Error: {errorMessage}");
        Assert.That(result, Is.Not.Null, "Sales Order creation result should not be null");

        // Extract DocEntry from result for verification 
        string? docEntry = result?.RootElement.GetProperty("DocEntry").ToString();
        Assert.That(docEntry, Is.Not.Null.And.Not.Empty, "DocEntry should be returned from goods receipt creation");
        await TestContext.Out.WriteLineAsync($"Created Sales Order with DocEntry: {docEntry}");
        return int.Parse(docEntry);
    }

    private async Task<int> ReleaseToPickList() {
        var data = new {
            Name       = $"Pick List {DateTime.Now:yyyyMMddHHmmss}",
            ObjectType = "17",
            PickListsLines = new[] {
                new {
                    BaseObjectType = 17,
                    OrderEntry     = SalesEntry,
                    OrderRowID     = 0,
                    ReleasedQuantity = 80,
                }
            }
        };
        (bool success, string? errorMessage, var result) = await sboCompany.PostAsync<JsonDocument>("PickLists", data);
        Assert.That(success, Is.True, $"Pick List creation should succeed. Error: {errorMessage}");
        Assert.That(result, Is.Not.Null, "Pick List creation result should not be null");
        
        //Extract PickEntry from result for verification
        string? pickEntry = result?.RootElement.GetProperty("Absoluteentry").ToString();
        Assert.That(pickEntry, Is.Not.Null.And.Not.Empty, "PickEntry should be returned from pick list creation");
        await TestContext.Out.WriteLineAsync($"Created Pick List with PickEntry: {pickEntry}");
        return int.Parse(pickEntry);
    }
}