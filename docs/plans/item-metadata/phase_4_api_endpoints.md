# Phase 4: API Endpoints

## Overview
Implement REST API endpoints for item metadata operations, following the same patterns as package metadata but adapted for ItemCode-based identification and external data source integration.

## Requirements
- PUT /api/item/{itemCode}/metadata endpoint for updates
- GET /api/general/item-metadata-definitions endpoint for configuration
- Proper error handling with appropriate HTTP status codes
- Authentication and authorization integration
- Support for ItemCode as string identifier

## Controller Implementation

### ItemController Extension
```csharp
namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public partial class ItemController : ControllerBase {
    private readonly IItemService _itemService;
    private readonly ILogger<ItemController> _logger;
    private readonly ISettings _settings;
    
    /// <summary>
    /// Updates metadata for a specific item
    /// </summary>
    /// <param name="itemCode">The item code to update metadata for</param>
    /// <param name="request">The metadata update request containing field values</param>
    /// <returns>The updated item metadata</returns>
    /// <response code="200">Returns the updated item metadata</response>
    /// <response code="400">If metadata validation fails or request is invalid</response>
    /// <response code="403">If the user lacks required permissions</response>
    /// <response code="404">If the item is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPut("{itemCode}/metadata")]
    [RequireAnyRole(RoleType.ItemManagement, RoleType.ItemManagementSupervisor)]
    [ProducesResponseType(typeof(ItemMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemMetadataResponse>> UpdateItemMetadata(
        string itemCode,
        [FromBody] UpdateItemMetadataRequest request) {
        
        try {
            var sessionInfo = HttpContext.GetSession();
            
            var updatedItem = await _itemService.UpdateItemMetadataAsync(
                itemCode, request, sessionInfo);
                
            return Ok(updatedItem);
        }
        catch (ItemNotFoundException ex) {
            return NotFound(new { error = ex.Message });
        }
        catch (ValidationException ex) {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex) {
            return Forbid(new { error = ex.Message });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error updating item metadata for {ItemCode}", itemCode);
            return BadRequest(new { error = "An error occurred updating item metadata" });
        }
    }
    
    /// <summary>
    /// Retrieves metadata for a specific item
    /// </summary>
    /// <param name="itemCode">The item code to retrieve metadata for</param>
    /// <returns>The item metadata</returns>
    /// <response code="200">Returns the item metadata</response>
    /// <response code="404">If the item is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{itemCode}/metadata")]
    [ProducesResponseType(typeof(ItemMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemMetadataResponse>> GetItemMetadata(string itemCode) {
        try {
            var itemMetadata = await _itemService.GetItemMetadataAsync(itemCode);
            
            if (itemMetadata == null) {
                return NotFound(new { error = $"Item '{itemCode}' not found" });
            }
            
            return Ok(itemMetadata);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error retrieving item metadata for {ItemCode}", itemCode);
            return BadRequest(new { error = "An error occurred retrieving item metadata" });
        }
    }
}
```

### GeneralController Extension
```csharp
namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public partial class GeneralController : ControllerBase {
    private readonly ISettings _settings;
    
    /// <summary>
    /// Gets the configured item metadata field definitions
    /// </summary>
    /// <returns>List of item metadata field definitions</returns>
    /// <response code="200">Returns the item metadata definitions</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("item-metadata-definitions")]
    [ProducesResponseType(typeof(IEnumerable<ItemMetadataDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IEnumerable<ItemMetadataDefinitionDto>> GetItemMetadataDefinitions() {
        var definitions = _settings.Item.MetadataDefinition
            .Select(d => new ItemMetadataDefinitionDto {
                Id = d.Id,
                Description = d.Description,
                Type = (int)d.Type,
                Required = d.Required,
                ReadOnly = d.ReadOnly
            });
            
        return Ok(definitions);
    }
}
```

## Response DTOs

### ItemMetadataDefinitionDto
```csharp
namespace Core.DTOs.Item;

/// <summary>
/// DTO for item metadata field definition
/// </summary>
public class ItemMetadataDefinitionDto {
    /// <summary>
    /// Unique field identifier
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// Human-readable field description
    /// </summary>
    public required string Description { get; set; }
    
    /// <summary>
    /// Field data type (0=String, 1=Decimal, 2=Date, 3=Integer)
    /// </summary>
    public required int Type { get; set; }
    
    /// <summary>
    /// Whether this field is required for updates
    /// </summary>
    public required bool Required { get; set; }
    
    /// <summary>
    /// Whether this field is read-only
    /// </summary>
    public required bool ReadOnly { get; set; }
}
```

## Request/Response Examples

### Update Item Metadata Request
```http
PUT /api/item/ITEM001/metadata
Content-Type: application/json
Authorization: Bearer <jwt-token>

{
  "metadata": {
    "PurchaseUnitVolume": 10.5,
    "PurchaseWeightUnit": 2.3,
    "U_LW_UPDATE_USER": 123,
    "U_LW_UPDATE_TIMESTAMP": "2025-01-15T10:30:00Z",
    "U_B1SStdTP": "Standard Type A"
  }
}
```

### Update Item Metadata Response
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "itemCode": "ITEM001",
  "itemName": "Sample Item Name",
  "purchaseUnitVolume": 10.5,
  "purchaseWeightUnit": 2.3,
  "u_LW_UPDATE_USER": 123,
  "u_LW_UPDATE_TIMESTAMP": "2025-01-15T10:30:00Z",
  "u_B1SStdTP": "Standard Type A"
}
```

### Get Item Metadata Definitions Response
```http
GET /api/general/item-metadata-definitions
Authorization: Bearer <jwt-token>

HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "id": "ItemCode",
    "description": "Item Code",
    "type": 0,
    "required": false,
    "readOnly": true
  },
  {
    "id": "ItemName", 
    "description": "Item Name",
    "type": 0,
    "required": false,
    "readOnly": true
  },
  {
    "id": "PurchaseUnitVolume",
    "description": "Purchase Unit Volume",
    "type": 1,
    "required": true,
    "readOnly": false
  },
  {
    "id": "PurchaseWeightUnit",
    "description": "Purchase Unit Weight",
    "type": 1,
    "required": false,
    "readOnly": false
  },
  {
    "id": "U_LW_UPDATE_USER",
    "description": "Update User ID",
    "type": 3,
    "required": false,
    "readOnly": false
  },
  {
    "id": "U_LW_UPDATE_TIMESTAMP",
    "description": "Update Timestamp",
    "type": 2,
    "required": false,
    "readOnly": false
  },
  {
    "id": "U_B1SStdTP",
    "description": "SAP Standard Type",
    "type": 0,
    "required": false,
    "readOnly": false
  }
]
```

## Error Handling

### Standard Error Responses

#### 400 Bad Request - Validation Error
```json
{
  "error": "Field validation failed: Field 'U_B1SStdTP' cannot exceed 50 characters"
}
```

#### 403 Forbidden - Permission Error
```json
{
  "error": "User lacks permission to update item metadata"
}
```

#### 404 Not Found - Item Not Found
```json
{
  "error": "Item 'INVALID001' not found"
}
```

#### 400 Bad Request - Read-Only Field Error
```json
{
  "error": "Read-only field validation failed: ItemCode is read-only and cannot be modified"
}
```

## Authorization & Security

### Role Requirements
- **ItemManagement**: Basic item metadata update permissions
- **ItemManagementSupervisor**: Enhanced permissions for sensitive operations
- **SuperUser**: Full access to all item metadata operations

### Role Attribute Usage
```csharp
[RequireAnyRole(RoleType.ItemManagement, RoleType.ItemManagementSupervisor)]
```

### Input Validation
- ItemCode parameter validation (non-empty, valid format)
- Metadata dictionary validation against configuration
- Request size limits to prevent DoS attacks
- SQL injection prevention through parameterized queries

## API Documentation

### OpenAPI/Swagger Integration
```csharp
/// <summary>
/// Updates metadata for a specific item
/// </summary>
/// <param name="itemCode">The item code to update metadata for</param>
/// <param name="request">The metadata update request containing field values</param>
/// <returns>The updated item metadata</returns>
/// <remarks>
/// Sample request:
/// 
///     PUT /api/item/ITEM001/metadata
///     {
///         "metadata": {
///             "PurchaseUnitVolume": 10.5,
///             "U_B1SStdTP": "Type A"
///         }
///     }
/// 
/// Only writable fields can be updated. Read-only fields (ItemCode, ItemName) will be rejected.
/// Required fields must be provided if configured in the metadata definitions.
/// </remarks>
```

## Performance Considerations

### Caching Strategy
- Cache item metadata definitions in memory
- Consider item metadata caching for frequently accessed items
- Implement cache invalidation for configuration changes

### Rate Limiting
- Implement rate limiting for metadata update operations
- Prevent excessive SAP Business One API calls
- Monitor and log API usage patterns

### Response Optimization
- Minimize response payload size
- Support conditional requests (ETag/If-Modified-Since)
- Implement compression for large responses

## Files to Create/Modify

### Controllers
- `Service/Controllers/ItemController.cs` - Add metadata endpoints
- `Service/Controllers/GeneralController.cs` - Add configuration endpoint

### DTOs
- `Core/DTOs/Item/ItemMetadataDefinitionDto.cs` - Configuration response model

### Route Configuration
- Update routing configuration for item metadata endpoints
- Ensure proper URL encoding for ItemCode parameters

## Testing Strategy

### Integration Tests
```csharp
[Test]
public async Task UpdateItemMetadata_ValidRequest_ReturnsOk() {
    // Arrange
    var itemCode = "TEST001";
    var request = new UpdateItemMetadataRequest {
        Metadata = new Dictionary<string, object?> {
            ["PurchaseUnitVolume"] = 10.5m,
            ["U_B1SStdTP"] = "Type A"
        }
    };
    
    // Act
    var response = await _client.PutAsJsonAsync($"/api/item/{itemCode}/metadata", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<ItemMetadataResponse>();
    result.PurchaseUnitVolume.Should().Be(10.5m);
}

[Test]
public async Task UpdateItemMetadata_ReadOnlyField_ReturnsBadRequest() {
    // Arrange
    var itemCode = "TEST001";
    var request = new UpdateItemMetadataRequest {
        Metadata = new Dictionary<string, object?> {
            ["ItemCode"] = "MODIFIED001" // Read-only field
        }
    };
    
    // Act
    var response = await _client.PutAsJsonAsync($"/api/item/{itemCode}/metadata", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

### Unit Tests
- Controller action tests with mocked services
- Error handling and status code validation
- Authorization attribute testing

## Dependencies
- Phase 1: Configuration models
- Phase 2: External adapter interface
- Phase 3: Service layer implementation
- Authentication/authorization middleware

## Next Phase Integration
This phase completes the core API functionality, enabling:
- Phase 5: Comprehensive testing and documentation
- Frontend integration for item metadata management
- Production deployment readiness