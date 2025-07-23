namespace Core.DTOs.Items;

/// <summary>
/// Request model for updating item metadata fields via API
/// Only writable fields should be included in the metadata dictionary
/// </summary>
public class UpdateItemMetadataRequest {
    /// <summary>
    /// Metadata field values as key-value pairs
    /// Key must match configured metadata definition ID (writable fields only)
    /// Value must be compatible with configured field type
    /// Read-only fields (ItemCode, ItemName) will be rejected if included
    /// </summary>
    public required Dictionary<string, object?> Metadata { get; set; } = new();
}