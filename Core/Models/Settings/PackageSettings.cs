using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Models.Settings;

public class PackageSettings {
    public BarcodeSettings Barcode { get; set; } = new();
    public LabelSettings   Label   { get; set; } = new();
    
    /// <summary>
    /// Configurable metadata field definitions for packages
    /// </summary>
    public MetadataDefinition[] MetadataDefinition { get; set; } = [];

    /// <summary>
    /// Validates the metadata definitions for consistency and correctness
    /// </summary>
    /// <returns>List of validation error messages, empty if valid</returns>
    public IEnumerable<string> ValidateMetadataDefinitions() {
        var errors = new List<string>();
        
        // Check for duplicate IDs
        var duplicateIds = MetadataDefinition
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        
        foreach (var duplicateId in duplicateIds) {
            errors.Add($"Duplicate metadata definition ID: {duplicateId}");
        }
        
        // Check for empty/invalid IDs
        var invalidIds = MetadataDefinition
            .Where(x => string.IsNullOrWhiteSpace(x.Id) || 
                       x.Id.Contains(' ') || 
                       !IsValidIdentifier(x.Id));
        
        foreach (var invalid in invalidIds) {
            errors.Add($"Invalid metadata ID '{invalid.Id}': IDs must be non-empty, no spaces, alphanumeric");
        }
        
        // Check for empty descriptions
        var emptyDescriptions = MetadataDefinition
            .Where(x => string.IsNullOrWhiteSpace(x.Description));
        
        foreach (var empty in emptyDescriptions) {
            errors.Add($"Empty description for metadata ID: {empty.Id}");
        }
        
        return errors;
    }

    private static bool IsValidIdentifier(string id) {
        if (string.IsNullOrEmpty(id)) return false;
        
        // Must start with letter or underscore
        if (!char.IsLetter(id[0]) && id[0] != '_') return false;
        
        // Rest must be letters, digits, or underscore
        return id.Skip(1).All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}

public class LabelSettings {
    public bool AutoPrint { get; set; }
}

public class BarcodeSettings {
    public string Prefix      { get; set; } = string.Empty;
    public string Suffix      { get; set; } = string.Empty;
    public int    Length      { get; set; }
    public int    StartNumber { get; set; }
}