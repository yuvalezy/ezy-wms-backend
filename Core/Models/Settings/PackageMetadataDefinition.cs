namespace Core.Models.Settings;

/// <summary>
/// Defines a custom metadata field that can be configured for packages
/// </summary>
public class PackageMetadataDefinition {
    /// <summary>
    /// Unique identifier for the metadata field (e.g., "Volume", "Weight", "Note")
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// Human-readable description/label for the field
    /// </summary>
    public required string Description { get; set; }
    
    /// <summary>
    /// Data type for validation and UI rendering
    /// </summary>
    public required MetadataFieldType Type { get; set; }
}

/// <summary>
/// Supported data types for package metadata fields
/// </summary>
public enum MetadataFieldType {
    /// <summary>
    /// Text/string value
    /// </summary>
    String,
    
    /// <summary>
    /// Decimal number value
    /// </summary>
    Decimal,
    
    /// <summary>
    /// Date value
    /// </summary>
    Date
}