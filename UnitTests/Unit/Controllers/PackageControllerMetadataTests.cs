using Core.DTOs.Package;
using Core.Models.Settings;

namespace UnitTests.Unit.Controllers;

[TestFixture]
public class PackageControllerMetadataTests {

    [Test]
    public void UpdatePackageMetadataRequest_PropertiesSetCorrectly() {
        // Arrange
        var metadata = new Dictionary<string, object?> {
            { "Volume", 10.5m },
            { "Note", "Test note" }
        };

        // Act
        var request = new UpdatePackageMetadataRequest {
            Metadata = metadata
        };

        // Assert
        Assert.That(request.Metadata, Is.Not.Null);
        Assert.That(request.Metadata, Is.EqualTo(metadata));
        Assert.That(request.Metadata.Count, Is.EqualTo(2));
    }

    [Test]
    public void PackageDto_CustomAttributes_CanStoreMetadata() {
        // Arrange & Act
        var packageDto = new PackageDto {
            Id = Guid.NewGuid(),
            Barcode = "TEST_BARCODE",
            WhsCode = "TEST_WHS",
            CustomAttributes = new Dictionary<string, object> {
                { "Volume", 10.5m },
                { "Weight", 2.3m },
                { "Note", "Test note" }
            }
        };

        // Assert
        Assert.That(packageDto.CustomAttributes, Is.Not.Null);
        Assert.That(packageDto.CustomAttributes!.Count, Is.EqualTo(3));
        Assert.That(packageDto.CustomAttributes.ContainsKey("Volume"), Is.True);
        Assert.That(packageDto.CustomAttributes["Volume"], Is.EqualTo(10.5m));
    }
}