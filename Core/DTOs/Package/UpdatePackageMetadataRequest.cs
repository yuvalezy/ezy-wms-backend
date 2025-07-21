using System.Collections.Generic;

namespace Core.DTOs.Package;

/// <summary>
/// Request model for updating package metadata fields
/// </summary>
public class UpdatePackageMetadataRequest {
    /// <summary>
    /// Metadata field values as key-value pairs
    /// Key must match configured metadata definition ID
    /// Value must be compatible with configured field type
    /// </summary>
    public required Dictionary<string, object?> Metadata { get; set; } = new();
}