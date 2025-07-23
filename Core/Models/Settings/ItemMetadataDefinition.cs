namespace Core.Models.Settings;

/// <summary>
/// Defines a custom metadata field that can be configured for items loaded from external systems
/// </summary>
public class ItemMetadataDefinition {
    /// <summary>
    /// Unique identifier for the metadata field (e.g., "ItemCode", "PurchaseUnitVolume", "U_B1SStdTP")
    /// Must match one of the supported item fields exactly
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
    
    /// <summary>
    /// Whether this field is required when updating item metadata
    /// Cannot be true for read-only fields
    /// </summary>
    public bool Required { get; set; } = false;
    
    /// <summary>
    /// Whether this field is read-only (loaded from external system, cannot be modified)
    /// ItemCode and ItemName are always read-only
    /// </summary>
    public bool ReadOnly { get; set; } = false;
}