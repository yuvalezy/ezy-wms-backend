# External Commands System Summary

## Overview

The External Commands System is a flexible framework that allows customers to automatically or manually trigger external integrations when specific events occur in the WMS system. The primary use case is for printing labels when packages are created or closed, but the system is designed to support various external integrations.

## Architecture

### Core Components

#### 1. **Settings Models** (`Core/Models/Settings/`)
- **ExternalCommandsSettings.cs** - Main container for all external command configurations
- **ExternalCommand.cs** - Individual command definition with queries, formatting, and destination
- **CommandQuery.cs** - SQL query definitions with parameter support
- **CommandDestination.cs** - Destination configuration (LocalPath, NetworkPath, FTP, SFTP)
- **CommandUIConfiguration.cs** - UI settings for manual command triggers
- **ExternalCommandsGlobalSettings.cs** - Global settings including retry policies and formatting options

#### 2. **Enums** (`Core/Enums/`)
- **CommandTriggerType.cs** - When commands execute (CreatePackage, ClosePackage, Manual)
- **CommandDestinationType.cs** - Where files are delivered (LocalPath, NetworkPath, FTP, SFTP)
- **CommandFileFormat.cs** - Output format (XML, JSON)
- **CommandQueryResultType.cs** - Query result expectations (Single, Multiple)

#### 3. **Services** (`Core/Services/` & `Infrastructure/Services/`)
- **IExternalCommandService.cs** / **ExternalCommandService.cs** - Command execution engine
- **IFileDeliveryService.cs** / **FileDeliveryService.cs** - File delivery to various destinations

### Key Features

#### **Automatic Triggers**
Commands execute automatically on specific events:
- **CreatePackage** - When a new package is created
- **ClosePackage** - When a package is finalized

#### **Manual Execution**
Commands can be triggered manually from the UI with:
- Configurable button text and confirmation messages
- Batch processing support for multiple packages
- Screen-specific command visibility

#### **Flexible Data Collection**
- Multiple SQL queries per command
- Parameter substitution (@PackageId, @WhsCode, etc.)
- Single or multiple row result handling
- Support for complex data relationships

#### **Multiple File Formats**
- **XML** - Configurable root elements, indentation, declarations
- **JSON** - Configurable property naming, indentation, date formatting

#### **Comprehensive Delivery Options**
1. **LocalPath** - Direct file system access
2. **NetworkPath** - UNC paths with optional network impersonation
3. **FTP** - Standard FTP with passive mode and SSL support
4. **SFTP** - Secure FTP with SSH key authentication

#### **Enterprise Features**
- Concurrent execution limits
- Retry policies with configurable delays
- Error handling and comprehensive logging
- File naming patterns with placeholder substitution
- Security features (encrypted passwords, network impersonation)

## Configuration

### Basic Structure
```json
{
  "ExternalCommands": {
    "Commands": [
      {
        "Id": "PrintPackageLabel",
        "Name": "Print Package Barcode Label",
        "Description": "Prints barcode when package is created",
        "ObjectType": "Package",
        "TriggerType": "CreatePackage",
        "Enabled": true,
        "Queries": [
          {
            "Name": "PackageData",
            "Query": "SELECT Barcode, WhsCode FROM Packages WHERE Id = @PackageId",
            "ResultType": "Single"
          }
        ],
        "FileFormat": "XML",
        "FileNamePattern": "PKG_{Barcode}_{Timestamp:yyyyMMddHHmmss}.xml",
        "Destination": {
          "Type": "LocalPath",
          "Path": "C:\\PrintQueue\\Labels"
        }
      }
    ],
    "GlobalSettings": {
      "MaxConcurrentExecutions": 5,
      "CommandTimeout": 30,
      "RetryPolicy": {
        "MaxRetries": 3,
        "RetryDelaySeconds": 5
      }
    }
  }
}
```

## Integration Points

### PackageController Integration
The system integrates seamlessly with the existing PackageController:

1. **Automatic Execution** - Commands trigger on package creation/closure
2. **Manual APIs** - New endpoints for UI integration:
   - `GET /api/package/manual-commands` - Get available commands
   - `POST /api/package/{id}/execute-command/{commandId}` - Execute single command
   - `POST /api/package/execute-batch-command` - Execute batch commands

### Dependency Injection
Services are registered in `DependencyInjectionConfig.cs`:
```csharp
services.AddScoped<IExternalCommandService, ExternalCommandService>();
services.AddScoped<IFileDeliveryService, FileDeliveryService>();
```

## Use Cases

### Primary: Label Printing
- **Creation Labels** - Small barcode labels printed immediately when packages are created
- **Content Labels** - Detailed labels with full package contents printed when packages are closed
- **Batch Printing** - Multiple labels generated simultaneously

### Secondary: External System Integration
- **ERP Exports** - Send package data to external ERP systems
- **Partner Integration** - Share package information with logistics partners
- **Audit Trails** - Export package data for compliance and auditing

## Security Considerations

- **Encrypted Passwords** - All credentials stored encrypted in configuration
- **Network Impersonation** - Secure access to network resources
- **SSH Key Support** - Public key authentication for SFTP
- **Host Fingerprint Validation** - SFTP security verification
- **Input Sanitization** - SQL injection protection through parameterized queries

## Extensibility

The system is designed for easy extension:

### Adding New Object Types
1. Add new enum value to `ObjectType`
2. Create corresponding triggers in controllers
3. Update command validation logic

### Adding New Destination Types
1. Add enum value to `CommandDestinationType`
2. Implement delivery logic in `FileDeliveryService`
3. Add configuration properties to `CommandDestination`

### Adding New File Formats
1. Add enum value to `CommandFileFormat`
2. Implement generation logic in `ExternalCommandService`
3. Add format-specific settings to global configuration

## Error Handling

- **Graceful Degradation** - Command failures don't impact core WMS operations
- **Comprehensive Logging** - Detailed logs for troubleshooting
- **Retry Mechanisms** - Configurable retry policies for transient failures
- **Connection Testing** - Built-in destination connectivity validation

## Performance Considerations

- **Concurrency Control** - Configurable maximum concurrent executions
- **Timeout Management** - Command execution timeouts prevent hanging
- **Temporary File Cleanup** - Automatic cleanup of generated files
- **Semaphore-based Throttling** - Prevents system overload

This system provides a robust, flexible foundation for external integrations while maintaining the security and reliability required for production warehouse management systems.