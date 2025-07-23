namespace Core.DTOs.Items;

/// <summary>
/// Response model containing complete item metadata from external system (SAP Business One)
/// Contains flexible metadata fields - external system determines available fields
/// </summary>
public class ItemMetadataResponse {
    /// <summary>
    /// All metadata field values as key-value pairs
    /// External system (SAP) determines which fields are available and their values
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();
}