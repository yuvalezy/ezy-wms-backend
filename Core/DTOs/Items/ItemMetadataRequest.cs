namespace Core.DTOs.Items;

/// <summary>
/// Request model for updating item metadata in external system (SAP Business One)
/// Contains flexible metadata fields - external system validates field names and permissions
/// </summary>
public class ItemMetadataRequest {
    /// <summary>
    /// Metadata field values as key-value pairs
    /// External system (SAP) will validate field names and read-only restrictions
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();
}