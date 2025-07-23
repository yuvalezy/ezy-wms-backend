using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Models.Settings;

public class ItemSettings {
    /// <summary>
    /// Configurable metadata field definitions for items loaded from external systems
    /// External system (SAP) will validate field names and read-only restrictions
    /// </summary>
    public ItemMetadataDefinition[] MetadataDefinition { get; set; } = [];

    /// <summary>
    /// Validates the metadata definitions for basic consistency
    /// Field-specific validation (valid names, read-only restrictions) is handled by external system
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
            errors.Add($"Duplicate item metadata definition ID: {duplicateId}");
        }
        
        // Check for empty/invalid IDs (basic C# identifier validation)
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
            errors.Add($"Empty description for item metadata ID: {empty.Id}");
        }
        
        // Validate that read-only fields cannot be marked as required
        var invalidRequiredReadOnly = MetadataDefinition
            .Where(x => x.ReadOnly && x.Required);
        
        foreach (var invalid in invalidRequiredReadOnly) {
            errors.Add($"Field '{invalid.Id}' cannot be both ReadOnly and Required");
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