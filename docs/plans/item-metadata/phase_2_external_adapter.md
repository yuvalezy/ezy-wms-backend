# Phase 2: External Adapter Interface

## Overview
Extend the IExternalSystemAdapter interface to support item metadata operations. Items are external objects loaded from SAP Business One, requiring dedicated adapter methods for retrieving and updating metadata.

## Requirements
- Extend IExternalSystemAdapter with item metadata methods
- Create DTOs for item metadata requests and responses
- Support for ItemCode-based operations (string identifier)
- Handle SAP Business One field mappings

## Interface Extensions

### New Methods for IExternalSystemAdapter
```csharp
/// <summary>
/// Retrieves item metadata from the external system by item code
/// </summary>
/// <param name="itemCode">The item code to retrieve metadata for</param>
/// <returns>Item metadata response with all configured fields</returns>
Task<ItemMetadataResponse?> GetItemMetadataAsync(string itemCode);

/// <summary>
/// Updates item metadata in the external system
/// </summary>
/// <param name="itemCode">The item code to update</param>
/// <param name="request">The metadata update request</param>
/// <returns>Updated item metadata response</returns>
Task<ItemMetadataResponse> UpdateItemMetadataAsync(string itemCode, ItemMetadataRequest request);
```

## DTO Structures

### ItemMetadataRequest
Request model for updating item metadata (write fields only):
```csharp
public class ItemMetadataRequest {
    public decimal? PurchaseUnitVolume { get; set; }
    public decimal? PurchaseWeightUnit { get; set; }
    public int? U_LW_UPDATE_USER { get; set; }
    public DateTime? U_LW_UPDATE_TIMESTAMP { get; set; }
    public string? U_B1SStdTP { get; set; } // Max 50 chars
}
```

### ItemMetadataResponse
Response model containing all item metadata fields:
```csharp
public class ItemMetadataResponse {
    // Read-only fields (loaded from SAP)
    public required string ItemCode { get; set; }
    public required string ItemName { get; set; }
    
    // Writable fields
    public decimal? PurchaseUnitVolume { get; set; }
    public decimal? PurchaseWeightUnit { get; set; }
    public int? U_LW_UPDATE_USER { get; set; }
    public DateTime? U_LW_UPDATE_TIMESTAMP { get; set; }
    public string? U_B1SStdTP { get; set; }
}
```

## SAP Business One Integration

### Field Mappings
Map the specified fields to SAP Business One item master fields:
- **ItemCode** → `ItemCode` (OITM.ItemCode)
- **ItemName** → `ItemName` (OITM.ItemName)
- **PurchaseUnitVolume** → Purchase unit volume field
- **PurchaseWeightUnit** → Purchase unit weight field
- **U_LW_UPDATE_USER** → User-defined field for update user
- **U_LW_UPDATE_TIMESTAMP** → User-defined field for update timestamp
- **U_B1SStdTP** → User-defined field (max 50 characters)

### SAP Adapter Implementation
```csharp
public class SapAdapter : IExternalSystemAdapter {
    public async Task<ItemMetadataResponse?> GetItemMetadataAsync(string itemCode) {
        // Connect to SAP Business One
        // Query item master data
        // Map SAP fields to ItemMetadataResponse
        // Handle null/missing items
    }
    
    public async Task<ItemMetadataResponse> UpdateItemMetadataAsync(
        string itemCode, 
        ItemMetadataRequest request) {
        // Validate item exists in SAP
        // Update only writable fields
        // Handle SAP transaction management
        // Return updated metadata
    }
}
```

## Error Handling

### Item Not Found
- Return null from GetItemMetadataAsync if item doesn't exist
- Throw ItemNotFoundException from UpdateItemMetadataAsync

### SAP Connection Issues
- Handle SAP Business One connection failures
- Retry logic for transient errors
- Proper exception wrapping for adapter errors

### Field Validation
- Validate U_B1SStdTP field length (max 50 chars)
- Handle SAP field type mismatches
- Validate required field constraints

## Files to Create/Modify

### Interface Extensions
- `Core/Interfaces/IExternalSystemAdapter.cs` - Add item metadata methods

### DTOs
- `Core/DTOs/Item/ItemMetadataRequest.cs` - Request model for updates
- `Core/DTOs/Item/ItemMetadataResponse.cs` - Response model with all fields

### SAP Implementation
- `Infrastructure/Adapters/SapAdapter.cs` - Implement metadata methods
- Add SAP field mapping configuration

### Exceptions
- `Core/Exceptions/ItemNotFoundException.cs` - Item not found exception

## Validation Rules

### ItemCode Validation
- Must be valid SAP item code format
- Case-sensitive matching
- Non-empty string validation

### Field Value Validation
- U_B1SStdTP maximum 50 characters
- Decimal precision validation for volume/weight
- Date format validation for timestamp fields

### SAP Integration Validation
- Verify item exists before update operations
- Validate SAP user permissions for field updates
- Handle SAP field locking/concurrency

## Testing Considerations

### Unit Tests
- Mock adapter interface for service layer testing
- DTO validation and serialization tests
- Field mapping validation tests

### Integration Tests
- SAP Business One connectivity tests
- End-to-end item metadata retrieval/update
- Error handling for missing items

### Performance Tests
- Bulk item metadata operations
- SAP connection pooling efficiency
- Response time validation

## Dependencies
- Phase 1: Configuration models must be completed
- SAP Business One SDK availability
- Database connection configuration

## Next Phase Integration
This phase enables:
- Phase 3: Service layer implementation with external data source
- Phase 4: API endpoints with ItemCode-based operations