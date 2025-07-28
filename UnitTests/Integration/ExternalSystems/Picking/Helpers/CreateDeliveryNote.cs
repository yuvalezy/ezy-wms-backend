using System.Text.Json;
using Adapters.CrossPlatform.SBO.Models;
using Adapters.CrossPlatform.SBO.Services;

namespace UnitTests.Integration.ExternalSystems.Picking.Helpers;

public class CreateDeliveryNote(SboCompany sboCompany, int absEntry, int series, string customerCode) {
    public int DeliveryEntry { get; private set; }

    public async Task Execute() {
        Assert.That(await sboCompany.ConnectCompany(), "Connection to SAP failed");
        var pickingData = await LoadPickingData();
        DeliveryEntry = await CreateDocument(pickingData);
    }


    private async Task<PickListSboResponse> LoadPickingData() {
        var response = await sboCompany.GetAsync<PickListSboResponse>($"PickLists({absEntry})");
        if (response == null) {
            throw new Exception($"Could not find Pick List {absEntry}");
        }

        if (response.Status == "ps_Closed") {
            throw new Exception("Cannot process document if the Status is closed");
        }

        return response;
    }

    private async Task<int> CreateDocument(PickListSboResponse pickingData) {
        var data = new {
            CardCode = customerCode,
            Series = series,
            DocDate = DateTime.Now.ToString("yyyy-MM-dd"),
            DocDueDate = DateTime.Now.ToString("yyyy-MM-dd"),
            Comments = "Test Delivery Note for Picking new Package Unit Test",

            DocumentLines = pickingData.PickListsLines.Select(item =>
            new {
                Quantity = item.PickedQuantity / 12,
                WarehouseCode = TestConstants.SessionInfo.Warehouse,
                UnitPrice = 10.0,
                UseBaseUnits = "tNO",
                BaseType = 17,
                BaseEntry = item.OrderEntry,
                BaseLine = item.OrderRowID,
                DocumentLinesBinAllocations = item.DocumentLinesBinAllocations.Select(bin => new {
                    bin.BinAbsEntry,
                    bin.Quantity,
                    bin.AllowNegativeQuantity,
                    bin.SerialAndBatchNumbersBaseLine,
                    bin.BaseLineNumber,
                })
            }
            )
        };

        (bool success, string? errorMessage, var result) = await sboCompany.PostAsync<JsonDocument>("DeliveryNotes", data);
        Assert.That(success, Is.True, $"Delivery Note creation should succeed. Error: {errorMessage}");
        Assert.That(result, Is.Not.Null, "Delivery Note creation result should not be null");

        // Extract DocEntry from result for verification 
        string? docEntry = result?.RootElement.GetProperty("DocEntry").ToString();
        Assert.That(docEntry, Is.Not.Null.And.Not.Empty, "DocEntry should be returned from goods receipt creation");
        await TestContext.Out.WriteLineAsync($"Created Delivery Note with DocEntry: {docEntry}");
        return int.Parse(docEntry);
    }
}