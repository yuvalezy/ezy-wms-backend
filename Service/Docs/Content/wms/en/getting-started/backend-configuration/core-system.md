---
title: Core System (Configurations.yaml)
description: Reference for the EZY WMS Configurations.yaml file covering business options, data filters, warehouse mappings and SAP B1 connection.
---

> **Current WMS note:** Live configuration is stored in the database and managed from the superuser Configuration screen. YAML files under `Service/init` are seed templates, not the runtime configuration source.

`Configurations.yaml` is the core-system seed/migration template at
`ezy-wms-backend/Service/init/Configurations.yaml`. Its values are stored in the
database and edited through the super-user Configuration screen after startup.

## Options

Business operation settings that control core WMS functionality and workflow behavior.

### General Settings

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `EnableUseBaseUn` | bool | `true` | Enable base unit conversion functionality |
| `UnitLabel` | string (nullable) | — | Custom label for base unit (e.g., "Meter") |
| `UnitAbbr` | string (nullable) | — | Custom abbreviation for base unit (e.g., "M") |
| `DozensLabel` | string (nullable) | — | Custom label for dozens unit (e.g., "Metro(s)") |
| `DozensAbbr` | string (nullable) | — | Custom abbreviation for dozens unit (e.g., "M") |
| `BoxLabel` | string (nullable) | — | Custom label for box/pack unit |
| `BoxAbbr` | string (nullable) | — | Custom abbreviation for box/pack unit |
| `MaxUnitLevel` | enum | — | Maximum unit level in stock formatting: `Unit` (full breakdown), `Dozen` (stop at dozens level), `Pack` (stop at pack level) |
| `ScannerMode` | enum | `ItemBarcode` | Input mode for barcode/item identification: `ItemBarcode` (scan barcodes), `ItemCode` (manual code entry) |
| `DisplayVendor` | bool | `true` | Display vendor information in UI screens |
| `WhsCodeBinSuffix` | bool | `false` | Append warehouse code as suffix to bin codes |

### Goods Receipt

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `GoodsReceiptDraft` | bool | `false` | Create goods receipt documents as drafts (requires approval) instead of direct posting |
| `GoodsReceiptModificationsRequiredSupervisor` | bool | `false` | Require supervisor approval for modifications to goods receipts |
| `GoodsReceiptCreateSupervisorRequired` | bool | `false` | Require supervisor approval to create goods receipts |
| `GoodsReceiptType` | enum | `Both` | Document type to create: `Transactional` (inventory transaction), `Confirmation` (document confirmation), `Both` |
| `GoodsReceiptTargetDocuments` | bool | `false` | Enable target document selection in goods receipt |
| `GoodsReceiptConfirmationAdjustStock` | bool | `false` | Automatically adjust stock levels when confirming goods receipt |
| `GoodsReceiptConfirmationAdjustStockPriceList` | int (nullable) | — | Price list ID used when adjusting stock during confirmation |

### Transfer

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `TransferTargetItems` | bool | `false` | Enable target item selection in transfers |
| `EnableTransferConfirm` | bool | `false` | Require confirmation step for transfer operations |
| `EnableTransferRequest` | bool | `false` | Enable stock transfer request workflow |
| `EnableWarehouseTransfer` | bool | `false` | Enable cross-warehouse transfers with approval workflow |
| `TransferCreateSupervisorRequired` | bool | `false` | Require supervisor approval to create inventory transfers |
| `DirectTransferAll` | bool | `false` | Transfer 100% of scanned item's stock from source bin (skips quantity input) |

### Package & Unit

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `DefaultUnitType` | enum | `Pack` | Default unit type: `Unit` (individual), `Dozen` (12 units), `Pack` (package/case) |
| `EnableUnitSelection` | bool | `false` | Allow users to select different unit types during operations |

### Session Management

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `IdleLogoutTimeout` | int (nullable) | `null` | Idle timeout in seconds before automatic logout (0 or null = no timeout) |

### Pick List

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `EnablePickingCheck` | bool | `false` | Require verification/check steps for picking operations |

### Quantity

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `EnableDecimalQuantities` | bool | `false` | Allow decimal quantities (2 decimal places precision) in all operations |

### Document Unit Overrides

Optionally override global unit settings for specific document types. Supported keys: `GoodsReceipt`, `InventoryCounting`, `Transfer`, `Picking`. Any property not specified falls back to the global `Options` value.

```yaml
DocumentUnitOverrides:
  GoodsReceipt:
    DefaultUnitType: Pack
  InventoryCounting:
    DefaultUnitType: Dozen
  Transfer:
    DefaultUnitType: Unit
```

## Filters

SQL-based filters restrict data visibility and access in queries.

| Field | Type | Description |
|-------|------|-------------|
| `Vendors` | string (nullable) | SQL WHERE clause for vendor filtering (e.g., `"QryGroup1" = 'Y'`) |
| `PickPackOnly.Query` | string | SQL WHERE clause for pick-pack customer filtering |
| `PickPackOnly.GroupBy` | string | SQL GROUP BY expression for pick-pack customer results |

**Note:** Filter values are raw SQL clauses. Ensure proper syntax for your database type.

## Warehouses

Warehouse-specific configuration keyed by warehouse code.

| Field | Type | Description |
|-------|------|-------------|
| `InitialCountingBinEntry` | int (nullable) | Bin entry ID for stock counting initialization |
| `CancelPickingBinEntry` | int | Bin entry ID for cancelled pick return location |
| `StagingBinEntry` | int (nullable) | Bin entry ID for staging/temporary holding area |

Example:

```yaml
Warehouses:
  WH01:
    InitialCountingBinEntry: 1001
    CancelPickingBinEntry: 1002
    StagingBinEntry: 1003
```

## External Adapter

Specifies the SAP Business One integration adapter.

The former `ExternalAdapter` setting is retired. This checkout uses the SAP
Service Layer for document writes and SQL Server-backed adapters for SAP reads.

## SboSettings

Configuration for connecting to SAP Business One database and services.

| Field | Type | Description |
|-------|------|-------------|
| `Server` | string | SAP B1 server hostname or IP address |
| `ServiceLayerUrl` | string (nullable) | SAP Service Layer URL (required for `SboServiceLayer`); format: `https://server:port` (port typically 50000) |
| `ServerType` | int | SQL Server database server type (`4`, `6`, `7`, `8`, `10`, `11`, `15`) |
| `TrustedConnection` | bool | Use Windows Authentication for database connection |
| `ServerUser` | string (nullable) | Database username (required if `TrustedConnection` is false) |
| `ServerPassword` | string (nullable) | Database password (required if `TrustedConnection` is false) |
| `Database` | string | SAP B1 database name |
| `User` | string (nullable) | SAP B1 application username |
| `Password` | string (nullable) | SAP B1 application password |

### ServerType Mapping

| Value | Database Server |
|-------|-----------------|
| `4` | MSSQL 2005 |
| `6` | MSSQL 2008 |
| `7` | MSSQL 2012 |
| `8` | MSSQL 2014 |
| `10` | MSSQL 2016 |
| `11` | MSSQL 2017 |
| `15` | MSSQL 2019 |

## Complete Configuration Example

```yaml
# Core System Configurations
# Essential business operation settings, external system integration, and warehouse configurations
# This is a mandatory configuration file for system operation

# Business Operation Options
# Controls core WMS functionality and workflow behavior
Options:
  # General Settings
  EnableUseBaseUn: true
  DozensLabel: "Metros"
  DozensAbbr: "M"
  MaxUnitLevel: Pack
  ScannerMode: ItemBarcode
  DisplayVendor: true
  WhsCodeBinSuffix: false

  # Goods Receipt Configuration
  GoodsReceiptDraft: false
  GoodsReceiptModificationsRequiredSupervisor: true
  GoodsReceiptCreateSupervisorRequired: true
  GoodsReceiptType: Both
  GoodsReceiptTargetDocuments: false
  GoodsReceiptConfirmationAdjustStock: true

  # Transfer Configuration
  TransferTargetItems: false
  EnableTransferConfirm: true
  EnableTransferRequest: false
  EnableWarehouseTransfer: true
  TransferCreateSupervisorRequired: false
  DirectTransferAll: true

  # Unit Configuration
  DefaultUnitType: Pack
  EnableUnitSelection: true

  # Session Management
  IdleLogoutTimeout: 0

  # Pick List Configuration
  EnablePickingCheck: true

  # Quantity Configuration
  EnableDecimalQuantities: true

  # Document Unit Overrides
  DocumentUnitOverrides:
    Transfer:
      DefaultUnitType: Unit

# Data Filtering Configuration
# SQL-based filters for restricting data visibility and access
Filters:
  Vendors: "\"QryGroup1\" = 'Y'"
  PickPackOnly:
    Query: "OCRD.\"QryGroup4\" = 'Y'"
    GroupBy: "OCRD.\"QryGroup4\""

# Warehouse-Specific Configuration
# Bin entry mappings and warehouse-specific operational settings
Warehouses:
  WH01:
    InitialCountingBinEntry: 1001
    CancelPickingBinEntry: 1002
    StagingBinEntry: 1003

# SAP Business One Connection Settings
# Configuration for connecting to SAP B1 database and services
SboSettings:
  Server: your-sap-server
  ServiceLayerUrl: https://your-sap-server:50000
  ServerType: 15
  TrustedConnection: false
  ServerUser: YOUR_SAP_USER
  ServerPassword: YOUR_SAP_PASSWORD
  Database: YOUR_DATABASE
  User: YOUR_SAP_USER
  Password: YOUR_SAP_PASSWORD
```
