# Phase 3: Service Layer Implementation

## Overview
Implement the service layer for item metadata operations, including validation of read-only fields, mandatory field requirements, and integration with the external adapter for SAP Business One operations.

## Requirements
- Add UpdateItemMetadataAsync to IItemService interface
- Implement metadata validation with read-only field restrictions
- Handle mandatory vs optional field validation
- Integrate with external adapter for data persistence
- Warehouse access control for item metadata operations

## Service Interface Extension

### IItemService Interface
```csharp
public interface IItemService {
    // ... existing methods ...
    
    /// <summary>
    /// Updates metadata for a specific item via external adapter
    /// </summary>
    /// <param name="itemCode">The item code to update</param>
    /// <param name="request">The metadata update request</param>
    /// <param name="sessionInfo">Current user session information</param>
    /// <returns>Updated item metadata response</returns>
    Task<ItemMetadataResponse> UpdateItemMetadataAsync(
        string itemCode, 
        UpdateItemMetadataRequest request, 
        SessionInfo sessionInfo);
    
    /// <summary>
    /// Retrieves metadata for a specific item via external adapter
    /// </summary>
    /// <param name="itemCode">The item code to retrieve metadata for</param>
    /// <returns>Item metadata response or null if not found</returns>  
    Task<ItemMetadataResponse?> GetItemMetadataAsync(string itemCode);
}
```

## Service Implementation

### ItemService Class Updates
```csharp
public class ItemService : IItemService {
    private readonly IExternalSystemAdapter _adapter;
    private readonly ISettings _settings;
    private readonly ILogger<ItemService> _logger;
    
    public async Task<ItemMetadataResponse> UpdateItemMetadataAsync(
        string itemCode,
        UpdateItemMetadataRequest request,
        SessionInfo sessionInfo) {
        
        // 1. Validate item exists
        var existingItem = await _adapter.GetItemMetadataAsync(itemCode);
        if (existingItem == null) {
            throw new ItemNotFoundException($"Item with code '{itemCode}' not found");
        }
        
        // 2. Validate metadata against configuration
        ValidateMetadataRequest(request);
        
        // 3. Check read-only field restrictions
        ValidateReadOnlyFields(request);
        
        // 4. Validate mandatory fields
        ValidateMandatoryFields(request);
        
        // 5. Update via external adapter
        var adapterRequest = MapToAdapterRequest(request);
        var updatedItem = await _adapter.UpdateItemMetadataAsync(itemCode, adapterRequest);
        
        _logger.LogInformation("Updated item metadata for {ItemCode} by user {UserId}", 
            itemCode, sessionInfo.UserId);
            
        return updatedItem;
    }
    
    public async Task<ItemMetadataResponse?> GetItemMetadataAsync(string itemCode) {
        return await _adapter.GetItemMetadataAsync(itemCode);
    }
}
```

## Validation Logic

### Read-Only Field Validation
```csharp
private void ValidateReadOnlyFields(UpdateItemMetadataRequest request) {
    var errors = new List<string>();
    
    // Check if any read-only fields are included in the request
    if (request.Metadata.ContainsKey("ItemCode")) {
        errors.Add("ItemCode is read-only and cannot be modified");
    }
    
    if (request.Metadata.ContainsKey("ItemName")) {
        errors.Add("ItemName is read-only and cannot be modified");
    }
    
    if (errors.Any()) {
        throw new ValidationException($"Read-only field validation failed: {string.Join(", ", errors)}");
    }
}
```

### Mandatory Field Validation
```csharp
private void ValidateMandatoryFields(UpdateItemMetadataRequest request) {
    var itemConfig = _settings.Item.MetadataDefinition;
    var errors = new List<string>();
    
    foreach (var fieldDef in itemConfig.Where(f => f.Required && !f.ReadOnly)) {
        if (!request.Metadata.ContainsKey(fieldDef.Id) || 
            request.Metadata[fieldDef.Id] == null) {
            errors.Add($"Required field '{fieldDef.Description}' is missing or null");
        }
    }
    
    if (errors.Any()) {
        throw new ValidationException($"Mandatory field validation failed: {string.Join(", ", errors)}");
    }
}
```

### Field Type Validation
```csharp
private void ValidateMetadataRequest(UpdateItemMetadataRequest request) {
    var itemConfig = _settings.Item.MetadataDefinition;
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
        
        // Special validation for U_B1SStdTP field length
        if (kvp.Key == "U_B1SStdTP" && kvp.Value is string strValue && strValue.Length > 50) {
            errors.Add("Field 'U_B1SStdTP' cannot exceed 50 characters");
        }
    }
    
    if (errors.Any()) {
        throw new ValidationException($"Field validation failed: {string.Join(", ", errors)}");
    }
}
```

## DTO Mapping

### UpdateItemMetadataRequest DTO
```csharp
namespace Core.DTOs.Item;

/// <summary>
/// Request model for updating item metadata fields
/// Only writable fields should be included
/// </summary>
public class UpdateItemMetadataRequest {
    /// <summary>
    /// Metadata field values as key-value pairs
    /// Key must match configured metadata definition ID (writable fields only)
    /// Value must be compatible with configured field type
    /// </summary>
    public required Dictionary<string, object?> Metadata { get; set; } = new();
}
```

### Adapter Request Mapping
```csharp
private ItemMetadataRequest MapToAdapterRequest(UpdateItemMetadataRequest request) {
    return new ItemMetadataRequest {
        PurchaseUnitVolume = GetFieldValue<decimal?>(request, "PurchaseUnitVolume"),
        PurchaseWeightUnit = GetFieldValue<decimal?>(request, "PurchaseWeightUnit"),
        U_LW_UPDATE_USER = GetFieldValue<int?>(request, "U_LW_UPDATE_USER"),
        U_LW_UPDATE_TIMESTAMP = GetFieldValue<DateTime?>(request, "U_LW_UPDATE_TIMESTAMP"),
        U_B1SStdTP = GetFieldValue<string?>(request, "U_B1SStdTP")
    };
}

private T GetFieldValue<T>(UpdateItemMetadataRequest request, string fieldName) {
    if (request.Metadata.TryGetValue(fieldName, out var value)) {
        return (T)value;
    }
    return default(T);
}
```

## Error Handling

### Custom Exceptions
```csharp
namespace Core.Exceptions;

public class ItemNotFoundException : Exception {
    public ItemNotFoundException(string message) : base(message) { }
}

public class ItemMetadataValidationException : ValidationException {
    public ItemMetadataValidationException(string message) : base(message) { }
}
```

### Exception Handling in Service
```csharp
public async Task<ItemMetadataResponse> UpdateItemMetadataAsync(
    string itemCode,
    UpdateItemMetadataRequest request,
    SessionInfo sessionInfo) {
    
    try {
        // ... validation and update logic ...
    }
    catch (ItemNotFoundException) {
        throw; // Re-throw as-is for 404 handling
    }
    catch (ValidationException) {
        throw; // Re-throw as-is for 400 handling  
    }
    catch (ExternalAdapterException ex) {
        _logger.LogError(ex, "External adapter error updating item {ItemCode}", itemCode);
        throw new InvalidOperationException("Unable to update item metadata due to external system error");
    }
    catch (Exception ex) {
        _logger.LogError(ex, "Unexpected error updating item metadata for {ItemCode}", itemCode);
        throw new InvalidOperationException("An unexpected error occurred updating item metadata");
    }
}
```

## Security & Access Control

### Warehouse Access Control
```csharp
private async Task ValidateWarehouseAccess(string itemCode, SessionInfo sessionInfo) {
    // Items might be warehouse-specific depending on SAP configuration
    // This validation depends on how items are scoped in the external system
    
    var itemMetadata = await _adapter.GetItemMetadataAsync(itemCode);
    if (itemMetadata == null) {
        throw new ItemNotFoundException($"Item '{itemCode}' not found");
    }
    
    // Additional warehouse-specific validation if required
    // This depends on SAP Business One item master configuration
}
```

### Permission Validation
```csharp
private void ValidateUpdatePermissions(SessionInfo sessionInfo) {
    // Check if user has permissions to update item metadata
    if (!sessionInfo.SuperUser && 
        !sessionInfo.Roles.Contains(RoleType.ItemManagement) &&
        !sessionInfo.Roles.Contains(RoleType.ItemManagementSupervisor)) {
        throw new UnauthorizedAccessException("User lacks permission to update item metadata");
    }
}
```

## Files to Create/Modify

### Service Interface
- `Core/Services/IItemService.cs` - Add metadata methods

### Service Implementation  
- `Infrastructure/Services/ItemService.cs` - Implement metadata operations

### DTOs
- `Core/DTOs/Item/UpdateItemMetadataRequest.cs` - API request model

### Exceptions
- `Core/Exceptions/ItemNotFoundException.cs` - Item not found exception
- `Core/Exceptions/ItemMetadataValidationException.cs` - Validation exception

## Testing Strategy

### Unit Tests
- Validation logic tests (read-only, mandatory, type validation)
- DTO mapping tests
- Error handling tests
- Permission validation tests

### Integration Tests
- External adapter integration tests
- End-to-end metadata update workflows
- Error scenario testing

### Mock Testing
- Mock external adapter for isolated service testing
- Mock configuration for different validation scenarios

## Dependencies
- Phase 1: Configuration models completed
- Phase 2: External adapter interface implemented
- SAP Business One connectivity established

## Next Phase Integration
This phase enables:
- Phase 4: API endpoints with proper service layer integration
- Complete item metadata workflow from API to SAP Business One