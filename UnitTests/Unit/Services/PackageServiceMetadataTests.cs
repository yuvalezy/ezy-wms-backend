using Core.DTOs.Package;
using Core.Models.Settings;

namespace UnitTests.Unit.Services;

[TestFixture]
public class PackageServiceMetadataTests {

    [Test]
    public void UpdatePackageMetadataRequest_CanBeCreated() {
        // Arrange & Act
        var request = new UpdatePackageMetadataRequest {
            Metadata = new Dictionary<string, object?> {
                { "Volume", 10.5m },
                { "Weight", 2.3m },
                { "Note", "Test note" }
            }
        };

        // Assert
        Assert.That(request, Is.Not.Null);
        Assert.That(request.Metadata, Is.Not.Null);
        Assert.That(request.Metadata.Count, Is.EqualTo(3));
        Assert.That(request.Metadata.ContainsKey("Volume"), Is.True);
        Assert.That(request.Metadata["Volume"], Is.EqualTo(10.5m));
    }

    [Test]
    public void UpdatePackageMetadataRequest_SupportsNullValues() {
        // Arrange & Act
        var request = new UpdatePackageMetadataRequest {
            Metadata = new Dictionary<string, object?> {
                { "Volume", 10.5m },
                { "Note", null } // Remove this field
            }
        };

        // Assert
        Assert.That(request.Metadata.ContainsKey("Volume"), Is.True);
        Assert.That(request.Metadata.ContainsKey("Note"), Is.True);
        Assert.That(request.Metadata["Note"], Is.Null);
    }

    [Test]
    public void UpdatePackageMetadataRequest_EmptyMetadata_IsValid() {
        // Arrange & Act
        var request = new UpdatePackageMetadataRequest {
            Metadata = new Dictionary<string, object?>()
        };

        // Assert
        Assert.That(request, Is.Not.Null);
        Assert.That(request.Metadata, Is.Not.Null);
        Assert.That(request.Metadata, Is.Empty);
    }

    [Test]
    public void UpdatePackageMetadataRequest_SupportsVariousDataTypes() {
        // Arrange & Act
        var request = new UpdatePackageMetadataRequest {
            Metadata = new Dictionary<string, object?> {
                { "Volume", 10.5m },        // Decimal
                { "Weight", 5.0 },          // Double
                { "Count", 42 },            // Int
                { "Note", "Test" },         // String
                { "ExpiryDate", DateTime.Today }, // DateTime
                { "Active", true },         // Boolean
                { "Empty", null }           // Null
            }
        };

        // Assert
        Assert.That(request.Metadata.Count, Is.EqualTo(7));
        Assert.That(request.Metadata["Volume"], Is.TypeOf<decimal>());
        Assert.That(request.Metadata["Weight"], Is.TypeOf<double>());
        Assert.That(request.Metadata["Count"], Is.TypeOf<int>());
        Assert.That(request.Metadata["Note"], Is.TypeOf<string>());
        Assert.That(request.Metadata["ExpiryDate"], Is.TypeOf<DateTime>());
        Assert.That(request.Metadata["Active"], Is.TypeOf<bool>());
        Assert.That(request.Metadata["Empty"], Is.Null);
    }
}