---
title: External Commands Configuration
description: Configure automated external commands that trigger file generation and delivery on warehouse events
---

> **Current WMS note:** Live configuration is stored in the database and managed from the superuser Configuration screen. YAML files under `Service/init` are seed templates, not the runtime configuration source.

## Overview

The `ExternalCommands.yaml` configuration file enables EZY WMS to automatically generate and deliver XML or JSON files to external systems in response to warehouse events. Commands can be triggered by system events (package creation, activation, closure) or manual user actions, with support for multiple destination types and advanced features like batch execution and retry policies.

**Configuration location:** `ezy-wms-backend/Service/config/ExternalCommands.yaml`

For richer JSON examples with extended field coverage, see `appsettings.external_commands_example.json` in the source repository.

## Core Concepts

- **Commands:** Individual external command configurations that define what data to collect, how to format it, and where to deliver it.
- **Queries:** SQL queries executed to gather data from the database, supporting parameterization and batch operations.
- **Destinations:** Delivery targets including local paths, network shares, FTP, and SFTP servers.
- **Global Settings:** System-wide configuration for concurrency, timeouts, retries, and file formatting.

## Commands Array

Each command in `ExternalCommands.Commands` is an independent configuration with the following structure:

### Command Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | string | Yes | Unique identifier for the command |
| `Name` | string | Yes | Human-readable name |
| `Description` | string | Yes | Description of functionality |
| `ObjectType` | enum | Yes | Type of object: `GoodsReceipt`, `InventoryCounting`, `Transfer`, `Picking`, `Package`, `PickingClosure` |
| `TriggerType` | enum | Yes | When to trigger: `CreatePackage`, `ActivatePackage`, `ClosePackage`, `Manual` |
| `Enabled` | bool | No | Enable/disable command (default: `true`) |
| `AllowBatchExecution` | bool | No | Support batch execution for multiple items (default: `false`) |
| `Queries` | array | Yes | Array of `CommandQuery` objects |
| `FileFormat` | enum | Yes | Output format: `XML` or `JSON` |
| `FileNamePattern` | string | Yes | Filename template with placeholders |
| `Destination` | object | Yes | Delivery configuration |
| `UIConfiguration` | object | No | UI settings for manual commands |

### ObjectType Enum

- `GoodsReceipt` (0)
- `InventoryCounting` (1)
- `Transfer` (2)
- `Picking` (3)
- `Package` (4)
- `PickingClosure` (5)

### TriggerType Enum

- `CreatePackage` (0) — Triggered when a package is created
- `ActivatePackage` (1) — Triggered when a package is activated
- `ClosePackage` (2) — Triggered when a package is closed
- `Manual` (3) — Triggered by user action (requires `UIConfiguration`)

## Queries

Queries retrieve data from the database to populate the generated file. Each query supports parameters and can return single or multiple rows.

| Field | Type | Description |
|-------|------|-------------|
| `Name` | string | Query identifier |
| `Query` | string | SQL statement; supports `@ObjectId`, `@PackageId`, `@PackageIds` parameters |
| `ResultType` | enum | `Single` (one row) or `Multiple` (multiple rows) |
| `IsBatchQuery` | bool | Indicates support for batch execution with multiple IDs (default: `false`) |

## File Format & Naming

### FileFormat

- `XML` (0) — Generate XML documents
- `JSON` (1) — Generate JSON documents

### FileNamePattern Placeholders

| Placeholder | Example | Notes |
|-------------|---------|-------|
| `{Barcode}` | `PKG123456` | Package barcode from query result |
| `{ObjectId}` | `42` | Object ID being processed |
| `{WhsCode}` | `WH01` | Warehouse code from query result |
| `{Timestamp:format}` | `20260424143022` | Current timestamp; use .NET format (e.g., `yyyyMMddHHmmss`) |
| `{BatchIndex}` | `1` | Index in batch execution (0-based) |
| Query fields | Custom | Any field returned by queries |

Example: `PKG_{Barcode}_{Timestamp:yyyyMMddHHmmss}.xml` → `PKG_ABC123_20260424143022.xml`

## Destination Configuration

Destination specifies where the generated file is delivered.

| Field | Type | Applies To | Description |
|-------|------|-----------|-------------|
| `Type` | enum | All | `LocalPath`, `NetworkPath`, `FTP`, `SFTP` |
| `Path` | string | All | File path, UNC path, or FTP/SFTP directory |
| `Host` | string | FTP, SFTP | Hostname or IP address |
| `Port` | int | FTP, SFTP | Port number (21 for FTP, 22 for SFTP) |
| `Username` | string | FTP, SFTP, NetworkPath | Authentication username |
| `Password` | string | FTP, SFTP, NetworkPath | Authentication password (recommend encryption) |
| `UseNetworkImpersonation` | bool | NetworkPath | Use impersonation for network access (default: `false`) |
| `UsePassiveMode` | bool | FTP | FTP passive mode (default: `true`) |
| `UseSsl` | bool | FTP | FTP over SSL/TLS (default: `false`) |
| `PrivateKeyPath` | string | SFTP | Path to private key file |
| `PrivateKeyPassphrase` | string | SFTP | Passphrase for encrypted private key |
| `HostFingerprint` | string | SFTP | Expected host fingerprint for verification |

### Destination Types

**LocalPath:** Writes to a local filesystem directory.
```yaml
Destination:
  Type: LocalPath
  Path: C:\PrintQueue\Labels\Incoming
```

**NetworkPath:** Writes to a network share (UNC path) with optional impersonation.
```yaml
Destination:
  Type: NetworkPath
  Path: \\PrintServer\Labels\PackageContents
  UseNetworkImpersonation: true
  Username: DOMAIN\service_account
  Password: YOUR_PASSWORD  # Recommend encryption/secret reference
```

**FTP:** Delivers via FTP (File Transfer Protocol).
```yaml
Destination:
  Type: FTP
  Host: ftp.example.com
  Port: 21
  Path: /imports/packages
  Username: your-ftp-user
  Password: YOUR_FTP_PASSWORD  # Recommend encryption/secret reference
  UsePassiveMode: true
  UseSsl: false  # Set to true for FTPS
```

**SFTP:** Delivers via SFTP (SSH File Transfer Protocol) with key-based authentication.
```yaml
Destination:
  Type: SFTP
  Host: sftp.partner.example
  Port: 22
  Path: /incoming/packages
  Username: your-sftp-user
  Password: YOUR_SFTP_PASSWORD  # Use if password auth; encrypt if provided
  PrivateKeyPath: C:\Keys\partner_sftp_key.ppk  # Path to private key
  PrivateKeyPassphrase: YOUR_KEY_PASSPHRASE
  HostFingerprint: ssh-rsa 2048 xx:xx:xx:...:xx  # Host fingerprint verification
```

## UI Configuration (Manual Commands)

Required when `TriggerType` is `Manual`. Controls how the command is presented to users.

| Field | Type | Description |
|-------|------|-------------|
| `AllowedScreens` | string[] | List of screens where the command button appears |
| `ButtonText` | string | Button label text (default: "Execute Command") |
| `RequireConfirmation` | bool | Require user confirmation (default: `true`) |
| `ConfirmationMessage` | string | Confirmation dialog text; supports `{count}` for batch operations |
| `MaxBatchSize` | int | Maximum items allowed in a batch operation |

## Global Settings

System-wide configuration for all external commands.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `MaxConcurrentExecutions` | int | 5 | Maximum concurrent command executions |
| `CommandTimeout` | int | 30 | Execution timeout in seconds |
| `RetryPolicy.MaxRetries` | int | 3 | Maximum retry attempts |
| `RetryPolicy.RetryDelaySeconds` | int | 5 | Delay between retries (seconds) |
| `RetryPolicy.RetryOnErrors` | string[] | `["NetworkError", "TimeoutError"]` | Error types triggering retries |
| `FileEncoding` | string | UTF-8 | File encoding |
| `XmlSettings.RootElementName` | string | Data | Root XML element name |
| `XmlSettings.IncludeXmlDeclaration` | bool | true | Include XML declaration |
| `XmlSettings.IndentXml` | bool | true | Indent XML output |
| `JsonSettings.IndentJson` | bool | true | Indent JSON output |
| `JsonSettings.CamelCasePropertyNames` | bool | false | Use camelCase for JSON properties |
| `JsonSettings.DateFormat` | string | yyyy-MM-ddTHH:mm:ss | Date format for JSON |

## Examples

### Example 1: Auto-Triggered XML to Local Path

Automatically generates an XML file when a package is created and writes it to a local print queue directory.

```yaml
- Id: AutoPrintPackageLabel
  Name: Auto Print Package Label
  Description: Prints barcode label automatically when package is created
  ObjectType: Package
  TriggerType: CreatePackage
  Enabled: true
  Queries:
    - Name: PackageData
      Query: SELECT p.Barcode, p.CreatedDate FROM Packages p WHERE p.Id = @ObjectId
      ResultType: Single
  FileFormat: XML
  FileNamePattern: PKG_{Barcode}_{Timestamp:yyyyMMddHHmmss}.xml
  Destination:
    Type: LocalPath
    Path: C:\PrintQueue\Labels\Incoming
```

### Example 2: Manual JSON Export with Batch Support

Manual command allowing users to export package content as JSON with batch processing capability.

```yaml
- Id: ExportPackageJSON
  Name: Export Package Content
  Description: Export selected package data as JSON
  ObjectType: Package
  TriggerType: Manual
  Enabled: true
  AllowBatchExecution: true
  UIConfiguration:
    AllowedScreens:
      - PackageDetails
      - PackageList
    ButtonText: Export Selected
    RequireConfirmation: true
    ConfirmationMessage: Export {count} package(s) to external system?
    MaxBatchSize: 50
  Queries:
    - Name: PackageHeader
      Query: SELECT p.Id, p.Barcode, p.WhsCode, p.Status FROM Packages p WHERE p.Id = @ObjectId
      ResultType: Single
    - Name: PackageContents
      Query: SELECT ItemCode, Quantity, BatchNumbers FROM PackageContents WHERE PackageId = @ObjectId
      ResultType: Multiple
  FileFormat: JSON
  FileNamePattern: EXPORT_{WhsCode}_{Barcode}_{Timestamp:yyyyMMddHHmmss}.json
  Destination:
    Type: NetworkPath
    Path: \\ExportServer\PackageExports
    UseNetworkImpersonation: true
    Username: DOMAIN\export_service
    Password: YOUR_PASSWORD
```

### Example 3: SFTP Delivery with Key Authentication

Sends package data to a partner's SFTP server using public key authentication.

```yaml
- Id: SendToPartnerSFTP
  Name: Send Package to Partner
  Description: Sends package information to partner SFTP server
  ObjectType: Package
  TriggerType: CreatePackage
  Enabled: true
  Queries:
    - Name: PackageSummary
      Query: >
        SELECT p.Barcode, p.WhsCode, COUNT(DISTINCT pc.ItemCode) as ItemCount,
        SUM(pc.Quantity) as TotalQuantity
        FROM Packages p LEFT JOIN PackageContents pc ON p.Id = pc.PackageId
        WHERE p.Id = @ObjectId
        GROUP BY p.Id, p.Barcode, p.WhsCode
      ResultType: Single
  FileFormat: JSON
  FileNamePattern: '{WhsCode}_PKG_{Barcode}_{Timestamp:yyyyMMddHHmmss}.json'
  Destination:
    Type: SFTP
    Host: sftp.partner.example
    Port: 22
    Path: /incoming/packages
    Username: your-sftp-user
    Password: YOUR_SFTP_PASSWORD
    PrivateKeyPath: C:\Keys\partner_sftp_key.ppk
    PrivateKeyPassphrase: YOUR_KEY_PASSPHRASE
    HostFingerprint: ssh-rsa 2048 xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx
```

## Complete Annotated Example

Below is a complete `ExternalCommands.yaml` with multiple commands and configured global settings:

```yaml
ExternalCommands:
  # Command definitions
  Commands:
    # Command 1: Automatic XML generation on package creation
    - Id: CreatePackageLabel
      Name: Package Creation Label
      Description: Generates XML label file when package is created
      ObjectType: Package
      TriggerType: CreatePackage
      Enabled: true
      Queries:
        - Name: PackageInfo
          Query: SELECT p.Id, p.Barcode, p.WhsCode, p.CreatedDate FROM Packages p WHERE p.Id = @ObjectId
          ResultType: Single
      FileFormat: XML
      FileNamePattern: PKG_{Barcode}_{Timestamp:yyyyMMddHHmmss}.xml
      Destination:
        Type: LocalPath
        Path: C:\PrintQueue\Labels\Incoming

    # Command 2: Manual export with batch and network destination
    - Id: BatchExportJSON
      Name: Batch Export Package Data
      Description: Export multiple packages to network share as JSON
      ObjectType: Package
      TriggerType: Manual
      Enabled: true
      AllowBatchExecution: true
      UIConfiguration:
        AllowedScreens:
          - PackageList
          - PackageDetails
        ButtonText: Export Selected Packages
        RequireConfirmation: true
        ConfirmationMessage: Export {count} package(s)?
        MaxBatchSize: 100
      Queries:
        - Name: BatchPackageData
          Query: SELECT p.Id, p.Barcode, p.WhsCode, p.Status FROM Packages p WHERE p.Id IN (@PackageIds)
          ResultType: Multiple
          IsBatchQuery: true
      FileFormat: JSON
      FileNamePattern: BATCH_EXPORT_{Timestamp:yyyyMMddHHmmss}_{BatchIndex}.json
      Destination:
        Type: NetworkPath
        Path: \\ExportServer\Packages
        UseNetworkImpersonation: true
        Username: DOMAIN\export_user
        Password: YOUR_PASSWORD

    # Command 3: FTP delivery on package closure
    - Id: ClosedPackageToFTP
      Name: Send Closed Package to FTP
      Description: Uploads closed package data to external FTP server
      ObjectType: Package
      TriggerType: ClosePackage
      Enabled: true
      Queries:
        - Name: ClosedPackageData
          Query: SELECT p.*, b.BinCode FROM Packages p LEFT JOIN Bins b ON p.BinEntry = b.BinEntry WHERE p.Id = @ObjectId
          ResultType: Single
        - Name: PackageLineItems
          Query: SELECT ItemCode, Quantity, BatchNumbers FROM PackageContents WHERE PackageId = @ObjectId
          ResultType: Multiple
      FileFormat: XML
      FileNamePattern: CLOSED_{WhsCode}_{Barcode}_{Timestamp:yyyyMMddHHmmss}.xml
      Destination:
        Type: FTP
        Host: ftp.example.com
        Port: 21
        Path: /packages/closed
        Username: your-ftp-user
        Password: YOUR_FTP_PASSWORD
        UsePassiveMode: true
        UseSsl: false

    # Command 4: SFTP with key authentication
    - Id: SFTPPartnerIntegration
      Name: Partner SFTP Integration
      Description: Securely sends package data to partner via SFTP
      ObjectType: Package
      TriggerType: CreatePackage
      Enabled: true
      Queries:
        - Name: PartnerData
          Query: SELECT p.Barcode, p.WhsCode, COUNT(*) as ItemCount FROM Packages p LEFT JOIN PackageContents pc ON p.Id = pc.PackageId WHERE p.Id = @ObjectId GROUP BY p.Id, p.Barcode, p.WhsCode
          ResultType: Single
      FileFormat: JSON
      FileNamePattern: '{WhsCode}_PKG_{Barcode}.json'
      Destination:
        Type: SFTP
        Host: sftp.partner.example
        Port: 22
        Path: /incoming
        Username: your-sftp-user
        Password: YOUR_SFTP_PASSWORD
        PrivateKeyPath: C:\Keys\partner_key.ppk
        PrivateKeyPassphrase: YOUR_KEY_PASSPHRASE
        HostFingerprint: ssh-rsa 2048 xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx

  # Global system settings
  GlobalSettings:
    # Maximum 5 external commands can run concurrently
    MaxConcurrentExecutions: 5
    
    # Each command has 30 seconds to complete
    CommandTimeout: 30
    
    # Retry failed commands
    RetryPolicy:
      MaxRetries: 3
      RetryDelaySeconds: 5
      RetryOnErrors:
        - NetworkError
        - TimeoutError
    
    # File encoding for all output files
    FileEncoding: UTF-8
    
    # XML formatting settings
    XmlSettings:
      RootElementName: PackageData
      IncludeXmlDeclaration: true
      IndentXml: true
    
    # JSON formatting settings
    JsonSettings:
      IndentJson: true
      CamelCasePropertyNames: false
      DateFormat: yyyy-MM-ddTHH:mm:ss
```

## Security Considerations

- **Credentials:** Store passwords in encrypted form or use secret management systems. Placeholder values like `YOUR_PASSWORD` should be replaced with encrypted values or environment variable references.
- **Network Impersonation:** Use dedicated service accounts with minimal required permissions for network share access.
- **SFTP Keys:** Store private keys securely with restricted file permissions. Passphrases should be encrypted at rest.
- **Host Fingerprint Verification:** For SFTP, capture and validate the partner's host fingerprint to prevent man-in-the-middle attacks.

## Troubleshooting

- **Commands not triggering:** Verify `Enabled: true` and that `TriggerType` matches the warehouse event.
- **File not created:** Check destination path exists and service account has write permissions.
- **Query errors:** Ensure SQL uses valid parameters (`@ObjectId`, `@PackageId`, `@PackageIds`) and that referenced tables/columns exist.
- **Destination connection failures:** Verify credentials, host/port settings, and network connectivity. Check firewall rules for FTP/SFTP ports.
- **Timeout errors:** Increase `CommandTimeout` in `GlobalSettings` if queries are slow or network latency is high.
