# Phase 1: Item Metadata Configuration & Models

## Overview
Create the core configuration models for Item metadata, extending the existing package metadata pattern to support items loaded from external systems with read-only fields and mandatory/optional field configuration.

## Requirements
- ItemMetadataDefinition class with Required property for mandatory field validation
- ItemSettings class integration with metadata definition array
- Validation for the 7 specified item fields
- Support for read-only fields (ItemCode, ItemName)

## Key Item Fields Configuration

### Read Fields (loaded from external adapter)
- **ItemCode** (string) - Read-only, primary identifier
- **ItemName** (string) - Read-only, item description
- **PurchaseUnitVolume** (decimal) - Writable, optional/mandatory configurable
- **PurchaseWeightUnit** (decimal) - Writable, optional/mandatory configurable
- **U_LW_UPDATE_USER** (int) - Writable, optional/mandatory configurable
- **U_LW_UPDATE_TIMESTAMP** (date) - Writable, optional/mandatory configurable
- **U_B1SStdTP** (string, max 50 chars) - Writable, optional/mandatory configurable

### Write Fields (can be modified via API)
All fields except ItemCode and ItemName, with configurable mandatory/optional validation.

## Implementation Details

### 1. ItemMetadataDefinition Class
Extends the package metadata pattern with additional properties:
- `Required` property for mandatory field validation
- `ReadOnly` property to prevent modification of certain fields
- Validation for SAP Business One field constraints

### 2. ItemSettings Integration
- Add MetadataDefinition array to ItemSettings
- Implement validation for item-specific rules
- Handle read-only field restrictions
- Support for external adapter integration

### 3. Configuration Structure
```json
{
  "Item": {
    "MetadataDefinition": [
      {
        "Type": "String",
        "Id": "ItemCode",
        "Description": "Item Code",
        "ReadOnly": true,
        "Required": false
      },
      {
        "Type": "String", 
        "Id": "ItemName",
        "Description": "Item Name",
        "ReadOnly": true,
        "Required": false
      },
      {
        "Type": "Decimal",
        "Id": "PurchaseUnitVolume",
        "Description": "Purchase Unit Volume",
        "ReadOnly": false,
        "Required": true
      }
    ]
  }
}
```

## Files to Create/Modify

### Core Models
- `Core/Models/Settings/ItemMetadataDefinition.cs` - New metadata definition class
- `Core/Models/Settings/ItemSettings.cs` - Extend with metadata configuration
- `Core/Enums/MetadataFieldType.cs` - Share with package metadata (if not already shared)

### Configuration
- Update `appsettings.json` template with Item metadata section
- Add validation for item-specific field constraints

## Validation Rules

### Field ID Validation
- Must be valid C# identifiers
- Must match the 7 specified item fields exactly
- Case-sensitive matching
- Unique within configuration

### Field Configuration Validation
- ReadOnly fields cannot be marked as Required
- ItemCode and ItemName must be ReadOnly
- Other fields can be configured as Required or optional
- Type validation for each field (string, decimal, date, int)

### SAP Business One Constraints
- U_B1SStdTP field limited to 50 characters
- Proper type mapping for SAP fields
- Validation for SAP-specific field formats

## Testing Considerations
- Unit tests for ItemMetadataDefinition validation
- Configuration validation tests
- Read-only field restriction tests
- SAP field constraint validation tests

## Next Phase Dependencies
This phase provides the foundation for:
- Phase 2: External adapter interface definitions
- Phase 3: Service layer implementation
- Phase 4: API endpoint implementation