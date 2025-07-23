using System.Text;
using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;
using Core.Models.Settings;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class ItemMetadataProcessor(
    SboCompany sboCompany, 
    ItemSettings itemSettings, 
    string itemCode,
    ILoggerFactory loggerFactory) : IDisposable
{
    private readonly Dictionary<string, object?> _cachedMetadata = new();
    private readonly ILogger<ItemMetadataProcessor> logger = loggerFactory.CreateLogger<ItemMetadataProcessor>();
    private bool _isDisposed;

    /// <summary>
    /// Retrieves item metadata from SAP Business One Service Layer
    /// Uses filtered fields based on ItemSettings configuration
    /// </summary>
    public async Task<ItemMetadataResponse?> GetItemMetadata()
    {
        try
        {
            logger.LogDebug("Fetching metadata for item {ItemCode}", itemCode);

            // Build $select query parameter from configured fields
            var selectFields = BuildSelectFields();
            var endpoint = $"Items('{itemCode}')";
            
            if (!string.IsNullOrEmpty(selectFields))
            {
                endpoint += $"?$select={selectFields}";
            }

            logger.LogDebug("Calling SAP Service Layer endpoint: {Endpoint}", endpoint);

            // Fetch item data from SAP
            var itemData = await sboCompany.GetAsync<JsonElement>(endpoint);
            
            if (itemData.ValueKind == JsonValueKind.Undefined || itemData.ValueKind == JsonValueKind.Null)
            {
                logger.LogWarning("Item {ItemCode} not found in SAP Business One", itemCode);
                return null;
            }

            // Convert SAP response to metadata dictionary
            var metadata = ExtractMetadataFromSapResponse(itemData);
            
            // Cache the metadata for comparison during updates
            _cachedMetadata.Clear();
            foreach (var kvp in metadata)
            {
                _cachedMetadata[kvp.Key] = kvp.Value;
            }

            logger.LogDebug("Successfully retrieved metadata for item {ItemCode} with {FieldCount} fields", 
                itemCode, metadata.Count);

            return new ItemMetadataResponse 
            { 
                Metadata = metadata 
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve metadata for item {ItemCode}", itemCode);
            throw new InvalidOperationException($"Unable to retrieve metadata for item '{itemCode}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates item metadata in SAP Business One Service Layer
    /// Uses PATCH method to update only the specified fields
    /// </summary>
    public async Task<ItemMetadataResponse> SetItemMetadata(ItemMetadataRequest request)
    {
        try
        {
            logger.LogDebug("Updating metadata for item {ItemCode} with {FieldCount} fields", 
                itemCode, request.Metadata.Count);

            // Validate that we have metadata to update
            if (request.Metadata.Count == 0)
            {
                logger.LogWarning("No metadata fields provided for update of item {ItemCode}", itemCode);
                throw new ArgumentException("No metadata fields provided for update");
            }

            // Filter only writable fields based on configuration
            var writableFields = FilterWritableFields(request.Metadata);
            
            if (writableFields.Count == 0)
            {
                logger.LogWarning("No writable fields found in request for item {ItemCode}", itemCode);
                throw new ArgumentException("No writable fields found in request");
            }

            // Build SAP update payload
            var updatePayload = BuildSapUpdatePayload(writableFields);
            
            logger.LogDebug("Updating item {ItemCode} with payload: {Payload}", itemCode, 
                JsonSerializer.Serialize(updatePayload, new JsonSerializerOptions { WriteIndented = true }));

            // Execute PATCH request to SAP
            var endpoint = $"Items('{itemCode}')";
            var (success, errorMessage) = await sboCompany.PatchAsync(endpoint, updatePayload);

            if (!success)
            {
                logger.LogError("Failed to update item {ItemCode} in SAP: {Error}", itemCode, errorMessage);
                throw new InvalidOperationException($"Failed to update item metadata in SAP: {errorMessage}");
            }

            logger.LogInformation("Successfully updated metadata for item {ItemCode}", itemCode);

            // Return updated metadata by fetching the current state
            var updatedMetadata = await GetItemMetadata();
            if (updatedMetadata == null)
            {
                throw new InvalidOperationException($"Failed to retrieve updated metadata for item '{itemCode}'");
            }

            return updatedMetadata;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update metadata for item {ItemCode}", itemCode);
            throw new InvalidOperationException($"Unable to update metadata for item '{itemCode}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Builds the $select query parameter from configured metadata fields
    /// </summary>
    private string BuildSelectFields()
    {
        if (itemSettings.MetadataDefinition.Length == 0)
        {
            return string.Empty;
        }

        var selectFields = itemSettings.MetadataDefinition
            .Select(field => field.Id)
            .Where(fieldId => !string.IsNullOrEmpty(fieldId))
            .Distinct()
            .ToList();

        return string.Join(",", selectFields);
    }


    /// <summary>
    /// Extracts metadata from SAP JSON response using configured field names
    /// </summary>
    private Dictionary<string, object?> ExtractMetadataFromSapResponse(JsonElement sapResponse)
    {
        var metadata = new Dictionary<string, object?>();

        foreach (var fieldDef in itemSettings.MetadataDefinition)
        {
            if (sapResponse.TryGetProperty(fieldDef.Id, out var propertyValue))
            {
                var convertedValue = ConvertSapValueToWmsType(propertyValue, fieldDef.Type);
                metadata[fieldDef.Id] = convertedValue;
            }
            else
            {
                logger.LogDebug("SAP property {FieldId} not found in response", fieldDef.Id);
                metadata[fieldDef.Id] = null;
            }
        }

        return metadata;
    }

    /// <summary>
    /// Converts SAP JSON values to WMS field types
    /// </summary>
    private static object? ConvertSapValueToWmsType(JsonElement sapValue, MetadataFieldType expectedType)
    {
        if (sapValue.ValueKind == JsonValueKind.Null || sapValue.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        try
        {
            return expectedType switch
            {
                MetadataFieldType.String => sapValue.GetString(),
                MetadataFieldType.Decimal => sapValue.GetDecimal(),
                MetadataFieldType.Integer => sapValue.GetInt32(),
                MetadataFieldType.Date => sapValue.GetDateTime(),
                _ => sapValue.ToString()
            };
        }
        catch (Exception)
        {
            // If conversion fails, return the raw string value
            return sapValue.ToString();
        }
    }

    /// <summary>
    /// Filters request metadata to include only writable fields based on configuration
    /// </summary>
    private Dictionary<string, object?> FilterWritableFields(Dictionary<string, object?> requestMetadata)
    {
        var writableFields = new Dictionary<string, object?>();

        foreach (var kvp in requestMetadata)
        {
            var fieldDef = itemSettings.MetadataDefinition.FirstOrDefault(f => 
                string.Equals(f.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));

            if (fieldDef != null && !fieldDef.ReadOnly)
            {
                writableFields[kvp.Key] = kvp.Value;
                logger.LogDebug("Including writable field {FieldId} in update", kvp.Key);
            }
            else if (fieldDef?.ReadOnly == true)
            {
                logger.LogWarning("Skipping read-only field {FieldId} from update", kvp.Key);
            }
            else
            {
                logger.LogWarning("Skipping unknown field {FieldId} from update", kvp.Key);
            }
        }

        return writableFields;
    }

    /// <summary>
    /// Builds the SAP update payload using configured field names
    /// </summary>
    private Dictionary<string, object?> BuildSapUpdatePayload(Dictionary<string, object?> wmsFields)
    {
        var sapPayload = new Dictionary<string, object?>();

        foreach (var kvp in wmsFields)
        {
            var convertedValue = ConvertWmsValueToSapType(kvp.Value, kvp.Key);
            
            sapPayload[kvp.Key] = convertedValue;
            logger.LogDebug("Adding field {FieldName} = {Value} to SAP payload", 
                kvp.Key, convertedValue);
        }

        return sapPayload;
    }

    /// <summary>
    /// Converts WMS field values to appropriate SAP types
    /// </summary>
    private object? ConvertWmsValueToSapType(object? wmsValue, string fieldId)
    {
        if (wmsValue == null)
        {
            return null;
        }

        var fieldDef = itemSettings.MetadataDefinition.FirstOrDefault(f => 
            string.Equals(f.Id, fieldId, StringComparison.OrdinalIgnoreCase));

        if (fieldDef == null)
        {
            return wmsValue;
        }

        try
        {
            return fieldDef.Type switch
            {
                MetadataFieldType.String => wmsValue.ToString(),
                MetadataFieldType.Decimal => Convert.ToDecimal(wmsValue),
                MetadataFieldType.Integer => Convert.ToInt32(wmsValue),
                MetadataFieldType.Date => wmsValue is DateTime dt ? dt : DateTime.Parse(wmsValue.ToString()!),
                _ => wmsValue
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to convert value {Value} for field {FieldId}: {Error}", 
                wmsValue, fieldId, ex.Message);
            return wmsValue; // Return original value if conversion fails
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _cachedMetadata.Clear();
            _isDisposed = true;
        }
    }
}