using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.Picking.PickingCancellationHelpers;

public class CheckTransferHelper(
    int                            pickEntry,
    PickingSelectionResponse[]     selection,
    WebApplicationFactory<Program> factory,
    int                            binEntry,
    int                            salesEntry,
    string                         itemCode,
    SboCompany                     sboCompany,
    Guid                           transferId) {
    public async Task Validate() {
        int cancelTransferEntry = await GetCancelTransferEntry();
        await ValidateCancelTransfer(cancelTransferEntry);
        await ValidatePickingCancelTransferData();
    }


    private async Task<int> GetCancelTransferEntry() {
        var response = await sboCompany.GetAsync<JsonDocument>($"StockTransfers?$select=DocEntry,DocNum,DocDate,DocumentStatus&$top=1&$orderby=DocEntry desc");
        return response!.RootElement.GetProperty("value")[0].GetProperty("DocEntry").GetInt32();
    }
    private async Task ValidateCancelTransfer(int docEntry) {
        var response = await sboCompany.GetAsync<JsonDocument>($"StockTransfers({docEntry})");
        Assert.That(response, Is.Not.Null, "Transfer response should not be null");
        
        var root = response!.RootElement;
        
        // Validate basic transfer properties
        Assert.That(root.GetProperty("DocEntry").GetInt32(), Is.EqualTo(docEntry), "DocEntry should match");
        Assert.That(root.GetProperty("DocumentStatus").GetString(), Is.EqualTo("bost_Open"), "Transfer should be open");
        
        // Validate transfer lines
        var transferLines = root.GetProperty("StockTransferLines").EnumerateArray().ToArray();
        var expectedItems = selection.GroupBy(s => s.ItemCode).ToArray();
        
        Assert.That(transferLines.Length, Is.EqualTo(expectedItems.Length), 
            $"Transfer should have {expectedItems.Length} line(s) for the picked items");
        
        foreach (var expectedItem in expectedItems) {
            var transferLine = transferLines.FirstOrDefault(line => line.GetProperty("ItemCode").GetString() == expectedItem.Key);
            
            Assert.That(transferLine.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined), $"Transfer line for item {expectedItem.Key} should exist");
            
            // Validate quantities
            decimal expectedQuantity = expectedItem.Sum(s => s.Quantity);
            decimal actualQuantity = transferLine.GetProperty("Quantity").GetDecimal();
            Assert.That(actualQuantity, Is.EqualTo(expectedQuantity), $"Quantity for item {expectedItem.Key} should match picked quantity");
            
            // Validate bin allocations
            var binAllocations = transferLine.GetProperty("StockTransferLinesBinAllocations").EnumerateArray().ToArray();
            int expectedFromBins = expectedItem.Select(s => s.BinEntry).Distinct().Count();
            int expectedToBins = 1; // All items go to the cancel bin
            int expectedTotalAllocations = expectedFromBins + expectedToBins;
            
            Assert.That(binAllocations.Length, Is.EqualTo(expectedTotalAllocations), 
                $"Item {expectedItem.Key} should have {expectedTotalAllocations} bin allocations");
            
            // Validate from warehouse bin allocations
            var fromAllocations = binAllocations.Where(ba => ba.GetProperty("BinActionType").GetString() == "batFromWarehouse").ToArray();
            Assert.That(fromAllocations.Length, Is.EqualTo(expectedFromBins), $"Item {expectedItem.Key} should have {expectedFromBins} from-warehouse allocations");
            
            // Validate to warehouse bin allocations (should be the cancel bin)
            var toAllocations = binAllocations.Where(ba => ba.GetProperty("BinActionType").GetString() == "batToWarehouse").ToArray();
            Assert.That(toAllocations.Length, Is.EqualTo(1), $"Item {expectedItem.Key} should have 1 to-warehouse allocation");
            Assert.That(toAllocations[0].GetProperty("BinAbsEntry").GetInt32(), Is.EqualTo(binEntry), $"Item {expectedItem.Key} should be transferred to the cancel bin");
            Assert.That(toAllocations[0].GetProperty("Quantity").GetDecimal(), Is.EqualTo(expectedQuantity), $"To-warehouse quantity for item {expectedItem.Key} should equal total picked quantity");
        }
    }
    private async Task ValidatePickingCancelTransferData() {
        using var scope   = factory.Services.CreateScope();
        var       service = scope.ServiceProvider.GetRequiredService<ITransferService>();
        var       data    = await service.PrepareTransferData(transferId);
        Assert.That(data, Is.Not.Null);
        Assert.That(data.ContainsKey(itemCode), Is.True, $"Transfer data should contain item {itemCode}");
        var itemData = data[itemCode];
        Assert.That(itemData.Quantity, Is.EqualTo(960), "Quantity should be 960");
        Assert.That(itemData.SourceBins.Count, Is.EqualTo(1), "Source bins should be 1");;
        var binSource = itemData.SourceBins.First();
        Assert.That(binSource.BinEntry, Is.EqualTo(binEntry), "Source bin should be the cancel bin");
        Assert.That(binSource.Quantity, Is.EqualTo(960), "Source bin quantity should be 960");
        Assert.That(itemData.SourceBins.Count, Is.EqualTo(1), "Target bins should be 1");
        var binTarget = itemData.SourceBins.First();
        Assert.That(binTarget.BinEntry, Is.EqualTo(binEntry), "Target bin should be the cancel bin");
        Assert.That(binTarget.Quantity, Is.EqualTo(960), "Target bin quantity should be 960");
    }
}