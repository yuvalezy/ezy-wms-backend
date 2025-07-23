# Item Metadata Management Feature Summary

## Overview
The Item Metadata Management feature provides flexible, configurable metadata handling for items loaded from external systems (SAP Business One). This feature allows customers to define custom fields for items based on their specific business needs without requiring code changes.

## Key Features

### ✅ **Flexible Configuration System**
- **ItemMetadataDefinition**: Configurable field definitions with type, description, and access control
- **Dynamic Field Types**: Support for String, Decimal, Integer, and Date field types  
- **Read-Only Protection**: Configure which fields are read-only vs writable
- **Mandatory Field Support**: Configure which fields are required for updates
- **External System Validation**: SAP Business One validates field existence and permissions

### ✅ **API Endpoints**
- **PUT /api/items/{itemCode}/metadata** - Update item metadata
- **GET /api/items/{itemCode}/metadata** - Retrieve item metadata
- **GET /api/general/item-metadata-definitions** - Get field configuration

### ✅ **SAP Business One Integration**
- **Service Layer REST API**: Uses SAP B1 Service Layer for data operations
- **Filtered Queries**: Only fetches configured fields using `$select` parameter
- **PATCH Updates**: Efficient updates using HTTP PATCH method
- **Direct Field Mapping**: Configuration field names map directly to SAP field names
- **Type Safety**: Proper type conversion between WMS and SAP formats

## Architecture

### **Service Layer**
- **IItemService**: Service interface for item metadata operations
- **ItemService**: Implementation with validation and external system integration
- **Standard Exception Handling**: Uses existing ExceptionHandlingMiddleware

### **External Integration**
- **IExternalSystemAdapter**: Extended with item metadata methods
- **SboServiceLayerAdapter**: Implements SAP Business One integration
- **ItemMetadataProcessor**: Handles SAP Service Layer communication

### **Configuration Model**
- **ItemSettings**: Container for metadata field definitions
- **ItemMetadataDefinition**: Individual field configuration with properties:
  - `Id`: Field identifier (exact SAP field name)
  - `Description`: Human-readable label
  - `Type`: Field data type (String/Decimal/Integer/Date)
  - `Required`: Whether field is mandatory for updates
  - `ReadOnly`: Whether field can be modified

## Configuration Example

```json
{
  "Item": {
    "MetadataDefinition": [
      {
        "Id": "ItemCode",
        "Description": "Item Code",
        "Type": "String",
        "ReadOnly": true,
        "Required": false
      },
      {
        "Id": "ItemName",
        "Description": "Item Name",
        "Type": "String", 
        "ReadOnly": true,
        "Required": false
      },
      {
        "Id": "PurchaseUnitVolume",
        "Description": "Purchase Unit Volume (m³)",
        "Type": "Decimal",
        "ReadOnly": false,
        "Required": true
      },
      {
        "Id": "U_CUSTOM_FIELD",
        "Description": "Custom Business Field",
        "Type": "String",
        "ReadOnly": false,
        "Required": false
      }
    ]
  }
}
```

## API Usage Examples

### **Get Item Metadata**
```http
GET /api/items/ITEM001/metadata
Authorization: Bearer <jwt-token>

Response:
{
  "metadata": {
    "ItemCode": "ITEM001",
    "ItemName": "Sample Item",
    "PurchaseUnitVolume": 10.5,
    "U_CUSTOM_FIELD": "Custom Value"
  }
}
```

### **Update Item Metadata**
```http
PUT /api/items/ITEM001/metadata
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "metadata": {
    "PurchaseUnitVolume": 12.0,
    "U_CUSTOM_FIELD": "Updated Value"
  }
}
```

### **Get Field Configuration**
```http
GET /api/general/item-metadata-definitions
Authorization: Bearer <jwt-token>

Response:
[
  {
    "id": "PurchaseUnitVolume",
    "description": "Purchase Unit Volume (m³)",
    "type": 1,
    "required": true,
    "readOnly": false
  }
]
```

## SAP Business One Integration

### **Service Layer Endpoints**
- **GET**: `{{sapUrl}}/b1s/v2/Items('{{itemCode}})?$select=field1,field2`
- **PATCH**: `{{sapUrl}}/b1s/v2/Items('{{itemCode}}')`

### **Features**
- **Authentication**: Uses B1SESSION cookie for SAP authentication
- **Error Handling**: Comprehensive error handling with SAP-specific error parsing
- **Type Conversion**: Automatic conversion between JSON and typed values
- **Field Filtering**: Only processes configured fields for performance

## Security & Permissions

### **Role-Based Access Control**
- **ItemManagement**: Basic item metadata update permissions
- **ItemManagementSupervisor**: Enhanced permissions for sensitive operations
- **Authorization**: All endpoints require authentication

### **Validation**
- **Read-Only Protection**: Prevents modification of read-only fields
- **Configuration Validation**: Ensures field types and requirements are met
- **External Validation**: SAP Business One validates field existence and permissions

## Business Benefits

### **Industry Flexibility**
- **Manufacturing**: Track production specifications, quality grades
- **Food & Beverage**: Manage nutritional information, allergen data
- **Pharmaceutical**: Handle regulatory data, batch information
- **Retail**: Store pricing tiers, supplier information

### **Operational Efficiency**
- **No Code Changes**: Add new fields through configuration only
- **Performance Optimized**: Only fetches/updates required fields
- **Type Safety**: Prevents data type errors
- **Audit Trail**: Comprehensive logging for all metadata operations

## Technical Specifications

### **Performance**
- **Filtered Queries**: Uses SAP $select to minimize data transfer
- **Efficient Updates**: PATCH method updates only changed fields
- **Caching**: Metadata processor caches data for comparison

### **Scalability**
- **Configuration-Driven**: Unlimited field definitions
- **External Validation**: Leverages SAP Business One for business rules
- **Clean Architecture**: Separation of concerns for maintainability

### **Error Handling**
- **Standard Exceptions**: Uses existing middleware for consistent error responses
- **SAP Error Mapping**: Converts SAP errors to user-friendly messages
- **Logging**: Comprehensive logging at Debug, Info, Warning, and Error levels

## Deployment Considerations

### **Configuration**
1. Define item metadata fields in `appsettings.json`
2. Ensure SAP user has permissions for configured fields
3. Test field access with sample items

### **Monitoring**
- Monitor API response times for metadata operations
- Track SAP Business One connection health
- Log validation errors for configuration issues

## Future Enhancements

### **Potential Extensions**
- **Bulk Operations**: Update metadata for multiple items
- **Field Dependencies**: Conditional field requirements
- **Advanced Validation**: Custom validation rules
- **Audit History**: Track metadata change history
- **Import/Export**: Bulk metadata management tools

The Item Metadata Management feature provides a robust, flexible foundation for managing item-specific data while maintaining clean architecture and excellent performance.