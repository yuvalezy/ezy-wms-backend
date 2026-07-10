---
title: Item Metadata (Item.yaml)
description: Reference for configuring item metadata field definitions in EZY WMS.
---

> **Current WMS note:** Live configuration is stored in the database and managed from the superuser Configuration screen. YAML files under `Service/init` are seed templates, not the runtime configuration source.

This document describes how to configure item metadata field definitions in the EZY WMS configuration file.

## Overview

The `Item.yaml` seed/migration template is located at
`ezy-wms-backend/Service/init/Item.yaml`. The database-backed `Item` section is
the runtime source and defines metadata fields that can be edited, validated, and
calculated within EZY WMS.

The configuration uses a structured format under `Item.MetadataDefinition`, which is an array of field definitions. Each definition maps to a supported SAP item field and specifies validation rules, data types, and optional calculations.

## Field Definition Structure

Each metadata field definition contains the following properties:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Id` | String | Yes | Unique identifier for the field. Must match a supported SAP item field name. |
| `Description` | String | Yes | Human-readable label displayed in the UI for end users. |
| `Type` | Enum | Yes | Data type for validation and rendering. Allowed values: `String`, `Decimal`, `Date`, `Integer`. |
| `ReadOnly` | Boolean | No | Whether the field cannot be edited after creation. Default: `false`. Note: `ItemCode` and `ItemName` are always read-only. |
| `Required` | Boolean | No | Whether the field must have a value during updates. Default: `false`. Cannot be true if `ReadOnly` is true. |
| `Step` | Integer | No | Step increment for decimal fields in the UI. Only applies when `Type` is `Decimal`. |
| `MirrorTo` | String | No | Copies the field value to another field automatically. Example: `PurchaseUnitLength` → `SalesUnitLength`. |
| `Query` | String | No | Optional SQL fragment for retrieving additional data. |
| `GroupBy` | String | No | Optional SQL expression for grouping query results. |
| `Calculated` | Object | No | Configuration for computed fields with formulas. See [Calculated Fields](#calculated-fields) section. |

## Validation Rules

- **Field Name Validation**: SAP validates field names. Only supported item fields in SAP are accepted.
- **Read-Only Constraint**: `Required` cannot be true when `ReadOnly` is true.
- **Step Property**: The `Step` property only applies to fields with `Type: Decimal`.
- **Calculated Fields**: When a field has a `Calculated` definition, its value is computed automatically based on dependencies.
- **Built-In Read-Only Fields**: `ItemCode` and `ItemName` are always read-only regardless of configuration.

## Mirrored Fields

Mirrored fields automatically copy values to related fields. This is useful for maintaining consistency between purchase and sales units.

Example: If you set `MirrorTo: SalesUnitLength` on `PurchaseUnitLength`, the purchase unit length value is automatically copied to the sales unit length field.

## Calculated Fields

Calculated fields compute their values using a formula based on other fields. They support complex expressions with multiple dependencies.

### Calculated Field Properties

| Property | Type | Description |
|----------|------|-------------|
| `Formula` | String | Expression that calculates the field value. Uses `{FieldName}` syntax for placeholders. Required. |
| `Dependencies` | Array | List of field IDs that this calculated field depends on. Required. |
| `Precision` | Integer | Number of decimal places to round the result. Default: `0`. |
| `ClearDependenciesOnManualEdit` | Boolean | When true, dependency fields are cleared if the calculated field is manually edited. Default: `false`. |

### Example: Volume Calculation

A volume field can be calculated as the product of length, width, and height:

```yaml
- Id: PurchaseUnitVolume
  Description: Unit Volume (m³)
  Type: Decimal
  Step: 4
  ReadOnly: false
  Required: false
  MirrorTo: SalesUnitVolume
  Calculated:
    Formula: "{PurchaseUnitLength} * {PurchaseUnitWidth} * {PurchaseUnitHeight}"
    Dependencies:
      - PurchaseUnitLength
      - PurchaseUnitWidth
      - PurchaseUnitHeight
    Precision: 4
    ClearDependenciesOnManualEdit: true
```

## Complete Annotated Example

Below is a complete example configuration with common use cases:

```yaml
Item:
  MetadataDefinition:
    # System identifier fields (always read-only)
    - Id: ItemCode
      Description: Item Code
      Type: String
      ReadOnly: true
      Required: false

    - Id: ItemName
      Description: Item Name
      Type: String
      ReadOnly: true
      Required: false

    # Dimensional fields with mirroring to sales units
    - Id: PurchaseUnitLength
      Description: Unit Length
      Type: Decimal
      Step: 4
      ReadOnly: false
      Required: false
      MirrorTo: SalesUnitLength

    - Id: PurchaseUnitWidth
      Description: Unit Width
      Type: Decimal
      Step: 4
      ReadOnly: false
      Required: false
      MirrorTo: SalesUnitWidth

    - Id: PurchaseUnitHeight
      Description: Unit Height
      Type: Decimal
      Step: 4
      ReadOnly: false
      Required: false
      MirrorTo: SalesUnitHeight

    # Calculated field that depends on dimensions
    - Id: PurchaseUnitVolume
      Description: Unit Volume (m³)
      Type: Decimal
      Step: 4
      ReadOnly: false
      Required: false
      MirrorTo: SalesUnitVolume
      Calculated:
        Formula: "{PurchaseUnitLength} * {PurchaseUnitWidth} * {PurchaseUnitHeight}"
        Dependencies:
          - PurchaseUnitLength
          - PurchaseUnitWidth
          - PurchaseUnitHeight
        Precision: 4
        ClearDependenciesOnManualEdit: true

    # Required editable field
    - Id: PurchaseUnitWeight
      Description: Unit Weight (KG)
      Type: Decimal
      ReadOnly: false
      Required: true
      MirrorTo: SalesUnitWeight

    # Custom SAP user-defined field
    - Id: U_B1SStdTP
      Description: Template Name
      Type: String
      ReadOnly: false
      Required: true

    # Audit tracking fields
    - Id: U_LW_UPDATE_TIMESTAMP
      Description: Last Update Time
      Type: Date
      ReadOnly: false
      Required: false

    - Id: U_LW_UPDATE_USER
      Description: Last Update ID
      Type: Integer
      ReadOnly: false
      Required: false
```

## Best Practices

- **Keep Required Fields Minimal**: Mark only fields that are essential for business logic as required.
- **Use Mirrors for Consistency**: Use `MirrorTo` to keep related purchase and sales fields synchronized.
- **Validate Dependencies**: Ensure all fields referenced in a calculated formula exist and are defined before the calculated field.
- **Set Appropriate Precision**: For calculated decimal fields, set `Precision` to match your business requirements.
- **Use Descriptive Labels**: Write clear `Description` values; these appear to end users in the UI.
