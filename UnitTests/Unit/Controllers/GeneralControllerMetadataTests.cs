// using Core.Models.Settings;
//
// namespace UnitTests.Unit.Controllers;
//
// [TestFixture]
// public class GeneralControllerMetadataTests {
//
//     [Test]
//     public void PackageMetadataDefinition_AllFieldTypes_AreSupported() {
//         // Arrange & Act
//         var stringDefinition = new PackageMetadataDefinition {
//             Id = "Note",
//             Description = "Note",
//             Type = MetadataFieldType.String
//         };
//
//         var decimalDefinition = new PackageMetadataDefinition {
//             Id = "Volume", 
//             Description = "Volume",
//             Type = MetadataFieldType.Decimal
//         };
//
//         var dateDefinition = new PackageMetadataDefinition {
//             Id = "ExpiryDate",
//             Description = "Expiry Date", 
//             Type = MetadataFieldType.Date
//         };
//
//         // Assert
//         Assert.That(stringDefinition.Type, Is.EqualTo(MetadataFieldType.String));
//         Assert.That(decimalDefinition.Type, Is.EqualTo(MetadataFieldType.Decimal));
//         Assert.That(dateDefinition.Type, Is.EqualTo(MetadataFieldType.Date));
//     }
//
//     [Test]
//     public void PackageMetadataDefinition_RequiredProperties_CanBeSet() {
//         // Arrange & Act
//         var definition = new PackageMetadataDefinition {
//             Id = "CustomField",
//             Description = "Custom Field Description",
//             Type = MetadataFieldType.String
//         };
//
//         // Assert
//         Assert.That(definition.Id, Is.EqualTo("CustomField"));
//         Assert.That(definition.Description, Is.EqualTo("Custom Field Description"));
//         Assert.That(definition.Type, Is.EqualTo(MetadataFieldType.String));
//     }
//
//     [Test]
//     public void MetadataFieldType_EnumValues_AreCorrect() {
//         // Arrange & Act
//         var stringValue = (int)MetadataFieldType.String;
//         var decimalValue = (int)MetadataFieldType.Decimal;
//         var dateValue = (int)MetadataFieldType.Date;
//
//         // Assert - Test that enum values are consistent
//         Assert.That(stringValue, Is.EqualTo(0));
//         Assert.That(decimalValue, Is.EqualTo(1));
//         Assert.That(dateValue, Is.EqualTo(2));
//     }
//
//     [Test]
//     public void PackageMetadataDefinition_Array_CanBeCreated() {
//         // Arrange & Act
//         var definitions = new PackageMetadataDefinition[] {
//             new() { Id = "Volume", Description = "Volume (m³)", Type = MetadataFieldType.Decimal },
//             new() { Id = "Weight", Description = "Weight (kg)", Type = MetadataFieldType.Decimal },
//             new() { Id = "Note", Description = "Note", Type = MetadataFieldType.String },
//             new() { Id = "ExpiryDate", Description = "Expiry Date", Type = MetadataFieldType.Date }
//         };
//
//         // Assert
//         Assert.That(definitions.Length, Is.EqualTo(4));
//         Assert.That(definitions.All(d => !string.IsNullOrEmpty(d.Id)), Is.True);
//         Assert.That(definitions.All(d => !string.IsNullOrEmpty(d.Description)), Is.True);
//         Assert.That(definitions.Select(d => d.Id), Is.Unique);
//     }
// }