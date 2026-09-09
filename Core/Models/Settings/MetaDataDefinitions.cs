using System;
using System.Collections.Generic;
using System.Linq;
using Core.Utils;

namespace Core.Models.Settings;

public class MetaDataDefinitions {
    /// <summary>
    /// Configurable metadata field definitions for items loaded from external systems
    /// External system (SAP) will validate field names and read-only restrictions
    /// </summary>
    public MetadataDefinition[] MetadataDefinition { get; set; } = [];

    /// <summary>
    /// Definitions the frontend may see. <c>Hidden</c> fields (e.g. a fixed volume unit code) are
    /// backend-only: they take part in the SAP write but are never rendered or editable.
    /// </summary>
    public MetadataDefinition[] VisibleDefinitions() =>
        MetadataDefinition.Where(d => !d.Hidden).ToArray();

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
            .Where(x => x is { ReadOnly: true, Required: true });
        
        foreach (var invalid in invalidRequiredReadOnly) {
            errors.Add($"Field '{invalid.Id}' cannot be both ReadOnly and Required");
        }

        var knownIds = new HashSet<string>(
            MetadataDefinition.Select(x => x.Id ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);

        foreach (var field in MetadataDefinition) {
            if (field.Hidden && field.Required) {
                errors.Add($"Field '{field.Id}' cannot be both Hidden and Required");
            }

            if (field.SyncToSap && string.IsNullOrWhiteSpace(field.StoreIn)) {
                errors.Add($"Field '{field.Id}' has SyncToSap but no StoreIn");
            }

            if (field.ClearSapField && (string.IsNullOrWhiteSpace(field.StoreIn) || field.SyncToSap)) {
                errors.Add($"Field '{field.Id}' has ClearSapField but it requires StoreIn and excludes SyncToSap");
            }

            if (!string.IsNullOrWhiteSpace(field.StoreIn) && !IsValidIdentifier(field.StoreIn)) {
                errors.Add($"Field '{field.Id}' has invalid StoreIn '{field.StoreIn}': must be a SAP property name");
            }

            if (field.FixedValue != null && !FixedValueMatchesType(field.FixedValue, field.Type)) {
                errors.Add($"Field '{field.Id}' FixedValue '{field.FixedValue}' is not a valid {field.Type}");
            }

            if (field.Calculated == null) continue;

            foreach (var dep in field.Calculated.Dependencies.Where(d => !knownIds.Contains(d))) {
                errors.Add($"Field '{field.Id}' depends on unknown field '{dep}'");
            }

            foreach (var reference in MetadataFormulaEvaluator.GetReferencedFields(field.Calculated.Formula)
                         .Where(r => !knownIds.Contains(r))) {
                errors.Add($"Field '{field.Id}' formula references unknown field '{reference}'");
            }
        }

        return errors;
    }

    private static bool FixedValueMatchesType(string value, MetadataFieldType type) => type switch {
        MetadataFieldType.Decimal => decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _),
        MetadataFieldType.Integer => int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _),
        MetadataFieldType.Date => DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _),
        _ => true
    };

    private static bool IsValidIdentifier(string id) {
        if (string.IsNullOrEmpty(id)) return false;
        
        // Must start with letter or underscore
        if (!char.IsLetter(id[0]) && id[0] != '_') return false;
        
        // Rest must be letters, digits, or underscore
        return id.Skip(1).All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}