using Core.DTOs.Transfer;
using Core.Entities;
using Core.Enums;
using Infrastructure.Services;

namespace UnitTests.Unit.Services;

[TestFixture]
public class TransferProcessingServiceTests {
    [Test]
    public void CalculateTransferQuantity_WhenNoBinAllocations_UsesLineQuantities() {
        var lines = new[] {
            new TransferLine {
                ItemCode = "ITEM-001",
                Date = DateTime.UtcNow,
                Quantity = 5,
                Type = SourceTarget.Source,
                BinEntry = null
            }
        };

        var quantity = TransferProcessingService.CalculateTransferQuantity(
            lines,
            Array.Empty<TransferCreationBinResponse>(),
            Array.Empty<TransferCreationBinResponse>());

        Assert.That(quantity, Is.EqualTo(5));
    }

    [Test]
    public void CalculateTransferQuantity_WhenBinAllocationsExist_UsesBinQuantitiesOnly() {
        var lines = new[] {
            new TransferLine {
                ItemCode = "ITEM-001",
                Date = DateTime.UtcNow,
                Quantity = 5,
                Type = SourceTarget.Source,
                BinEntry = 10
            },
            new TransferLine {
                ItemCode = "ITEM-001",
                Date = DateTime.UtcNow,
                Quantity = 5,
                Type = SourceTarget.Target,
                BinEntry = 20
            }
        };

        var sourceBins = new[] {
            new TransferCreationBinResponse { BinEntry = 10, Quantity = 5 }
        };
        var targetBins = new[] {
            new TransferCreationBinResponse { BinEntry = 20, Quantity = 5 }
        };

        var quantity = TransferProcessingService.CalculateTransferQuantity(lines, sourceBins, targetBins);

        Assert.That(quantity, Is.EqualTo(5));
    }
}
