using Core.Models.Settings;
using System.Linq;

namespace UnitTests.Unit.Settings;

[TestFixture]
public class PackageMetadataTests {
    
    [Test]
    public void ValidateMetadataDefinitions_ValidConfiguration_ReturnsNoErrors() {
        // Arrange
        var settings = new PackageSettings {
            MetadataDefinition = new[] {
                new PackageMetadataDefinition { Id = "Volume", Description = "Volume", Type = MetadataFieldType.Decimal },
                new PackageMetadataDefinition { Id = "Note", Description = "Note", Type = MetadataFieldType.String },
                new PackageMetadataDefinition { Id = "ExpiryDate", Description = "Expiry Date", Type = MetadataFieldType.Date }
            }
        };
        
        // Act
        var errors = settings.ValidateMetadataDefinitions();
        
        // Assert
        Assert.That(errors, Is.Empty);
    }
    
    [Test]
    public void ValidateMetadataDefinitions_DuplicateIds_ReturnsError() {
        // Arrange
        var settings = new PackageSettings {
            MetadataDefinition = new[] {
                new PackageMetadataDefinition { Id = "Volume", Description = "Volume", Type = MetadataFieldType.Decimal },
                new PackageMetadataDefinition { Id = "volume", Description = "Volume 2", Type = MetadataFieldType.String }
            }
        };
        
        // Act
        var errors = settings.ValidateMetadataDefinitions().ToList();
        
        // Assert
        Assert.That(errors, Is.Not.Empty);
        Assert.That(errors[0], Does.Contain("Duplicate metadata definition ID: Volume"));
    }
    
    [Test]
    public void ValidateMetadataDefinitions_InvalidId_ReturnsError() {
        // Arrange
        var settings = new PackageSettings {
            MetadataDefinition = new[] {
                new PackageMetadataDefinition { Id = "Volume Weight", Description = "Volume Weight", Type = MetadataFieldType.Decimal },
                new PackageMetadataDefinition { Id = "2Volume", Description = "Volume", Type = MetadataFieldType.String },
                new PackageMetadataDefinition { Id = "", Description = "Empty ID", Type = MetadataFieldType.String }
            }
        };
        
        // Act
        var errors = settings.ValidateMetadataDefinitions().ToList();
        
        // Assert
        Assert.That(errors.Count, Is.EqualTo(3));
        Assert.That(errors.Any(e => e.Contains("Volume Weight")));
        Assert.That(errors.Any(e => e.Contains("2Volume")));
        Assert.That(errors.Any(e => e.Contains("must be non-empty")));
    }
    
    [Test]
    public void ValidateMetadataDefinitions_EmptyDescription_ReturnsError() {
        // Arrange
        var settings = new PackageSettings {
            MetadataDefinition = new[] {
                new PackageMetadataDefinition { Id = "Volume", Description = "", Type = MetadataFieldType.Decimal },
                new PackageMetadataDefinition { Id = "Note", Description = "   ", Type = MetadataFieldType.String }
            }
        };
        
        // Act
        var errors = settings.ValidateMetadataDefinitions().ToList();
        
        // Assert
        Assert.That(errors.Count, Is.EqualTo(2));
        Assert.That(errors.Any(e => e.Contains("Empty description for metadata ID: Volume")));
        Assert.That(errors.Any(e => e.Contains("Empty description for metadata ID: Note")));
    }
    
    [Test]
    public void ValidateMetadataDefinitions_ValidIdentifiers_AcceptsCorrectFormats() {
        // Arrange
        var settings = new PackageSettings {
            MetadataDefinition = new[] {
                new PackageMetadataDefinition { Id = "Volume", Description = "Volume", Type = MetadataFieldType.Decimal },
                new PackageMetadataDefinition { Id = "_Weight", Description = "Weight", Type = MetadataFieldType.Decimal },
                new PackageMetadataDefinition { Id = "Weight_KG", Description = "Weight KG", Type = MetadataFieldType.Decimal },
                new PackageMetadataDefinition { Id = "Expiry123", Description = "Expiry", Type = MetadataFieldType.Date }
            }
        };
        
        // Act
        var errors = settings.ValidateMetadataDefinitions();
        
        // Assert
        Assert.That(errors, Is.Empty);
    }
    
    [Test]
    public void ValidateMetadataDefinitions_EmptyArray_ReturnsNoErrors() {
        // Arrange
        var settings = new PackageSettings {
            MetadataDefinition = []
        };
        
        // Act
        var errors = settings.ValidateMetadataDefinitions();
        
        // Assert
        Assert.That(errors, Is.Empty);
    }
    
    [Test]
    public void MetadataDefinition_DefaultValue_IsEmptyArray() {
        // Arrange & Act
        var settings = new PackageSettings();
        
        // Assert
        Assert.That(settings.MetadataDefinition, Is.Not.Null);
        Assert.That(settings.MetadataDefinition, Is.Empty);
    }
    
    [Test]
    public void PackageMetadataDefinition_RequiredProperties_CanBeSet() {
        // Arrange & Act
        var definition = new PackageMetadataDefinition {
            Id = "TestId",
            Description = "Test Description",
            Type = MetadataFieldType.String
        };
        
        // Assert
        Assert.That(definition.Id, Is.EqualTo("TestId"));
        Assert.That(definition.Description, Is.EqualTo("Test Description"));
        Assert.That(definition.Type, Is.EqualTo(MetadataFieldType.String));
    }
    
    [Test]
    public void MetadataFieldType_AllValues_AreAvailable() {
        // Act & Assert
        Assert.DoesNotThrow(() => {
            var stringType = MetadataFieldType.String;
            var decimalType = MetadataFieldType.Decimal;
            var dateType = MetadataFieldType.Date;
            
            Assert.That(stringType, Is.EqualTo(MetadataFieldType.String));
            Assert.That(decimalType, Is.EqualTo(MetadataFieldType.Decimal));
            Assert.That(dateType, Is.EqualTo(MetadataFieldType.Date));
        });
    }
}