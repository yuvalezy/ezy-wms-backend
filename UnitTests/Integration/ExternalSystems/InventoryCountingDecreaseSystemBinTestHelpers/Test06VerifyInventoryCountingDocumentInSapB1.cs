using Adapters.CrossPlatform.SBO.Services;
using Core.Enums;
using Core.Interfaces;

namespace UnitTests.Integration.ExternalSystems.InventoryCountingDecreaseSystemBinTestHelpers;

public class Test06VerifyInventoryCountingDocumentInSapB1(
    int                                                               countingEntry,
    SboCompany                                                        sboCompany,
    string                                                            testItem,
    string                                                            testWarehouse,
    List<(int binEntry, string binCode, int quantity, UnitType unit)> binEntries,
    ISettings                                                         settings) {
    private readonly int testBinLocation = settings.Filters.InitialCountingBinEntry!.Value;

    public async Task Execute() {
        var response = await sboCompany.GetAsync<CountingVerification>($"InventoryCountings({countingEntry})");
        Assert.That(response, Is.Not.Null, "Inventory Counting data should be retrievable");

        Assert.That(response.InventoryCountingLines, Is.Not.Null, "Inventory Counting lines should be retrievable");
        Assert.That(response.InventoryCountingLines.Length, Is.EqualTo(binEntries.Count + 1), "Inventory Counting lines should match bin entries count plus system bin entry");

        int systemQuantity = 960;

        foreach (var entry in binEntries) {
            int entryQuantity = entry.quantity;
            if (entry.unit != UnitType.Unit)
                entryQuantity *= 12;
            if (entry.unit == UnitType.Pack)
                entryQuantity *= 4;

            var line = response.InventoryCountingLines.FirstOrDefault(l => l.BinEntry == entry.binEntry);
            Assert.That(line, Is.Not.Null, $"Line for bin entry {entry.binEntry} should be retrievable");
            Assert.That(line.ItemCode, Is.EqualTo(testItem), $"Line for bin entry {entry.binEntry} should have correct item code");
            Assert.That(line.ItemDescription, Is.EqualTo($"Test Item {testItem}"), $"Line for bin entry {entry.binEntry} should have correct item description");
            Assert.That(line.WarehouseCode, Is.EqualTo(testWarehouse), $"Line for bin entry {entry.binEntry} should have correct warehouse code");
            Assert.That(line.BinEntry, Is.EqualTo(entry.binEntry), $"Line for bin entry {entry.binEntry} should have correct bin entry");
            Assert.That(line.Counted, Is.EqualTo("tYES"), $"Line for bin entry {entry.binEntry} should have correct counted value");
            Assert.That(line.CountedQuantity, Is.EqualTo(entryQuantity), $"Line for bin entry {entry.binEntry} should have correct counted quantity");
            Assert.That(line.Variance, Is.EqualTo(entryQuantity), $"Line for bin entry {entry.binEntry} should have correct variance");
            systemQuantity -= entryQuantity;
        }

        var systemLine = response.InventoryCountingLines.FirstOrDefault(l => l.BinEntry == testBinLocation);
        Assert.That(systemLine, Is.Not.Null, $"Line for bin entry {testBinLocation} should be retrievable");
        Assert.That(systemLine.ItemCode, Is.EqualTo(testItem), $"Line for bin entry {testBinLocation} should have correct item code");
        Assert.That(systemLine.ItemDescription, Is.EqualTo($"Test Item {testItem}"), $"Line for bin entry {testBinLocation} should have correct item description");
        Assert.That(systemLine.WarehouseCode, Is.EqualTo(testWarehouse), $"Line for bin entry {testBinLocation} should have correct warehouse code");
        Assert.That(systemLine.BinEntry, Is.EqualTo(testBinLocation), $"Line for bin entry {testBinLocation} should have correct bin entry");
        Assert.That(systemLine.Counted, Is.EqualTo("tYES"), $"Line for bin entry {testBinLocation} should have correct counted value");
        Assert.That(systemLine.CountedQuantity, Is.EqualTo(systemQuantity), $"Line for bin entry {testBinLocation} should have correct counted quantity");
        Assert.That(systemLine.Variance, Is.EqualTo(960 - systemQuantity), $"Line for bin entry {testBinLocation} should have correct variance");
    }
}

public record CountingVerification(CountingVerificationLine[] InventoryCountingLines);

public record CountingVerificationLine(string ItemCode, string ItemDescription, string WarehouseCode, int? BinEntry, string Counted, decimal CountedQuantity, decimal Variance);