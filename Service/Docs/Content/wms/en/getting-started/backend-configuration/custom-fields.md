---
title: Custom Fields Configuration
description: Configure custom fields for Items, PickingDetails, Warehouses, and Vendors in EZY WMS.
---

> **Current WMS note:** Live configuration is stored in the database and managed from the superuser Configuration screen. YAML files under `Service/init` are seed templates, not the runtime configuration source.

## Overview

Custom Fields allow you to extend standard data models in EZY WMS by mapping SAP database columns and calculated SQL expressions to field identifiers. These fields are then available for display and use across different modules.

Configuration file location:
```
ezy-wms-backend/Service/config/CustomFields.yaml
```

## Structure

The `CustomFields` section is a dictionary keyed by collection name. Each collection contains an array of custom field definitions:

```yaml
CustomFields:
  Items:
    - Key: FieldId
      Description: Display Label
      Type: Text
      Query: SQL_EXPRESSION
      GroupBy: SQL_GROUP_EXPRESSION  # optional
  PickingDetails:
    # ...
```

## CustomField Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Key` | string | Yes | Unique identifier for the field within the collection. |
| `Description` | string | Yes | Human-readable label displayed in the UI. |
| `Type` | enum | Yes | Data type: `Text` (0), `Number` (1), or `Date` (2). |
| `Query` | string | Yes | Raw SQL expression that retrieves the field value. Ignored in JSON serialization. |
| `GroupBy` | string | No | SQL grouping expression used in aggregated queries. Ignored in JSON serialization. |

## Important Notes

- **SQL Fragments**: `Query` and `GroupBy` are raw SQL fragments embedded into larger queries by the service. They must be valid for your SAP database flavor (SQL Server, etc.).
- **JSON Serialization**: `Query` and `GroupBy` properties are marked with `[JsonIgnore]` and are not sent to clients.
- **Uniqueness**: Each `Key` must be unique within its collection.

## Examples

### Items Collection

Define custom fields for item-related operations:

```yaml
CustomFields:
  Items:
    - Key: FrgnName
      Description: Second Name
      Type: Text
      Query: OITM.FrgnName
    
    - Key: ItmsGrpNam
      Description: Item Group
      Type: Text
      Query: (SELECT [ItmsGrpNam] FROM OITB WHERE ItmsGrpCod = OITM.ItmsGrpCod)
      GroupBy: OITM.ItmsGrpCod
    
    - Key: UpdateDate
      Description: Update Date
      Type: Date
      Query: OITM.UpdateDate
```

### PickingDetails Collection

Include a field with SQL grouping for aggregated operations:

```yaml
CustomFields:
  PickingDetails:
    - Key: Marca
      Description: Marca
      Type: Text
      Query: COALESCE(ORDR.U_MARCA, OINV.U_MARCA, OWTQ.U_MARCA)
      GroupBy: ORDR.U_MARCA, OINV.U_MARCA, OWTQ.U_MARCA
```

## Additional Collections (Optional)

You can extend custom fields to other collections. Uncomment and customize as needed:

```yaml
CustomFields:
  # Warehouses:
  #   - Key: WarehouseCode
  #     Description: Warehouse Code
  #     Type: Text
  #     Query: OWHS.WhsCode
  #
  # Vendors:
  #   - Key: VendorCode
  #     Description: Vendor Code
  #     Type: Text
  #     Query: OCRD.CardCode
```

## Complete Example

Below is a fully annotated `CustomFields.yaml` configuration:

```yaml
# Custom Fields Configuration
# Defines custom fields that can be displayed and used across different modules
# Maps to Dictionary<string, CustomField[]> in Settings.CustomFields

CustomFields:
  # Items custom fields - Fields available for item-related operations
  Items:
    # Second Name - a simple column reference
    - Key: FrgnName
      Description: Second Name
      Type: Text
      Query: OITM.FrgnName
      # GroupBy is optional; omitted here since no aggregation is needed

    # Item Group - uses a subquery and grouping for aggregated results
    - Key: ItmsGrpNam
      Description: Item Group
      Type: Text
      Query: (SELECT [ItmsGrpNam] FROM OITB WHERE ItmsGrpCod = OITM.ItmsGrpCod)
      GroupBy: OITM.ItmsGrpCod

    # Firm Name - another subquery with grouping
    - Key: FirmName
      Description: Firm Name
      Type: Text
      Query: (SELECT [FirmName] FROM OMRC WHERE FirmCode = OITM.FirmCode)
      GroupBy: OITM.FirmCode

    # Numeric field - item entry identifier
    - Key: DocEntry
      Description: Item Entry
      Type: Number
      Query: OITM.DocEntry

    # Date field - last update timestamp
    - Key: UpdateDate
      Description: Update Date
      Type: Date
      Query: OITM.UpdateDate

    # Volume in cubic meters
    - Key: Volume
      Description: Volumen (m³)
      Type: Number
      Query: OITM.BVolume

    # Weight in kilograms
    - Key: Weight
      Description: Peso (kg)
      Type: Number
      Query: OITM.BVolume

    # Purchase factor
    - Key: Factor
      Description: Factor
      Type: Number
      Query: OITM.PurFactor1

  # PickingDetails custom fields
  PickingDetails:
    # Marca field with COALESCE fallback logic and grouping
    - Key: Marca
      Description: Marca
      Type: Text
      Query: COALESCE(ORDR.U_MARCA, OINV.U_MARCA, OWTQ.U_MARCA)
      GroupBy: ORDR.U_MARCA, OINV.U_MARCA, OWTQ.U_MARCA

  # Uncomment to add custom fields for Warehouses
  # Warehouses:
  #   - Key: WarehouseCode
  #     Description: Warehouse Code
  #     Type: Text
  #     Query: OWHS.WhsCode
  #
  #   - Key: WarehouseName
  #     Description: Warehouse Name
  #     Type: Text
  #     Query: OWHS.WhsName

  # Uncomment to add custom fields for Vendors
  # Vendors:
  #   - Key: VendorCode
  #     Description: Vendor Code
  #     Type: Text
  #     Query: OCRD.CardCode
  #
  #   - Key: VendorName
  #     Description: Vendor Name
  #     Type: Text
  #     Query: OCRD.CardName
  #
  #   - Key: CreditLimit
  #     Description: Credit Limit
  #     Type: Number
  #     Query: OCRD.CreditLine
```

## Validation

- Ensure all `Key` values are unique within each collection.
- Verify that SQL expressions in `Query` and `GroupBy` are valid for your database.
- Confirm that referenced tables and columns exist in your SAP database schema.
