using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Core.DTOs.Items;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ItemService(
    IExternalSystemAdapter adapter,
    ISettings settings,
    ILogger<ItemService> logger) : IItemService {
    
    public async Task<ItemMetadataResponse> UpdateItemMetadataAsync(
        string itemCode,
        UpdateItemMetadataRequest request,
        SessionInfo sessionInfo) {
        
        try {
            logger.LogInformation("Updating item metadata for {ItemCode} by user {UserId}", 
                itemCode, sessionInfo.UserId);
            
            // 1. Validate user permissions
            ValidateUpdatePermissions(sessionInfo);
            
            // 2. Validate item exists in external system
            var existingItem = await adapter.GetItemMetadataAsync(itemCode);
            if (existingItem == null) {
                throw new System.Collections.Generic.KeyNotFoundException($"Item with code '{itemCode}' not found");
            }
            
            // 3. Validate metadata against configuration
            ValidateMetadataRequest(request);
            
            // 4. Validate mandatory fields
            ValidateMandatoryFields(request);
            
            // 5. Map to adapter request and update via external adapter
            var adapterRequest = MapToAdapterRequest(request);
            var updatedItem = await adapter.UpdateItemMetadataAsync(itemCode, adapterRequest);
            
            logger.LogInformation("Successfully updated item metadata for {ItemCode} by user {UserId}", 
                itemCode, sessionInfo.UserId);
                
            return updatedItem;
        }
        catch (System.Collections.Generic.KeyNotFoundException) {
            logger.LogWarning("Item not found: {ItemCode}", itemCode);
            throw; // Re-throw as-is for 404 handling
        }
        catch (ValidationException ex) {
            logger.LogWarning("Validation failed for item {ItemCode}: {Error}", itemCode, ex.Message);
            throw; // Re-throw as-is for 400 handling  
        }
        catch (ArgumentException ex) {
            logger.LogWarning("Validation failed for item {ItemCode}: {Error}", itemCode, ex.Message);
            throw; // Re-throw as-is for 400 handling  
        }
        catch (UnauthorizedAccessException ex) {
            logger.LogWarning("Unauthorized access for item {ItemCode} by user {UserId}: {Error}", 
                itemCode, sessionInfo.UserId, ex.Message);
            throw; // Re-throw as-is for 403 handling
        }
        catch (Exception ex) {
            logger.LogError(ex, "Unexpected error updating item metadata for {ItemCode}", itemCode);
            throw new InvalidOperationException("An unexpected error occurred updating item metadata");
        }
    }
    
    public async Task<ItemMetadataResponse?> GetItemMetadataAsync(string itemCode) {
        try {
            logger.LogDebug("Retrieving item metadata for {ItemCode}", itemCode);
            return await adapter.GetItemMetadataAsync(itemCode);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving item metadata for {ItemCode}", itemCode);
            throw new InvalidOperationException($"Unable to retrieve item metadata for {itemCode}");
        }
    }

    /// <summary>
    /// Validates that the user has permission to update item metadata
    /// </summary>
    private static void ValidateUpdatePermissions(SessionInfo sessionInfo) {
        // Check if user has permissions to update item metadata
        if (!sessionInfo.SuperUser && 
            !sessionInfo.Roles.Contains(RoleType.ItemManagement) &&
            !sessionInfo.Roles.Contains(RoleType.ItemManagementSupervisor)) {
            throw new UnauthorizedAccessException("User lacks permission to update item metadata");
        }
    }


    /// <summary>
    /// Validates that all mandatory fields are provided
    /// </summary>
    private void ValidateMandatoryFields(UpdateItemMetadataRequest request) {
        var itemConfig = settings.Item.MetadataDefinition;
        var errors = new List<string>();
        
        foreach (var fieldDef in itemConfig.Where(f => f.Required && !f.ReadOnly)) {
            if (!request.Metadata.ContainsKey(fieldDef.Id) || 
                request.Metadata[fieldDef.Id] == null) {
                errors.Add($"Required field '{fieldDef.Description}' is missing or null");
            }
        }
        
        if (errors.Count > 0) {
            throw new ValidationException($"Mandatory field validation failed: {string.Join(", ", errors)}");
        }
    }

    /// <summary>
    /// Validates metadata request against configuration
    /// </summary>
    private void ValidateMetadataRequest(UpdateItemMetadataRequest request) {
        var itemConfig = settings.Item.MetadataDefinition;
        var errors = new List<string>();
        
        foreach (var kvp in request.Metadata) {
            var fieldDef = itemConfig.FirstOrDefault(f => f.Id == kvp.Key);
            if (fieldDef == null) {
                errors.Add($"Unknown field '{kvp.Key}' not configured in metadata definitions");
                continue;
            }
            
            if (kvp.Value != null && !ValidateFieldType(kvp.Value, fieldDef.Type)) {
                errors.Add($"Field '{kvp.Key}' value '{kvp.Value}' is not compatible with type {fieldDef.Type}");
            }
        }
        
        if (errors.Count > 0) {
            throw new ValidationException($"Field validation failed: {string.Join(", ", errors)}");
        }
    }

    /// <summary>
    /// Validates that a value matches the expected field type
    /// </summary>
    private static bool ValidateFieldType(object value, MetadataFieldType expectedType) {
        return expectedType switch {
            MetadataFieldType.String => value is string or JsonElement { ValueKind: JsonValueKind.String },
            MetadataFieldType.Decimal => value is decimal or double or float or int or long or 
                                        JsonElement { ValueKind: JsonValueKind.Number },
            MetadataFieldType.Date => value is DateTime or DateTimeOffset or 
                                     JsonElement { ValueKind: JsonValueKind.String } ||
                                     (value is string str && DateTime.TryParse(str, out _)),
            MetadataFieldType.Integer => value is int or long or short or byte or 
                                        JsonElement { ValueKind: JsonValueKind.Number },
            _ => false
        };
    }

    /// <summary>
    /// Maps the API request to the external adapter request format
    /// Uses dynamic property mapping instead of hard-coded fields
    /// </summary>
    private static ItemMetadataRequest MapToAdapterRequest(UpdateItemMetadataRequest request) {
        // The external adapter will handle the actual field mapping
        // For now, return a request with the raw metadata dictionary
        // This allows the adapter to be flexible with field names
        return new ItemMetadataRequest {
            Metadata = request.Metadata
        };
    }

}