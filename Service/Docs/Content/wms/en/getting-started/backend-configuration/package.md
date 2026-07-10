---
title: Package Configuration
description: Configure package labeling, barcode generation, and metadata for EZY WMS.
---

> **Current WMS note:** Live configuration is stored in the database and managed from the superuser Configuration screen. YAML files under `Service/init` are seed templates, not the runtime configuration source.

The `Package.yaml` configuration file defines how packages are labeled, barcoded, and enriched with custom metadata fields. This guide covers the three main subsections: Label, Barcode, and MetadataDefinition.

## Label

The `Label` section controls automatic printing behavior when packages are created.

### AutoPrint

- **Type:** Boolean
- **Default:** `false`
- **Description:** When set to `true`, package labels are automatically printed immediately upon package creation. Requires a configured printer and label template.

## Barcode

The `Barcode` section defines the format and generation rules for package barcodes.

### Prefix

- **Type:** String
- **Default:** `""` (empty)
- **Description:** A fixed prefix prepended to all generated barcodes. Commonly used to identify the barcode type (e.g., `PKG` for packages).

### Length

- **Type:** Integer
- **Default:** Required (no default value)
- **Description:** The total length of the generated barcode, including prefix and suffix. The numeric portion is zero-padded to fill the remaining space.

### Suffix

- **Type:** String
- **Default:** `""` (empty)
- **Description:** A fixed suffix appended to all generated barcodes. May be used for checksum digits or additional identification.

### StartNumber

- **Type:** Integer
- **Default:** `1`
- **Description:** The first number in the barcode sequence. Useful when migrating from an existing system and continuing a previous sequence.

### Generation Rule

Barcodes are generated using the pattern:

```
[Prefix][ZeroPaddedNumber][Suffix]
```

The `ZeroPaddedNumber` is zero-padded to ensure the total barcode length equals the configured `Length` value.

### Examples

Given `Length=14`, `Prefix="PKG"`, `Suffix=""`, and `StartNumber=1`:

- **First package:** `PKG00000000001` (11 zeros between prefix and number)
- **100th package:** `PKG00000000100`
- **10,000th package:** `PKG00010000000`

Another example with `Length=12`, `Prefix="A"`, `StartNumber=99`:

- **First package:** `A00000000099`
- **Second package:** `A00000000100`

## MetadataDefinition

The `MetadataDefinition` section is an array of custom metadata fields. Each field is used during package creation and data entry, and is validated against SAP B1 integration standards.

### Field Properties

Each metadata field definition supports the following properties:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Id` | String | Yes | Unique identifier for the field within package metadata. Cannot be duplicated. |
| `Description` | String | Yes | Human-readable label displayed in UI forms and reports. |
| `Type` | Enum | Yes | Data type for validation and UI rendering: `String`, `Decimal`, `Date`, or `Integer`. |
| `Step` | Decimal | No | Step increment for `Decimal` type fields (e.g., `0.01` for currency). |
| `Required` | Boolean | No | Whether the field must be populated during package creation. Default: `false`. |
| `ReadOnly` | Boolean | No | Whether the field is display-only and cannot be edited. Default: `false`. |
| `Query` | String | No | SQL query for retrieving dropdown or lookup data. |
| `GroupBy` | String | No | SQL expression for grouping query results. |
| `Calculated` | Object | No | Configuration for computed or derived field values. |

### Example Fields

The configuration includes three example fields:

**Volume** — A decimal field for package volume:
```yaml
- Type: Decimal
  Id: Volume
  Description: "Volume (m³)"
```

**Note** — A text field for special notes:
```yaml
- Type: String
  Id: Note
  Description: "Special Notes"
```

**ExpiryDate** — A date field for expiration tracking:
```yaml
- Type: Date
  Id: ExpiryDate
  Description: "Expiry Date"
```

## Notes

- **AutoPrint** requires a configured printer and label template in your WMS setup. Verify printer connectivity before enabling.
- **Barcode configuration** should be finalized before the first package is created. Changing prefix, length, or suffix after packages exist may cause conflicts or duplicate barcode issues.
- **Metadata fields** are validated against your SAP B1 system during integration. Ensure custom field definitions align with your SAP schema.
- Barcode **uniqueness is enforced** by the system; the `StartNumber` parameter should only be adjusted during system migration or setup.

## Complete Configuration Example

Below is a fully annotated example of a `Package.yaml` file:

```yaml
# Package Configuration
# Defines package labeling, barcode generation, and metadata settings
# Maps to Settings.Package in the application

Package:
  # Label configuration for package printing
  Label:
    # Automatically print labels when packages are created.
    # Requires printer configuration and label templates.
    AutoPrint: true
  
  # Barcode generation configuration for packages
  Barcode:
    # Prefix for all package barcodes (identifies barcode type)
    Prefix: "PKG"
    
    # Total barcode length including prefix and suffix
    # Numeric portion is zero-padded to reach this length
    # Example: with Prefix="PKG" and Length=14, first barcode is "PKG00000000001"
    Length: 14
    
    # Suffix appended to all barcodes (optional, can be empty)
    Suffix: ""
    
    # Starting number for the sequence (useful for migrations)
    # Each new package increments from this value
    StartNumber: 1
  
  # Metadata field definitions for packages
  # Array of custom fields validated during data entry
  MetadataDefinition:
    # Example: Volume measurement
    - Type: Decimal
      Id: Volume
      Description: "Volume (m³)"
      Step: 0.01          # Optional: step increment for UI spinners
      Required: false     # Optional: field is optional during entry
      ReadOnly: false     # Optional: field is editable
    
    # Example: Special notes or comments
    - Type: String
      Id: Note
      Description: "Special Notes"
      Required: false
      ReadOnly: false
    
    # Example: Product expiry tracking
    - Type: Date
      Id: ExpiryDate
      Description: "Expiry Date"
      Required: true      # Optional: field is mandatory
      ReadOnly: false

# File location: ezy-wms-backend/Service/config/Package.yaml
```

---

**Configuration File Location:** `ezy-wms-backend/Service/config/Package.yaml`
