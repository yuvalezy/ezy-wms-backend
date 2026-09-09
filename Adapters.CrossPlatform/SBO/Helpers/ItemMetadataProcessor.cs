using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;
using Core.Models.Settings;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

/// <summary>
/// Reads and writes item metadata through the Service Layer <c>Items</c> entity.
/// All mapping rules (JSON-blob storage, scaling, mirroring, calculated and fixed values) live in
/// <see cref="ItemMetadataPayloadBuilder"/>; this class only does the HTTP round-trips.
/// </summary>
public class ItemMetadataProcessor(
    SboCompany sboCompany,
    MetaDataDefinitions metaDataDefinitions,
    string itemCode,
    ILoggerFactory loggerFactory) : IDisposable {
    private readonly ILogger<ItemMetadataProcessor> logger = loggerFactory.CreateLogger<ItemMetadataProcessor>();

    /// <summary>Everything a save needs from the current SAP state, fetched in one GET.</summary>
    private sealed record LoadedItem(
        Dictionary<string, object?> Values,
        Dictionary<string, ItemMetadataPayloadBuilder.Blob> Blobs,
        Dictionary<string, decimal> ScaleValues);

    /// <summary>
    /// Retrieves item metadata from SAP Business One Service Layer.
    /// Hidden definitions are stripped — the frontend never sees them.
    /// </summary>
    public async Task<ItemMetadataResponse?> GetItemMetadata() {
        try {
            var loaded = await LoadAsync();
            if (loaded == null) {
                return null;
            }

            var visible = ItemMetadataPayloadBuilder.VisibleValues(loaded.Values, metaDataDefinitions);

            logger.LogDebug("Successfully retrieved metadata for item {ItemCode} with {FieldCount} fields",
                itemCode, visible.Count);

            return new ItemMetadataResponse { Metadata = visible };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to retrieve metadata for item {ItemCode}", itemCode);
            throw new InvalidOperationException($"Unable to retrieve metadata for item '{itemCode}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates item metadata in SAP Business One Service Layer using PATCH.
    /// Only SAP properties that must change (plus touched JSON blobs) are sent.
    /// </summary>
    public async Task<ItemMetadataResponse> SetItemMetadata(ItemMetadataRequest request) {
        try {
            logger.LogDebug("Updating metadata for item {ItemCode} with {FieldCount} fields",
                itemCode, request.Metadata.Count);

            if (request.Metadata.Count == 0) {
                logger.LogWarning("No metadata fields provided for update of item {ItemCode}", itemCode);
                throw new ArgumentException("No metadata fields provided for update");
            }

            var writableFields = ItemMetadataPayloadBuilder.FilterWritableFields(request.Metadata, metaDataDefinitions, logger);

            if (writableFields.Count == 0) {
                logger.LogWarning("No writable fields found in request for item {ItemCode}", itemCode);
                throw new ArgumentException("No writable fields found in request");
            }

            // One GET gives the scale factors, the current blob(s) to merge into and the current values
            // that calculated fields may depend on.
            var loaded = await LoadAsync()
                ?? throw new KeyNotFoundException($"Item '{itemCode}' not found in SAP Business One");

            var updatePayload = ItemMetadataPayloadBuilder.BuildSapPayload(
                metaDataDefinitions, loaded.Values, loaded.Blobs, writableFields, loaded.ScaleValues, itemCode, logger);

            logger.LogDebug("Updating item {ItemCode} with payload: {Payload}", itemCode,
                JsonSerializer.Serialize(updatePayload, new JsonSerializerOptions { WriteIndented = true }));

            var endpoint = $"Items('{itemCode}')";
            var (success, errorMessage) = await sboCompany.PatchAsync(endpoint, updatePayload);

            if (!success) {
                logger.LogError("Failed to update item {ItemCode} in SAP: {Error}", itemCode, errorMessage);
                throw new InvalidOperationException($"Failed to update item metadata in SAP: {errorMessage}");
            }

            logger.LogInformation("Successfully updated metadata for item {ItemCode}", itemCode);

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

    private async Task<LoadedItem?> LoadAsync() {
        logger.LogDebug("Fetching metadata for item {ItemCode}", itemCode);

        var selectFields = ItemMetadataPayloadBuilder.BuildSelectFields(metaDataDefinitions);
        var endpoint = $"Items('{itemCode}')";
        if (!string.IsNullOrEmpty(selectFields)) {
            endpoint += $"?$select={selectFields}";
        }

        logger.LogDebug("Calling SAP Service Layer endpoint: {Endpoint}", endpoint);

        var itemData = await sboCompany.GetAsync<JsonElement>(endpoint);

        if (itemData.ValueKind == JsonValueKind.Undefined || itemData.ValueKind == JsonValueKind.Null) {
            logger.LogWarning("Item {ItemCode} not found in SAP Business One", itemCode);
            return null;
        }

        var scaleValues = ItemMetadataPayloadBuilder.BuildScaleValueMap(itemData, metaDataDefinitions);
        var blobs = ItemMetadataPayloadBuilder.ParseBlobs(itemData, metaDataDefinitions, logger);
        var values = ItemMetadataPayloadBuilder.ExtractValues(itemData, metaDataDefinitions, blobs, scaleValues, logger);

        return new LoadedItem(values, blobs, scaleValues);
    }

    public void Dispose() {
        // Nothing to release; kept so callers can keep the `using` pattern.
    }
}
