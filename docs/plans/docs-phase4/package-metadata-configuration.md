# Package Metadata Configuration Guide

## Overview

The Package Metadata feature allows each customer to define custom properties for packages based on their specific industry needs. This guide provides comprehensive configuration examples and best practices.

## Configuration Structure

Metadata fields are configured in the `appsettings.json` file under the `Package.MetadataDefinition` section:

```json
{
  "Package": {
    "MetadataDefinition": [
      {
        "Type": "Decimal",
        "Id": "Volume", 
        "Description": "Volume (m³)"
      },
      {
        "Type": "Decimal",
        "Id": "Weight",
        "Description": "Weight (kg)"
      },
      {
        "Type": "String",
        "Id": "Note",
        "Description": "Special Notes"
      },
      {
        "Type": "Date",
        "Id": "ExpiryDate",
        "Description": "Expiration Date"
      }
    ]
  }
}
```

## Field Types

| Type | Description | Examples | Frontend Input |
|------|-------------|----------|----------------|
| `String` | Text values | Notes, Comments, Batch Numbers | Text input |
| `Decimal` | Numeric values | Volume, Weight, Temperature | Number input |
| `Date` | Date values | Expiry Date, Manufacturing Date | Date picker |

## Industry-Specific Examples

### Food & Beverage Industry

```json
{
  "Package": {
    "MetadataDefinition": [
      {
        "Type": "Date",
        "Id": "ExpiryDate",
        "Description": "Expiration Date"
      },
      {
        "Type": "Date", 
        "Id": "ManufacturingDate",
        "Description": "Manufacturing Date"
      },
      {
        "Type": "String",
        "Id": "BatchNumber",
        "Description": "Batch Number"
      },
      {
        "Type": "Decimal",
        "Id": "Temperature",
        "Description": "Storage Temperature (°C)"
      },
      {
        "Type": "String",
        "Id": "AllergenInfo",
        "Description": "Allergen Information"
      },
      {
        "Type": "String",
        "Id": "QualityGrade",
        "Description": "Quality Grade"
      }
    ]
  }
}
```

### Pharmaceutical Industry

```json
{
  "Package": {
    "MetadataDefinition": [
      {
        "Type": "String",
        "Id": "LotNumber",
        "Description": "Lot Number"
      },
      {
        "Type": "Date",
        "Id": "ExpiryDate", 
        "Description": "Expiration Date"
      },
      {
        "Type": "String",
        "Id": "NDCNumber",
        "Description": "NDC Number"
      },
      {
        "Type": "Decimal",
        "Id": "Potency",
        "Description": "Potency (%)"
      },
      {
        "Type": "String",
        "Id": "StorageConditions",
        "Description": "Storage Conditions"
      },
      {
        "Type": "String",
        "Id": "Manufacturer",
        "Description": "Manufacturer"
      },
      {
        "Type": "Date",
        "Id": "TestDate",
        "Description": "Quality Test Date"
      }
    ]
  }
}
```

### Electronics Industry

```json
{
  "Package": {
    "MetadataDefinition": [
      {
        "Type": "String",
        "Id": "SerialNumber",
        "Description": "Serial Number"
      },
      {
        "Type": "String",
        "Id": "ModelNumber", 
        "Description": "Model Number"
      },
      {
        "Type": "Date",
        "Id": "ManufacturingDate",
        "Description": "Manufacturing Date"
      },
      {
        "Type": "String",
        "Id": "FirmwareVersion",
        "Description": "Firmware Version"
      },
      {
        "Type": "String",
        "Id": "Warranty",
        "Description": "Warranty Period"
      },
      {
        "Type": "String",
        "Id": "Compliance",
        "Description": "Compliance Certifications"
      }
    ]
  }
}
```

### Automotive Parts

```json
{
  "Package": {
    "MetadataDefinition": [
      {
        "Type": "String",
        "Id": "PartNumber",
        "Description": "OEM Part Number"
      },
      {
        "Type": "String",
        "Id": "VehicleCompatibility",
        "Description": "Vehicle Compatibility"
      },
      {
        "Type": "Decimal",
        "Id": "Weight",
        "Description": "Weight (kg)"
      },
      {
        "Type": "String",
        "Id": "Material",
        "Description": "Material Composition"
      },
      {
        "Type": "Date",
        "Id": "TestDate",
        "Description": "Quality Test Date"
      },
      {
        "Type": "String",
        "Id": "SupplierCode",
        "Description": "Supplier Code"
      }
    ]
  }
}
```

### Textile & Apparel

```json
{
  "Package": {
    "MetadataDefinition": [
      {
        "Type": "String",
        "Id": "Size",
        "Description": "Size"
      },
      {
        "Type": "String",
        "Id": "Color",
        "Description": "Color"
      },
      {
        "Type": "String",
        "Id": "Season",
        "Description": "Season Collection"
      },
      {
        "Type": "String",
        "Id": "FabricComposition",
        "Description": "Fabric Composition"
      },
      {
        "Type": "String",
        "Id": "CareInstructions",
        "Description": "Care Instructions"
      },
      {
        "Type": "String",
        "Id": "Origin",
        "Description": "Country of Origin"
      }
    ]
  }
}
```

## Configuration Rules

### Field ID Requirements
- Must be valid C# identifiers (alphanumeric, underscore, no spaces)
- Must start with letter or underscore  
- Maximum length: 50 characters
- Case-sensitive
- Must be unique within the configuration

### Valid IDs
```
✅ Volume
✅ Weight_KG  
✅ ExpiryDate
✅ Temperature_C
✅ _BatchNumber
```

### Invalid IDs
```
❌ Volume Weight (contains space)
❌ 2Volume (starts with number)  
❌ Expiry-Date (contains hyphen)
❌ "" (empty string)
```

### Description Requirements
- Must be non-empty after trimming whitespace
- Maximum length: 255 characters
- Used as display label in frontend
- Can contain any Unicode characters

## Data Storage

- All metadata is stored in the existing `CustomAttributes` JSON column
- No database schema changes required
- Backward compatible with existing packages
- Maximum JSON size: 8000 characters per package

## API Endpoints

### Get Metadata Definitions
```http
GET /api/general/package-metadata-definitions
```

**Response:**
```json
[
  {
    "id": "Volume",
    "description": "Volume (m³)",
    "type": 1
  },
  {
    "id": "Note", 
    "description": "Special Notes",
    "type": 0
  }
]
```

### Update Package Metadata
```http
PUT /api/package/{id}/metadata
Content-Type: application/json

{
  "metadata": {
    "Volume": 10.5,
    "Note": "Fragile items",
    "ExpiryDate": "2025-12-31T00:00:00Z"
  }
}
```

**Response:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "barcode": "PKG001",
  "customAttributes": {
    "Volume": 10.5,
    "Note": "Fragile items", 
    "ExpiryDate": "2025-12-31T00:00:00Z"
  },
  "metadataDefinitions": [...]
}
```

## Frontend Integration

### Display Component
```tsx
import { PackageMetadataDisplay } from '@/features/packages/components';

<PackageMetadataDisplay 
  packageData={packageData}
  showTitle={true}
  className="my-4"
/>
```

### Form Component
```tsx
import { PackageMetadataForm } from '@/features/packages/components';

<PackageMetadataForm
  packageData={packageData}
  onSave={(updatedPackage) => console.log('Saved:', updatedPackage)}
  onCancel={() => console.log('Cancelled')}
/>
```

## Validation Rules

1. **Field Existence**: Only configured field IDs are accepted
2. **Type Validation**: Values must match the configured field type
3. **Optional Fields**: All fields are optional by design
4. **Null Handling**: Setting a field to `null` removes it from the package

## Best Practices

### Configuration Management
1. Plan field requirements before deployment
2. Use descriptive field IDs that won't conflict with future needs
3. Keep descriptions user-friendly and translatable
4. Consider maximum JSON size limits when adding many fields

### Performance Considerations
1. Limit the number of metadata fields (recommended: < 20 per package type)
2. Use appropriate field types to minimize storage size
3. Consider indexing frequently queried metadata fields

### Security
1. Validate all user input before storing
2. Sanitize strings to prevent XSS attacks
3. Implement proper access controls for metadata updates

## Troubleshooting

### Common Configuration Errors
1. **Duplicate IDs**: Each field ID must be unique
2. **Invalid identifiers**: Field IDs must follow C# naming rules
3. **Empty descriptions**: Descriptions cannot be empty or whitespace-only
4. **Unknown field types**: Only String(0), Decimal(1), Date(2) are supported

### Runtime Issues
1. **Validation failures**: Check field types match expected values
2. **Missing definitions**: Ensure configuration is loaded correctly
3. **JSON serialization errors**: Verify data types are compatible

## Migration Guide

### From No Metadata to Metadata
1. Add configuration to `appsettings.json`
2. Deploy backend changes
3. Deploy frontend changes  
4. Test with sample packages
5. Train users on new functionality

### Adding New Fields
1. Update configuration
2. Restart application (or implement hot reload)
3. New fields will be available immediately
4. Existing packages retain their current metadata

### Removing Fields
1. Remove from configuration
2. Existing data in removed fields will be preserved but not displayed
3. Consider data migration if permanent removal is needed