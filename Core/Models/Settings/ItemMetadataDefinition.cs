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

    /// <summary>
    /// Optional calculation configuration for computed metadata fields
    /// </summary>
    public Calculated? Calculated { get; set; }
}

/// <summary>
/// Defines calculation settings for computed metadata fields
/// </summary>
public class Calculated {
    /// <summary>
    /// The formula expression used to calculate the field value
    /// </summary>
    public required string Formula { get; set; } = "";

    /// <summary>
    /// Array of field IDs that this calculated field depends on
    /// </summary>
    public string[] Dependencies { get; set; } = [];

    /// <summary>
    /// Number of decimal places to round the calculated result to (for numeric fields)
    /// </summary>
    public int Precision { get; set; } = 0;

    /// <summary>
    /// When true, dependency fields are cleared when the calculated field is manually edited
    /// </summary>
    public bool ClearDependenciesOnManualEdit { get; set; } = false;
}