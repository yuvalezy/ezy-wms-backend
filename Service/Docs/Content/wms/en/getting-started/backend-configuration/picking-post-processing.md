---
title: Picking Post-Processing (PickingPostProcessing.yaml)
description: Reference for the EZY WMS PickingPostProcessing.yaml file that registers post-pick processors implemented in customer extension assemblies.
---

> **Current WMS note:** Live configuration is stored in the database and managed from the superuser Configuration screen. YAML files under `Service/init` are seed templates, not the runtime configuration source.

## Overview

The picking post processing configuration defines custom .NET processors that run automatically after picking operations are completed. These processors enable business logic such as SAP integration, packaging calculations, inventory adjustments, and external system notifications.

**Configuration file location:**
```
ezy-wms-backend/Service/config/PickingPostProcessing.yaml
```

Alternatively, processors can be configured in customer-extension `appsettings.json` under the `PickingPostProcessing.Processors` array (see Deployment Notes below).

## Configuration Structure

The configuration defines a top-level `PickingPostProcessingProcessors` array. Each entry registers one post-processor.

### Processor Fields

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `Id` | string | Yes | — | Unique identifier for this processor. Must be unique across all post-processors. |
| `Assembly` | string | Yes | — | .NET assembly filename (e.g., `Customer.Extensions.dll`) containing the processor implementation. |
| `TypeName` | string | Yes | — | Full .NET type name implementing the post-processor interface (e.g., `Customer.Extensions.PackagingCalculatorPostProcessor`). |
| `Enabled` | bool | No | `true` | When `false`, this processor is skipped during execution. |
| `Configuration` | object | No | — | Free-form dictionary of processor-specific settings passed at runtime. |

### Configuration Dictionary Keys

The `Configuration` section contains processor-defined keys. Common keys include:

| Key | Type | Example | Notes |
|-----|------|---------|-------|
| `Enabled` | bool | `true` | Processor logic enabled/disabled. |
| `Description` | string | `"Calculates packaging requirements"` | Describes the processor purpose. |
| `BatchSize` | int | `100` | Number of items per batch. |
| `TimeoutSeconds` | int | `30` | Processor execution timeout. |
| `RetryAttempts` | int | `3` | Retry count on failure. |
| `LogLevel` | string | `Information` | Logging verbosity: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`. |
| `ConnectionString` | string | See example | Database connection (processor-specific). |
| `ApiEndpoint` | string | See example | External API endpoint URL. |
| `ApiKey` | string | See example | API authentication key. |
| `ProcessingMode` | string | `Synchronous` or `Asynchronous` | Execution mode. |
| `MaxConcurrency` | int | `5` | Maximum concurrent operations. |
| `Recipients` | array | `["admin@example.com"]` | Notification recipients (integration-dependent). |

**Important:** Configuration keys and their values are processor-specific. Consult the processor's documentation for available settings.

## Example Configurations

### Basic Packaging Calculator

```yaml
PickingPostProcessingProcessors:
  - Id: customer-packaging-calculator
    Assembly: Customer.Extensions.dll
    TypeName: Customer.Extensions.PackagingCalculatorPostProcessor
    Enabled: true
    Configuration:
      Enabled: true
      Description: Calculates packaging requirements and updates SAP Order lines
      BatchSize: 100
      TimeoutSeconds: 30
      RetryAttempts: 3
      LogLevel: Information
```

### SAP Integration Processor (with external settings)

```yaml
  - Id: sap-integration-processor
    Assembly: Customer.Extensions.dll
    TypeName: Customer.Extensions.SapIntegrationPostProcessor
    Enabled: true
    Configuration:
      Enabled: true
      Description: Sends picking completion events to SAP
      ConnectionString: "Server=your-sap-server;SystemId=YOUR_SYSTEM_ID;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
      ApiEndpoint: "https://api.example.com/sap-webhook"
      ApiKey: "YOUR_API_KEY"
      ProcessingMode: Asynchronous
      MaxConcurrency: 5
      TimeoutSeconds: 60
      RetryAttempts: 5
      LogLevel: Information
      Recipients:
        - admin@example.com
        - warehouse@example.com
```

## Deployment Notes

1. **Assembly Deployment:** The processor assembly (`.dll` file) must be deployed alongside the Service executable in the same directory or in the configured assembly search paths.

2. **Type Contract:** The `TypeName` must reference a class implementing the EZY WMS picking post-processor interface. Refer to the `Customer.Extensions` project for the interface contract and implementation examples.

3. **Configuration Schema:** Each processor defines its own configuration schema. Configuration keys are processor-specific — consult the processor's documentation for valid keys and expected value types.

4. **Runtime Behavior:**
   - Processors execute sequentially in the order defined in the configuration.
   - If `Enabled` is `false`, the processor is skipped.
   - If a processor throws an uncaught exception, the picking operation is marked as completed, but the error is logged. Subsequent processors still execute.

5. **Logging:** Set `LogLevel` appropriately for troubleshooting. Use `Debug` or `Trace` for development; `Information` or higher for production.

6. **JSON Configuration (appsettings):** Processors can also be configured in a customer-extension `appsettings.json` file under:
   ```json
   {
     "PickingPostProcessing": {
       "Processors": [ /* array of processor objects */ ]
     }
   }
   ```
   The JSON structure mirrors the YAML format.

## Complete Annotated Example

```yaml
# Picking Post Processing Configuration
# Defines post-processors that run after picking operations are completed
PickingPostProcessingProcessors:

  # Packaging Calculator Processor
  - Id: customer-packaging-calculator
    Assembly: Customer.Extensions.dll
    TypeName: Customer.Extensions.PackagingCalculatorPostProcessor
    Enabled: true
    Configuration:
      Enabled: true
      Description: Calculates packaging requirements and updates SAP Order lines
      BatchSize: 100
      TimeoutSeconds: 30
      RetryAttempts: 3
      LogLevel: Information
      # Optional keys (processor-specific; include if processor supports them):
      # ConnectionString: "Server=your-db;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
      # ApiEndpoint: "https://api.example.com/webhook"
      # ApiKey: "YOUR_API_KEY"

  # SAP Integration Processor (commented out example)
  # - Id: sap-integration-processor
  #   Assembly: Customer.Extensions.dll
  #   TypeName: Customer.Extensions.SapIntegrationPostProcessor
  #   Enabled: false
  #   Configuration:
  #     Enabled: true
  #     Description: Sends picking events to SAP
  #     ConnectionString: "Server=your-sap-server;SystemId=YOUR_SYSTEM;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
  #     ProcessingMode: Asynchronous
  #     MaxConcurrency: 5
  #     TimeoutSeconds: 60
  #     RetryAttempts: 5
  #     LogLevel: Information
  #     Recipients:
  #       - admin@example.com
  #       - warehouse@example.com

  # Inventory Adjustment Processor (commented out example)
  # - Id: inventory-adjustment-processor
  #   Assembly: Warehouse.Extensions.dll
  #   TypeName: Warehouse.Extensions.InventoryAdjustmentProcessor
  #   Enabled: true
  #   Configuration:
  #     AdjustmentReason: "Picking Completion"
  #     RequireApproval: false
  #     ThresholdAmount: 1000.00
```
