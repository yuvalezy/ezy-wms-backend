using System.Globalization;
using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;
using Core.Models.Settings;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class ItemMetadataProcessor(
    SboCompany sboCompany,
    MetaDataDefinitions metaDataDefinitions,
    string itemCode,
    ILoggerFactory loggerFactory) : IDisposable {
    private readonly Dictionary<string, object?> cachedMetadata = new();
    private readonly ILogger<ItemMetadataProcessor> logger = loggerFactory.CreateLogger<ItemMetadataProcessor>();
    private bool isDisposed;

    /// <summary>
    /// Retrieves item metadata from SAP Business One Service Layer
    /// Uses filtered fields based on ItemSettings configuration
    /// </summary>
    public async Task<ItemMetadataResponse?> GetItemMetadata() {
        try {
            logger.LogDebug("Fetching metadata for item {ItemCode}", itemCode);

            // Build $select query parameter from configured fields
            var selectFields = BuildSelectFields();
            var endpoint = $"Items('{itemCode}')";

            if (!string.IsNullOrEmpty(selectFields)) {
                endpoint += $"?$select={selectFields}";
            }

            logger.LogDebug("Calling SAP Service Layer endpoint: {Endpoint}", endpoint);

            // Fetch item data from SAP
            var itemData = await sboCompany.GetAsync<JsonElement>(endpoint);

            if (itemData.ValueKind == JsonValueKind.Undefined || itemData.ValueKind == JsonValueKind.Null) {
                logger.LogWarning("Item {ItemCode} not found in SAP Business One", itemCode);
                return null;
            }

            // Convert SAP response to metadata dictionary
            var metadata = ExtractMetadataFromSapResponse(itemData);

            // Cache the metadata for comparison during updates
            cachedMetadata.Clear();
            foreach (var kvp in metadata) {
                cachedMetadata[kvp.Key] = kvp.Value;
            }

            logger.LogDebug("Successfully retrieved metadata for item {ItemCode} with {FieldCount} fields",
                itemCode, metadata.Count);

            return new ItemMetadataResponse {
                Metadata = metadata
            };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to retrieve metadata for item {ItemCode}", itemCode);
            throw new InvalidOperationException($"Unable to retrieve metadata for item '{itemCode}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates item metadata in SAP Business One Service Layer
    /// Uses PATCH method to update only the specified fields
    /// </summary>
    public async Task<ItemMetadataResponse> SetItemMetadata(ItemMetadataRequest request) {
        try {
            logger.LogDebug("Updating metadata for item {ItemCode} with {FieldCount} fields",
                itemCode, request.Metadata.Count);

            // Validate that we have metadata to update
            if (request.Metadata.Count == 0) {
                logger.LogWarning("No metadata fields provided for update of item {ItemCode}", itemCode);
                throw new ArgumentException("No metadata fields provided for update");
            }

            // Filter only writable fields based on configuration
            var writableFields = FilterWritableFields(request.Metadata);

            if (writableFields.Count == 0) {
                logger.LogWarning("No writable fields found in request for item {ItemCode}", itemCode);
                throw new ArgumentException("No writable fields found in request");
            }

            // Pre-fetch scale field values needed to convert WMS values back to SAP scale
            var scaleValues = await FetchScaleFieldValuesAsync();

            // Build SAP update payload
            var updatePayload = BuildSapUpdatePayload(writableFields, scaleValues);

            logger.LogDebug("Updating item {ItemCode} with payload: {Payload}", itemCode,
                JsonSerializer.Serialize(updatePayload, new JsonSerializerOptions { WriteIndented = true }));

            // Execute PATCH request to SAP
            var endpoint = $"Items('{itemCode}')";
            var (success, errorMessage) = await sboCompany.PatchAsync(endpoint, updatePayload);

            if (!success) {
                logger.LogError("Failed to update item {ItemCode} in SAP: {Error}", itemCode, errorMessage);
                throw new InvalidOperationException($"Failed to update item metadata in SAP: {errorMessage}");
            }

            logger.LogInformation("Successfully updated metadata for item {ItemCode}", itemCode);

            // Return updated metadata by fetching the current state
            var updatedMetadata = await GetItemMetadata();
            if (updatedMetadata == null) {
                throw new InvalidOperationException($"Failed to retrieve updated metadata for item '{itemCode}'");
            }

            return updatedMetadata;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to update metadata for item {ItemCode}", itemCode);
            throw new InvalidOperationException($"Unable to update metadata for item '{itemCode}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Builds the $select query parameter from configured metadata fields.
    /// Also includes any ScaleByField references so scale factors are always available
    /// in the SAP response, even if the referenced field is not itself a defined metadata field.
    /// </summary>
    private string BuildSelectFields() {
        if (metaDataDefinitions.MetadataDefinition.Length == 0) {
            return string.Empty;
        }

        var selectFields = metaDataDefinitions.MetadataDefinition
            .Select(field => field.Id)
            .Concat(metaDataDefinitions.MetadataDefinition.Select(field => field.ScaleByField))
            .Where(fieldId => !string.IsNullOrEmpty(fieldId))
            .Distinct()
            .ToList();

        return string.Join(",", selectFields);
    }

    /// <summary>
    /// Builds a map of scale field ID → decimal value from a raw SAP response.
    /// Only includes fields that are referenced by at least one MetadataDefinition.ScaleByField.
    /// </summary>
    private Dictionary<string, decimal> BuildScaleValueMap(JsonElement response) {
        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var fieldId in metaDataDefinitions.MetadataDefinition
            .Select(f => f.ScaleByField)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()) {
            if (response.TryGetProperty(fieldId!, out var el)
                && el.TryGetDecimal(out var v) && v > 0) {
                map[fieldId!] = v;
            }
        }
        return map;
    }

    /// <summary>
    /// Fetches only the scale factor fields from SAP — used before a PATCH when we need
    /// to divide WMS values back to SAP scale without a full metadata load.
    /// </summary>
    private async Task<Dictionary<string, decimal>> FetchScaleFieldValuesAsync() {
        var needed = metaDataDefinitions.MetadataDefinition
            .Select(f => f.ScaleByField)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        if (needed.Count == 0) return new();

        var select = string.Join(",", needed);
        var el = await sboCompany.GetAsync<JsonElement>($"Items('{itemCode}')?$select={select}");
        return BuildScaleValueMap(el);
    }

    /// <summary>
    /// Extracts metadata from SAP JSON response using configured field names.
    /// Fields with ScaleByField are multiplied by the corresponding scale factor value.
    /// </summary>
    private Dictionary<string, object?> ExtractMetadataFromSapResponse(JsonElement sapResponse) {
        var metadata = new Dictionary<string, object?>();
        var scaleValues = BuildScaleValueMap(sapResponse);

        foreach (var fieldDef in metaDataDefinitions.MetadataDefinition) {
            if (sapResponse.TryGetProperty(fieldDef.Id, out var propertyValue)) {
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

    /// <summary>
    /// Converts SAP JSON values to WMS field types
    /// </summary>
    private static object? ConvertSapValueToWmsType(JsonElement sapValue, MetadataFieldType expectedType) {
        if (sapValue.ValueKind == JsonValueKind.Null || sapValue.ValueKind == JsonValueKind.Undefined) {
            return null;
        }

        try {
            return expectedType switch {
                MetadataFieldType.String => sapValue.GetString(),
                MetadataFieldType.Decimal => sapValue.GetDecimal(),
                MetadataFieldType.Integer => sapValue.GetInt32(),
                MetadataFieldType.Date => sapValue.GetDateTime(),
                _ => sapValue.ToString()
            };
        }
        catch (Exception) {
            // If conversion fails, return the raw string value
            return sapValue.ToString();
        }
    }

    /// <summary>
    /// Filters request metadata to include only writable fields based on configuration
    /// </summary>
    private Dictionary<string, object?> FilterWritableFields(Dictionary<string, object?> requestMetadata) {
        var writableFields = new Dictionary<string, object?>();

        foreach (var kvp in requestMetadata) {
            var fieldDef = metaDataDefinitions.MetadataDefinition.FirstOrDefault(f =>
            string.Equals(f.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));

            if (fieldDef != null && !fieldDef.ReadOnly) {
                writableFields[kvp.Key] = kvp.Value;
                logger.LogDebug("Including writable field {FieldId} in update", kvp.Key);
            }
            else if (fieldDef?.ReadOnly == true) {
                logger.LogWarning("Skipping read-only field {FieldId} from update", kvp.Key);
            }
            else {
                logger.LogWarning("Skipping unknown field {FieldId} from update", kvp.Key);
            }
        }

        return writableFields;
    }

    /// <summary>
    /// Builds the SAP update payload using configured field names.
    /// Fields with ScaleByField are divided by the corresponding scale factor before being sent to SAP.
    /// </summary>
    private Dictionary<string, object?> BuildSapUpdatePayload(
        Dictionary<string, object?> wmsFields,
        Dictionary<string, decimal> scaleValues) {
        var sapPayload = new Dictionary<string, object?>();

        foreach (var kvp in wmsFields) {
            var fieldDef = metaDataDefinitions.MetadataDefinition.FirstOrDefault(f =>
                string.Equals(f.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));

            object? convertedValue = ConvertWmsValueToSapType(kvp.Value, kvp.Key);

            // When a field is configured to scale, the scale factor MUST be available and positive.
            // Writing an unscaled value to SAP would corrupt stock-critical data, so fail loudly.
            if (!string.IsNullOrEmpty(fieldDef?.ScaleByField)) {
                if (!scaleValues.TryGetValue(fieldDef.ScaleByField, out var scale) || scale <= 0) {
                    throw new InvalidOperationException(
                        $"Cannot scale field '{kvp.Key}' for item '{itemCode}': scale factor " +
                        $"'{fieldDef.ScaleByField}' is missing or not positive in SAP. Aborting update to avoid writing unscaled data.");
                }

                if (convertedValue is decimal d) {
                    convertedValue = d / scale;
                }
            }

            sapPayload[kvp.Key] = convertedValue;
            logger.LogDebug("Adding field {FieldName} = {Value} to SAP payload",
                kvp.Key, convertedValue);

            // Mirror the value to the configured target field (e.g., Purchase → Sales)
            if (!string.IsNullOrEmpty(fieldDef?.MirrorTo)) {
                sapPayload[fieldDef.MirrorTo] = convertedValue;
                logger.LogDebug("Mirroring field {FieldName} → {MirrorField} = {Value}",
                    kvp.Key, fieldDef.MirrorTo, convertedValue);
            }
        }

        return sapPayload;
    }

    /// <summary>
    /// Converts WMS field values to appropriate SAP types.
    /// Request values arrive as <see cref="JsonElement"/> (System.Text.Json binds object
    /// dictionary values to JsonElement) and are unwrapped to a CLR primitive first.
    /// Throws on conversion failure rather than silently passing the raw value through —
    /// sending an unconverted value would also bypass scaling and write wrong data to SAP.
    /// </summary>
    private object? ConvertWmsValueToSapType(object? wmsValue, string fieldId) {
        if (wmsValue == null) {
            return null;
        }

        var fieldDef = metaDataDefinitions.MetadataDefinition.FirstOrDefault(f =>
        string.Equals(f.Id, fieldId, StringComparison.OrdinalIgnoreCase));

        if (fieldDef == null) {
            return wmsValue;
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
            logger.LogError(ex, "Failed to convert value {Value} for field {FieldId}", raw, fieldId);
            throw new InvalidOperationException(
                $"Field '{fieldId}' value '{raw}' could not be converted to {fieldDef.Type}.", ex);
        }
    }

    /// <summary>
    /// Unwraps a JsonElement (from a deserialized request body) into a CLR primitive.
    /// </summary>
    private static object? UnwrapJsonElement(JsonElement el) => el.ValueKind switch {
        JsonValueKind.Number => el.TryGetDecimal(out var d) ? d : el.GetDouble(),
        JsonValueKind.String => el.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => el.ToString()
    };

    public void Dispose() {
        if (!isDisposed) {
            cachedMetadata.Clear();
            isDisposed = true;
        }
    }
}
