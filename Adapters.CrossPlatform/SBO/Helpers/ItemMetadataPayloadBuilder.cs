using System.Globalization;
using System.Text.Json;
using Core.Models.Settings;
using Core.Utils;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

/// <summary>
/// Pure, testable half of <see cref="ItemMetadataProcessor"/>: turns a raw SAP item response into WMS
/// metadata values and turns a WMS update into the SAP PATCH payload. No I/O.
///
/// Storage model per <see cref="MetadataDefinition"/>:
/// <list type="bullet">
///   <item><c>StoreIn</c> unset → the value lives in the SAP property named <c>Id</c> (scaled by <c>ScaleByField</c>).</item>
///   <item><c>StoreIn</c> set → the value lives as key <c>Id</c> inside the JSON blob held by the SAP text UDF
///   named <c>StoreIn</c>, unscaled. Read falls back to the SAP property when the blob has no entry. The SAP
///   property is only written when <c>SyncToSap</c> is true — or zeroed (with its <c>MirrorTo</c> twin) when
///   <c>ClearSapField</c> is true, so SAP cannot derive other fields from stale values.</item>
///   <item><c>FixedValue</c> set → constant, written on every save.</item>
///   <item><c>Calculated</c> set → recomputed whenever a dependency is edited (client value is not trusted).</item>
/// </list>
/// </summary>
public static class ItemMetadataPayloadBuilder {
    /// <summary>A parsed JSON blob: existing keys kept verbatim so unknown data written by others survives a merge.</summary>
    public sealed class Blob {
        public Dictionary<string, object?> Entries { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool Touched { get; set; }
    }

    public static string BuildSelectFields(MetaDataDefinitions definitions) {
        if (definitions.MetadataDefinition.Length == 0) {
            return string.Empty;
        }

        var selectFields = definitions.MetadataDefinition
            .Select(field => field.Id)
            .Concat(definitions.MetadataDefinition.Select(field => field.ScaleByField))
            .Concat(definitions.MetadataDefinition.Select(field => field.StoreIn))
            .Where(fieldId => !string.IsNullOrEmpty(fieldId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return string.Join(",", selectFields);
    }

    public static IEnumerable<string> ScaleFieldIds(MetaDataDefinitions definitions) =>
        definitions.MetadataDefinition
            .Select(f => f.ScaleByField)
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> StoreInIds(MetaDataDefinitions definitions) =>
        definitions.MetadataDefinition
            .Select(f => f.StoreIn)
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>Scale field ID → value, only positive decimals count.</summary>
    public static Dictionary<string, decimal> BuildScaleValueMap(JsonElement response, MetaDataDefinitions definitions) {
        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var fieldId in ScaleFieldIds(definitions)) {
            if (TryGetPropertyIgnoreCase(response, fieldId, out var el)
                && el.TryGetDecimal(out var v) && v > 0) {
                map[fieldId] = v;
            }
        }
        return map;
    }

    /// <summary>
    /// Parses every StoreIn UDF present in the response. A malformed or empty blob is treated as empty
    /// (logged) — it will simply be rewritten on the next save.
    /// </summary>
    public static Dictionary<string, Blob> ParseBlobs(JsonElement response, MetaDataDefinitions definitions, ILogger logger) {
        var blobs = new Dictionary<string, Blob>(StringComparer.OrdinalIgnoreCase);
        foreach (var storeIn in StoreInIds(definitions)) {
            var blob = new Blob();
            blobs[storeIn] = blob;

            if (!TryGetPropertyIgnoreCase(response, storeIn, out var el)
                || el.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(el.GetString())) {
                continue;
            }

            try {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(el.GetString()!);
                if (parsed == null) continue;
                foreach (var kvp in parsed) {
                    blob.Entries[kvp.Key] = kvp.Value;
                }
            }
            catch (JsonException ex) {
                logger.LogWarning(ex, "UDF {StoreIn} does not contain valid JSON; treating as empty", storeIn);
            }
        }
        return blobs;
    }

    /// <summary>
    /// Resolves every definition (hidden ones included) to its WMS-side value.
    /// FixedValue → constant; StoreIn blob entry → unscaled; otherwise SAP property × scale.
    /// </summary>
    public static Dictionary<string, object?> ExtractValues(
        JsonElement response,
        MetaDataDefinitions definitions,
        Dictionary<string, Blob> blobs,
        Dictionary<string, decimal> scaleValues,
        ILogger logger) {
        var metadata = new Dictionary<string, object?>();

        foreach (var fieldDef in definitions.MetadataDefinition) {
            if (fieldDef.FixedValue != null) {
                metadata[fieldDef.Id] = ConvertFixedValue(fieldDef);
                continue;
            }

            if (!string.IsNullOrEmpty(fieldDef.StoreIn)
                && blobs.TryGetValue(fieldDef.StoreIn, out var blob)
                && blob.Entries.TryGetValue(fieldDef.Id, out var stored)) {
                metadata[fieldDef.Id] = stored is JsonElement je
                    ? ConvertSapValueToWmsType(je, fieldDef.Type)
                    : stored;
                continue;
            }

            if (TryGetPropertyIgnoreCase(response, fieldDef.Id, out var propertyValue)) {
                object? convertedValue = ConvertSapValueToWmsType(propertyValue, fieldDef.Type);

                if (!string.IsNullOrEmpty(fieldDef.ScaleByField)
                    && scaleValues.TryGetValue(fieldDef.ScaleByField, out var scale)
                    && convertedValue is decimal d) {
                    convertedValue = d * scale;
                }

                metadata[fieldDef.Id] = convertedValue;
            }
            else {
                logger.LogDebug("SAP property {FieldId} not found in response", fieldDef.Id);
                metadata[fieldDef.Id] = null;
            }
        }

        return metadata;
    }

    /// <summary>Values the frontend is allowed to see: everything except <c>Hidden</c> definitions.</summary>
    public static Dictionary<string, object?> VisibleValues(Dictionary<string, object?> all, MetaDataDefinitions definitions) {
        var hidden = definitions.MetadataDefinition.Where(d => d.Hidden).Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return all.Where(kvp => !hidden.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Request fields that may be written by the user: known, not ReadOnly, not Hidden, no FixedValue.
    /// Keys are normalised to the configured <c>Id</c> casing.
    /// </summary>
    public static Dictionary<string, object?> FilterWritableFields(
        Dictionary<string, object?> requestMetadata, MetaDataDefinitions definitions, ILogger logger) {
        var writableFields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in requestMetadata) {
            var fieldDef = Find(definitions, kvp.Key);

            if (fieldDef == null) {
                logger.LogWarning("Skipping unknown field {FieldId} from update", kvp.Key);
            }
            else if (fieldDef.ReadOnly || fieldDef.Hidden || fieldDef.FixedValue != null) {
                logger.LogWarning("Skipping non-editable field {FieldId} from update", kvp.Key);
            }
            else {
                writableFields[fieldDef.Id] = kvp.Value;
                logger.LogDebug("Including writable field {FieldId} in update", fieldDef.Id);
            }
        }

        return writableFields;
    }

    /// <summary>
    /// Builds the SAP PATCH payload.
    /// <paramref name="currentValues"/> are the item's values as loaded (all definitions), <paramref name="writableFields"/>
    /// the user's changes. Calculated fields are re-evaluated and fixed values injected; the returned payload holds only
    /// SAP properties that must change plus the serialised blob(s) that were touched.
    /// </summary>
    public static Dictionary<string, object?> BuildSapPayload(
        MetaDataDefinitions definitions,
        Dictionary<string, object?> currentValues,
        Dictionary<string, Blob> blobs,
        Dictionary<string, object?> writableFields,
        Dictionary<string, decimal> scaleValues,
        string itemCode,
        ILogger logger) {
        var merged = new Dictionary<string, object?>(currentValues, StringComparer.OrdinalIgnoreCase);
        var touched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in writableFields) {
            var fieldDef = Find(definitions, kvp.Key)!;
            merged[fieldDef.Id] = ConvertWmsValueToSapType(kvp.Value, fieldDef, logger);
            touched.Add(fieldDef.Id);
        }

        // Calculated fields: the backend is authoritative. Recomputed only when one of the dependencies
        // was edited in this request (an unrelated save must not rewrite SAP). When every dependency is
        // present the formula result replaces whatever the client sent; when a dependency is missing
        // (manual edit with ClearDependenciesOnManualEdit) the merged value stands.
        foreach (var fieldDef in definitions.MetadataDefinition.Where(f => f.Calculated != null)) {
            var calc = fieldDef.Calculated!;
            if (!calc.Dependencies.Any(touched.Contains)) continue;

            var depsPresent = calc.Dependencies.All(dep =>
                merged.TryGetValue(dep, out var v) && v != null);
            if (!depsPresent) continue;

            var result = MetadataFormulaEvaluator.Evaluate(calc.Formula, merged, calc.Precision);
            if (result == null) {
                logger.LogWarning("Formula for {FieldId} could not be evaluated for item {ItemCode}; leaving value unchanged",
                    fieldDef.Id, itemCode);
                continue;
            }

            merged[fieldDef.Id] = result.Value;
            touched.Add(fieldDef.Id);
        }

        foreach (var fieldDef in definitions.MetadataDefinition.Where(f => f.FixedValue != null)) {
            merged[fieldDef.Id] = ConvertFixedValue(fieldDef);
            touched.Add(fieldDef.Id);
        }

        var sapPayload = new Dictionary<string, object?>();

        foreach (var fieldDef in definitions.MetadataDefinition.Where(f => touched.Contains(f.Id))) {
            var value = merged[fieldDef.Id];

            if (!string.IsNullOrEmpty(fieldDef.StoreIn)) {
                if (!blobs.TryGetValue(fieldDef.StoreIn, out var blob)) {
                    blob = new Blob();
                    blobs[fieldDef.StoreIn] = blob;
                }
                blob.Entries[fieldDef.Id] = value;
                blob.Touched = true;
                logger.LogDebug("Storing field {FieldName} = {Value} in {StoreIn}", fieldDef.Id, value, fieldDef.StoreIn);

                if (fieldDef.ClearSapField) {
                    // SAP derives e.g. volume from L×W×H and ignores a patched volume while any dimension is
                    // non-zero — so the SAP-side copy of a blob-stored field must be zeroed, twin included.
                    var cleared = ClearedSapValue(fieldDef.Type);
                    sapPayload[fieldDef.Id] = cleared;
                    if (!string.IsNullOrEmpty(fieldDef.MirrorTo)) sapPayload[fieldDef.MirrorTo] = cleared;
                    logger.LogDebug("Clearing SAP field {FieldName} (and mirror {MirrorField})", fieldDef.Id, fieldDef.MirrorTo);
                }

                if (!fieldDef.SyncToSap) continue;
            }

            var sapValue = value;

            // When a field is configured to scale, the scale factor MUST be available and positive.
            // Writing an unscaled value to SAP would corrupt stock-critical data, so fail loudly.
            if (!string.IsNullOrEmpty(fieldDef.ScaleByField)) {
                if (!scaleValues.TryGetValue(fieldDef.ScaleByField, out var scale) || scale <= 0) {
                    throw new InvalidOperationException(
                        $"Cannot scale field '{fieldDef.Id}' for item '{itemCode}': scale factor " +
                        $"'{fieldDef.ScaleByField}' is missing or not positive in SAP. Aborting update to avoid writing unscaled data.");
                }

                if (sapValue is decimal d) {
                    sapValue = d / scale;
                }
            }

            sapPayload[fieldDef.Id] = sapValue;
            logger.LogDebug("Adding field {FieldName} = {Value} to SAP payload", fieldDef.Id, sapValue);

            // Mirror the value to the configured target field (e.g., Purchase → Sales)
            if (!string.IsNullOrEmpty(fieldDef.MirrorTo)) {
                sapPayload[fieldDef.MirrorTo] = sapValue;
                logger.LogDebug("Mirroring field {FieldName} → {MirrorField} = {Value}", fieldDef.Id, fieldDef.MirrorTo, sapValue);
            }
        }

        foreach (var kvp in blobs.Where(b => b.Value.Touched)) {
            sapPayload[kvp.Key] = SerializeBlob(kvp.Value);
        }

        return sapPayload;
    }

    private static object? ClearedSapValue(MetadataFieldType type) => type switch {
        MetadataFieldType.Decimal => 0m,
        MetadataFieldType.Integer => 0,
        _ => null
    };

    public static string SerializeBlob(Blob blob) =>
        JsonSerializer.Serialize(blob.Entries, new JsonSerializerOptions { WriteIndented = false });

    public static MetadataDefinition? Find(MetaDataDefinitions definitions, string id) =>
        definitions.MetadataDefinition.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase));

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value) {
        if (element.ValueKind == JsonValueKind.Object) {
            if (element.TryGetProperty(name, out value)) return true;
            foreach (var prop in element.EnumerateObject()) {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)) {
                    value = prop.Value;
                    return true;
                }
            }
        }
        value = default;
        return false;
    }

    public static object? ConvertFixedValue(MetadataDefinition fieldDef) {
        var raw = fieldDef.FixedValue;
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return fieldDef.Type switch {
            MetadataFieldType.Decimal => decimal.Parse(raw, NumberStyles.Number, CultureInfo.InvariantCulture),
            MetadataFieldType.Integer => int.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture),
            MetadataFieldType.Date => DateTime.Parse(raw, CultureInfo.InvariantCulture),
            _ => raw
        };
    }

    /// <summary>Converts SAP JSON values (or blob entries) to WMS field types; falls back to the raw string.</summary>
    public static object? ConvertSapValueToWmsType(JsonElement sapValue, MetadataFieldType expectedType) {
        if (sapValue.ValueKind == JsonValueKind.Null || sapValue.ValueKind == JsonValueKind.Undefined) {
            return null;
        }

        try {
            return expectedType switch {
                MetadataFieldType.String => sapValue.ValueKind == JsonValueKind.String ? sapValue.GetString() : sapValue.ToString(),
                MetadataFieldType.Decimal => sapValue.ValueKind == JsonValueKind.String
                    ? decimal.Parse(sapValue.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture)
                    : sapValue.GetDecimal(),
                MetadataFieldType.Integer => sapValue.ValueKind == JsonValueKind.String
                    ? int.Parse(sapValue.GetString()!, NumberStyles.Integer, CultureInfo.InvariantCulture)
                    : sapValue.GetInt32(),
                MetadataFieldType.Date => sapValue.GetDateTime(),
                _ => sapValue.ToString()
            };
        }
        catch (Exception) {
            return sapValue.ToString();
        }
    }

    /// <summary>
    /// Converts a WMS request value to the CLR type SAP expects. Request values arrive as
    /// <see cref="JsonElement"/> and are unwrapped first. Throws on conversion failure rather than
    /// silently passing the raw value through — an unconverted value would bypass scaling.
    /// </summary>
    public static object? ConvertWmsValueToSapType(object? wmsValue, MetadataDefinition fieldDef, ILogger logger) {
        if (wmsValue == null) {
            return null;
        }

        var raw = wmsValue is JsonElement je ? UnwrapJsonElement(je) : wmsValue;
        if (raw == null) {
            return null;
        }

        try {
            return fieldDef.Type switch {
                MetadataFieldType.String => raw.ToString(),
                MetadataFieldType.Decimal => Convert.ToDecimal(raw, CultureInfo.InvariantCulture),
                MetadataFieldType.Integer => Convert.ToInt32(raw, CultureInfo.InvariantCulture),
                MetadataFieldType.Date => raw is DateTime dt ? dt : DateTime.Parse(raw.ToString()!, CultureInfo.InvariantCulture),
                _ => raw
            };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to convert value {Value} for field {FieldId}", raw, fieldDef.Id);
            throw new InvalidOperationException(
                $"Field '{fieldDef.Id}' value '{raw}' could not be converted to {fieldDef.Type}.", ex);
        }
    }

    private static object? UnwrapJsonElement(JsonElement el) => el.ValueKind switch {
        JsonValueKind.Number => el.TryGetDecimal(out var d) ? d : el.GetDouble(),
        JsonValueKind.String => el.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => el.ToString()
    };
}
