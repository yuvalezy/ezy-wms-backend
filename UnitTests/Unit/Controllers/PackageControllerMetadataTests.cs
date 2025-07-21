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
    public void PackageDto_MetadataDefinitions_CanBeSet() {
        // Arrange
        var metadataDefinitions = new PackageMetadataDefinition[] {
            new() { Id = "Volume", Description = "Volume", Type = MetadataFieldType.Decimal },
            new() { Id = "Note", Description = "Note", Type = MetadataFieldType.String }
        };

        // Act
        var packageDto = new PackageDto {
            Id = Guid.NewGuid(),
            Barcode = "TEST_BARCODE",
            WhsCode = "TEST_WHS",
            MetadataDefinitions = metadataDefinitions
        };

        // Assert
        Assert.That(packageDto.MetadataDefinitions, Is.Not.Null);
        Assert.That(packageDto.MetadataDefinitions.Length, Is.EqualTo(2));
        Assert.That(packageDto.MetadataDefinitions[0].Id, Is.EqualTo("Volume"));
        Assert.That(packageDto.MetadataDefinitions[0].Type, Is.EqualTo(MetadataFieldType.Decimal));
    }

    [Test]
    public void PackageDto_MetadataDefinitions_DefaultIsEmptyArray() {
        // Arrange & Act
        var packageDto = new PackageDto {
            Id = Guid.NewGuid(),
            Barcode = "TEST_BARCODE",
            WhsCode = "TEST_WHS"
        };

        // Assert
        Assert.That(packageDto.MetadataDefinitions, Is.Not.Null);
        Assert.That(packageDto.MetadataDefinitions, Is.Empty);
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