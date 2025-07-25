# External Commands API Reference

## Overview

The External Commands API provides endpoints for managing and executing external commands within the WMS system. These APIs enable both automatic command execution (triggered by package events) and manual command execution from the frontend.

## Base URL
All endpoints are relative to the API base URL: `/api/package`

## Authentication
All endpoints require authentication via JWT token or session cookie.

## Endpoints

### 1. Get Manual Commands
Retrieves available manual commands for packages that can be triggered from a specific screen.

#### Request
```http
GET /api/package/manual-commands?screenName={screenName}
```

#### Parameters
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| screenName | string | No | "PackageList" | The screen name where commands will be displayed |

#### Response
```json
[
  {
    "id": "PrintPackageLabel",
    "name": "Print Package Label",
    "description": "Prints a label for the selected package",
    "buttonText": "Print Label",
    "requireConfirmation": true,
    "confirmationMessage": "Print label for this package?",
    "maxBatchSize": 100,
    "allowBatchExecution": true
  }
]
```

#### Response Fields
| Field | Type | Description |
|-------|------|-------------|
| id | string | Unique command identifier |
| name | string | Human-readable command name |
| description | string | Command description |
| buttonText | string | Text to display on trigger button |
| requireConfirmation | boolean | Whether to show confirmation dialog |
| confirmationMessage | string | Message for confirmation dialog |
| maxBatchSize | number | Maximum items in batch (if batch supported) |
| allowBatchExecution | boolean | Whether command supports batch execution |

#### Status Codes
- **200 OK** - Commands retrieved successfully
- **401 Unauthorized** - Authentication required

---

### 2. Execute Manual Command
Executes a specific manual command for a single package.

#### Request
```http
POST /api/package/{id}/execute-command/{commandId}
```

#### Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | guid | Yes | Package unique identifier |
| commandId | string | Yes | Command identifier to execute |

#### Response
```json
{
  "message": "Command executed successfully"
}
```

#### Status Codes
- **200 OK** - Command executed successfully
- **400 Bad Request** - Command execution failed
- **401 Unauthorized** - Authentication required
- **403 Forbidden** - Insufficient permissions
- **404 Not Found** - Package or command not found

#### Required Permissions
- PackageManagement OR PackageManagementSupervisor

---

### 3. Execute Batch Manual Command
Executes a manual command for multiple packages simultaneously.

#### Request
```http
POST /api/package/execute-batch-command
```

#### Request Body
```json
{
  "commandId": "PrintPackageLabel",
  "packageIds": [
    "123e4567-e89b-12d3-a456-426614174000",
    "123e4567-e89b-12d3-a456-426614174001"
  ]
}
```

#### Request Fields
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| commandId | string | Yes | Command identifier to execute |
| packageIds | guid[] | Yes | Array of package IDs to process |

#### Response
```json
{
  "message": "Batch command executed successfully for 2 packages"
}
```

#### Status Codes
- **200 OK** - Batch command executed successfully
- **400 Bad Request** - Command execution failed or command doesn't support batch execution
- **401 Unauthorized** - Authentication required
- **403 Forbidden** - Insufficient permissions

#### Required Permissions
- PackageManagement OR PackageManagementSupervisor

## Automatic Command Execution

Commands configured with automatic triggers execute automatically during package operations. No explicit API calls are required for these triggers.

### Trigger Events

#### CreatePackage Trigger
Commands with `TriggerType: "CreatePackage"` execute automatically when:
- `POST /api/package` (Create Package) completes successfully

#### ClosePackage Trigger  
Commands with `TriggerType: "ClosePackage"` execute automatically when:
- `POST /api/package/{id}/close` (Close Package) completes successfully

## Error Handling

### Common Error Response Format
```json
{
  "error": "Error message describing what went wrong"
}
```

### Error Scenarios

#### Configuration Errors
- Invalid command configuration
- Missing required settings
- Invalid file format or destination settings

#### Execution Errors
- Database query failures
- File generation errors
- Destination delivery failures (network issues, authentication, etc.)

#### Permission Errors
- Insufficient user permissions
- Package not accessible by current user's warehouse

#### Validation Errors
- Invalid package IDs
- Command not found
- Batch size exceeds maximum allowed

## Configuration Examples

### Simple Label Printing Command
```json
{
  "Id": "PrintPackageLabel",
  "Name": "Print Package Label",
  "Description": "Prints barcode label when package is created",
  "ObjectType": "Package",
  "TriggerType": "CreatePackage",
  "Enabled": true,
  "Queries": [
    {
      "Name": "PackageData",
      "Query": "SELECT p.Barcode, p.WhsCode, p.CreatedDate FROM Packages p WHERE p.Id = @PackageId",
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
```

### Manual Export Command with UI Configuration
```json
{
  "Id": "ExportToERP",
  "Name": "Export to ERP System",
  "Description": "Exports package data to external ERP system",
  "ObjectType": "Package",
  "TriggerType": "Manual",
  "Enabled": true,
  "UIConfiguration": {
    "AllowedScreens": ["PackageDetails", "PackageList"],
    "ButtonText": "Export to ERP",
    "RequireConfirmation": true,
    "ConfirmationMessage": "Export this package to the ERP system?"
  },
  "Queries": [
    {
      "Name": "PackageDetails",
      "Query": "SELECT p.*, b.BinCode FROM Packages p LEFT JOIN Bins b ON p.BinEntry = b.BinEntry WHERE p.Id = @PackageId",
      "ResultType": "Single"
    },
    {
      "Name": "PackageContents",
      "Query": "SELECT pc.ItemCode, pc.Quantity, i.ItemName FROM PackageContents pc INNER JOIN Items i ON pc.ItemCode = i.ItemCode WHERE pc.PackageId = @PackageId",
      "ResultType": "Multiple"
    }
  ],
  "FileFormat": "JSON",
  "FileNamePattern": "ERP_EXPORT_{Barcode}_{Timestamp:yyyyMMddHHmmss}.json",
  "Destination": {
    "Type": "SFTP",
    "Host": "erp.company.com",
    "Port": 22,
    "Path": "/imports/packages",
    "Username": "wms_integration",
    "Password": "encrypted_password_here",
    "PrivateKeyPath": "/keys/erp_key.ppk"
  }
}
```

### Batch Command Configuration
```json
{
  "Id": "BatchPrintLabels",
  "Name": "Print Multiple Labels",
  "Description": "Print labels for multiple packages",
  "ObjectType": "Package",
  "TriggerType": "Manual",
  "Enabled": true,
  "AllowBatchExecution": true,
  "UIConfiguration": {
    "AllowedScreens": ["PackageList"],
    "ButtonText": "Print Selected Labels",
    "RequireConfirmation": true,
    "ConfirmationMessage": "Print labels for {count} selected packages?",
    "MaxBatchSize": 50
  },
  "Queries": [
    {
      "Name": "BatchPackageData",
      "Query": "SELECT p.Id, p.Barcode, p.WhsCode FROM Packages p WHERE p.Id IN (@PackageIds)",
      "ResultType": "Multiple",
      "IsBatchQuery": true
    }
  ],
  "FileFormat": "XML",
  "FileNamePattern": "BATCH_LABELS_{Timestamp:yyyyMMddHHmmss}.xml",
  "Destination": {
    "Type": "LocalPath",
    "Path": "C:\\PrintQueue\\Batch"
  }
}
```

## Query Parameters and Placeholders

### Available Parameters
Commands can use these parameters in SQL queries:
- `@PackageId` - Current package ID
- `@PackageIds` - Comma-separated list for batch operations
- `@WhsCode` - Warehouse code from context
- `@Timestamp` - Current UTC timestamp

### File Name Placeholders
File naming patterns support these placeholders:
- `{Barcode}` - Package barcode
- `{WhsCode}` - Warehouse code
- `{Timestamp}` - Current timestamp
- `{Timestamp:format}` - Formatted timestamp (e.g., `{Timestamp:yyyyMMddHHmmss}`)
- `{BatchIndex}` - Unique batch identifier for batch operations

## Security Notes

- All command execution requires appropriate user permissions
- Commands only execute for packages in the user's assigned warehouse
- SQL queries use parameterized statements to prevent injection attacks
- File system access respects configured permissions and network impersonation settings
- Sensitive configuration data (passwords, keys) should be encrypted

## Rate Limiting and Performance

- Maximum concurrent command executions controlled by `GlobalSettings.MaxConcurrentExecutions`
- Command timeout controlled by `GlobalSettings.CommandTimeout`
- Batch operations have configurable size limits
- Retry policies handle transient failures automatically

## Frontend Integration Guidelines

### Displaying Manual Commands
1. Call `GET /api/package/manual-commands?screenName={current_screen}` to get available commands
2. Display commands as buttons/menu items using `buttonText`
3. Show confirmation dialog if `requireConfirmation` is true
4. Support batch selection if `allowBatchExecution` is true

### Executing Commands
1. **Single Package**: Call `POST /api/package/{id}/execute-command/{commandId}`
2. **Multiple Packages**: Call `POST /api/package/execute-batch-command` with package array
3. Handle errors gracefully and display appropriate user messages
4. Provide feedback on successful execution

### Error Handling in UI
- Display specific error messages from API responses
- Provide retry options for transient failures
- Log errors for troubleshooting while showing user-friendly messages