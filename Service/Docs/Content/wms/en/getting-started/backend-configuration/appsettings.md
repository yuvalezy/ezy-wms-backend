---
title: Backend Configuration (appsettings.json)
description: Configure the EZY WMS Service with appsettings.json. Reference for all settings including CORS, logging, database, authentication, licensing, and background services.
---

> **Current WMS note:** Live configuration is stored in the database and managed from the superuser Configuration screen. YAML files under `Service/init` are seed templates, not the runtime configuration source.

The EZY WMS Service reads infrastructure settings from `appsettings.json`, located
in `ezy-wms-backend/Service/`. WMS business sections are database-backed; this file
covers host settings such as CORS, logging, database connections, JWT
authentication, session management, licensing, background services, email, and
health checks. Use `appsettings.example.json` as a starting point and supply only
the settings required by the deployment.

## Cors

Defines which origins can make cross-origin requests to the API.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `AllowedOrigins` | Array of strings | `["http://localhost:5001"]` | List of allowed origin URLs (e.g., Portal frontend). Wildcard `*` is not recommended for production. |

**Note:** Ensure Portal and other frontend clients are included. Requests from unlisted origins will be rejected.

## Serilog

Structured logging configuration. Logs are written to console and rolling files.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Using` | Array | `["Serilog.Sinks.Console", "Serilog.Sinks.File"]` | Enabled log sinks. |
| `MinimumLevel.Default` | String | `Warning` | Minimum log level for all loggers (e.g., `Debug`, `Information`, `Warning`, `Error`). |
| `MinimumLevel.Override` | Object | See example | Per-namespace overrides (e.g., `Microsoft.Hosting.Lifetime: Information`). |
| `WriteTo[0].Name` | String | `Console` | Log output to console. |
| `WriteTo[1].Name` | String | `File` | Log output to rotating file. |
| `WriteTo[1].Args.path` | String | `./logs/log-.txt` | Log file path with `{Date}` placeholder for daily rotation. |
| `WriteTo[1].Args.rollingInterval` | String | `Day` | Rotate logs daily. Options: `Day`, `Hour`, `Month`. |
| `WriteTo[1].Args.retainedFileCountLimit` | Integer | `7` | Keep this many log files. Older files are deleted. |
| `WriteTo[1].Args.fileSizeLimitBytes` | Integer | `10485760` (10 MB) | Maximum size per log file before rollover. |
| `WriteTo[1].Args.rollOnFileSizeLimit` | Boolean | `true` | Rotate log files when size limit is reached. |
| `Enrich` | Array | `["FromLogContext", "WithMachineName", "WithThreadId"]` | Add context info to logs (machine name, thread ID). |

**Notes:**
- Logs are stored in `./logs/` (relative to service executable).
- Production deployments should use at least `Warning` level to reduce noise.
- Entity Framework Core logs are set to `Warning` by default to reduce verbosity.

## Logging

Legacy .NET logging configuration. Serilog is preferred but both coexist.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `LogLevel.Default` | String | `Error` | Default level for all loggers. |
| `LogLevel.Microsoft` | String | `Error` | Level for Microsoft framework logs. |
| `LogLevel.Microsoft.Hosting.Lifetime` | String | `Error` | Level for service startup/shutdown logs. |

## AllowedHosts

Security setting to restrict which Host headers the service will accept.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `AllowedHosts` | String | `*` | Allowed Host header values. Use `*` to allow all, or list specific hostnames (comma-separated). |

**Note:** In production, replace `*` with actual hostnames or IP addresses to prevent host-based attacks.

## Kestrel

HTTP server configuration.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Kestrel.Endpoints.Http.Url` | String | `http://localhost:5000` | HTTP endpoint URL. Use `http://0.0.0.0:5000` to listen on all interfaces. |

**Note:** Change the port or bind address to match your deployment network. Default `localhost` only accepts local connections.

## ConnectionStrings

Database connection strings for the service and external adapters.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `DefaultConnection` | String | SQL Server connection string | Primary WMS database. Includes server, database name, user, password, and certificate trust settings. |
| `ExternalAdapterConnection` | String | SQL Server connection string | Optional database for external system adapters. Can point to the same or different SQL Server. |
| `ReportingConnection` | String | (blank) | Optional read-only SQL Server connection used **only** to run admin-authored report SQL. Left blank, reports fall back to `ExternalAdapterConnection`. Because report SQL is written at runtime by Super Users, set this to a dedicated read-only login (`db_datareader` with `DENY INSERT, UPDATE, DELETE, EXECUTE`) as the only reliable protection against a destructive typo on a live SAP database. |

**Note:** Both use SQL Server connection string format. For cloud SQL (Azure, etc.), adjust `Server`, `User Id`, and `Password`. `TrustServerCertificate=true` is for development; for production, use proper certificates.

## Jwt

JWT authentication settings for Bearer tokens.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Key` | String | `JwtToken` (example) | Secret key for signing JWT tokens. Must be a long, random string (at least 32 bytes). |
| `Issuer` | String | `YourIssuer` (example) | Token issuer claim (e.g., `EzyWMS`). |
| `Audience` | String | `YourAudience` (example) | Token audience claim (e.g., `EzyWMS-Client`). |
| `ExpiresInMinutes` | Integer | `60` | Token expiration time in minutes. After this, tokens are invalid. |

**Critical:** Replace `Key` with a cryptographically secure random string. The example value `JwtToken` is for demonstration only and must not be used in production.

## SessionManagement

Configures session storage backend (in-memory or Redis).

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Type` | String | `Redis` (example) or `InMemory` | Session storage type. Options: `InMemory` (single-instance only), `Redis` (distributed). |
| `Redis.Host` | String | `localhost` | Redis server hostname or IP. Required if `Type: Redis`. |
| `Redis.Port` | Integer | `6379` | Redis server port. Default is `6379`. |
| `Cookie.Domain` | String or null | `null` | Cookie domain (e.g., `.example.com`). Leave `null` for current domain only. |
| `Cookie.Secure` | Boolean | `false` | Send cookie only over HTTPS. Set `true` in production. |
| `Cookie.SameSite` | String | `Lax` | CSRF protection mode. Options: `Strict`, `Lax`, `None`. Use `Lax` for typical deployments. |
| `Cookie.HttpOnly` | Boolean | `true` | Prevent JavaScript access to session cookie. Keep `true` for security. |

**Critical Notes:**
- If `Type: Redis`, a Redis server must be running and accessible at the specified host and port. Without Redis, session operations will fail.
- For single-instance deployments without Redis, use `InMemory`, but sessions are lost on restart.
- In production, always set `Cookie.Secure: true` and use HTTPS.

## Licensing

License validation and caching settings.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `EncryptionKey` | String (base64) | See example | Base64-encoded 32-byte key for encrypting license data. Must match the key used by the licensing cloud. |
| `CloudEndpoint` | String (URL) | `http://localhost:3001` | Licensing cloud service URL. |
| `BearerToken` | String | See example | Bearer token for authenticating requests to the licensing cloud. |
| `CacheExpirationHours` | Integer | `24` | How long to cache valid licenses locally before re-checking with cloud. |
| `GracePeriodDays` | Integer | `7` | Days the service can operate with an expired or invalid license before blocking operations. |
| `DemoExpirationDays` | Integer | `30` | Validity period for demo licenses. |
| `WarningThresholdDays` | Integer | `3` | Days remaining until license expires when warnings begin. |

**Critical Notes:**
- `EncryptionKey` must be exactly a base64-encoded 32-byte value. Incorrect format will cause license decryption failures.
- `CloudEndpoint` and `BearerToken` must point to the licensing service. Invalid values prevent license validation.
- During the grace period, the service continues operating with a warning, but after grace period expires, features are locked.

## BackgroundServices

Long-running background tasks.

### PickListSync

Synchronizes pick list operations with inventory.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `IntervalSeconds` | Integer | `60` | How often to check for pick list updates (in seconds). |
| `Enabled` | Boolean | `true` | Enable or disable the pick list sync service. |
| `CheckClosedPickLists` | Boolean | `true` | Monitor closed pick lists for completion. |
| `ProcessPackageMovements` | Boolean | `true` | Update inventory when packages are moved. |

### CloudSync

Synchronizes data with the licensing/management cloud.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `SyncIntervalMinutes` | Integer | `10` | How often to sync with cloud (in minutes). |
| `ValidationIntervalHours` | Integer | `24` | How often to perform full validation against cloud (in hours). |
| `Enabled` | Boolean | `true` | Enable or disable cloud sync. |

**Note:** CloudSync requires network connectivity to the cloud endpoint. If the cloud is unreachable, the service continues with cached data but will log warnings.

## Smtp

Email settings for sending notifications (optional).

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Enabled` | Boolean | `false` | Enable or disable email functionality. |
| `Host` | String | Empty | SMTP server hostname (e.g., `smtp.gmail.com`). Required if `Enabled: true`. |
| `Port` | Integer | `587` | SMTP port (typically `587` for TLS or `25` for unencrypted). |
| `EnableSsl` | Boolean | `true` | Use TLS/SSL for the SMTP connection. |
| `Username` | String | Empty | SMTP authentication username. |
| `Password` | String | Empty | SMTP authentication password. |
| `FromEmail` | String | Empty | Sender email address (e.g., `noreply@example.com`). |
| `FromName` | String | `EzyWMS` | Display name for the sender. |
| `TimeoutSeconds` | Integer | `30` | Timeout for SMTP operations (in seconds). |
| `TimeZoneId` | String | `America/Panama` | Timezone for email timestamps and scheduling. |

**Notes:**
- Email is optional. Set `Enabled: false` to disable (emails are not sent).
- If enabled, all SMTP fields must be filled. Invalid credentials will cause email send failures and errors in logs.

## UptimeKuma

Health check integration with UptimeKuma monitoring service (optional).

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Enabled` | Boolean | `false` | Enable or disable UptimeKuma heartbeats. |
| `PushUrl` | String | See example | UptimeKuma push monitor endpoint URL. Format: `https://uptime.example.com/api/push/YOUR_PUSH_KEY`. |
| `HeartbeatIntervalSeconds` | Integer | `60` | How often to send a heartbeat to UptimeKuma (in seconds). |
| `SendOnStartup` | Boolean | `true` | Send a heartbeat when the service starts. |
| `SendOnShutdown` | Boolean | `true` | Send a heartbeat when the service shuts down. |

**Notes:**
- UptimeKuma monitoring is optional. Set `Enabled: false` to disable.
- If enabled, the `PushUrl` must be valid and accessible. Unreachable URLs will cause connection timeouts in logs but will not block service startup.

---

## Complete Example

Below is a fully annotated example `appsettings.json` file suitable for a production deployment. Replace all placeholder values with your environment's actual settings.

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://portal.example.com",
      "https://admin.example.com"
    ]
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "./logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "Microsoft": "Error",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "portal.example.com,admin.example.com,your-db-server",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-db-server;Database=EZY_WMS;User Id=sa;Password=YOUR_DB_PASSWORD;TrustServerCertificate=false;Encrypt=true;",
    "ExternalAdapterConnection": "Server=your-db-server;Database=EZY_ADAPTERS;User Id=sa;Password=YOUR_DB_PASSWORD;TrustServerCertificate=false;Encrypt=true;",
    "ReportingConnection": ""
  },
  "Jwt": {
    "Key": "REPLACE_WITH_RANDOM_SECRET_AT_LEAST_32_BYTES_LONG_AND_CRYPTOGRAPHICALLY_SECURE",
    "Issuer": "EzyWMS",
    "Audience": "EzyWMS-Client",
    "ExpiresInMinutes": 60
  },
  "SessionManagement": {
    "Type": "Redis",
    "Redis": {
      "Host": "your-redis-server",
      "Port": 6379
    },
    "Cookie": {
      "Domain": null,
      "Secure": true,
      "SameSite": "Lax",
      "HttpOnly": true
    }
  },
  "PresenceTracking": {
    "Type": "Redis"
  },
  "Licensing": {
    "EncryptionKey": "REPLACE_WITH_BASE64_ENCODED_32_BYTE_KEY",
    "CloudEndpoint": "https://licensing.example.com",
    "BearerToken": "REPLACE_WITH_BEARER_TOKEN",
    "CacheExpirationHours": 24,
    "GracePeriodDays": 7,
    "DemoExpirationDays": 30,
    "WarningThresholdDays": 3
  },
  "BackgroundServices": {
    "PickListSync": {
      "IntervalSeconds": 60,
      "Enabled": true,
      "CheckClosedPickLists": true,
      "ProcessPackageMovements": true
    },
    "CloudSync": {
      "SyncIntervalMinutes": 10,
      "ValidationIntervalHours": 24,
      "Enabled": true
    }
  },
  "Smtp": {
    "Enabled": true,
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "noreply@example.com",
    "Password": "YOUR_SMTP_PASSWORD",
    "FromEmail": "noreply@example.com",
    "FromName": "EZY WMS",
    "TimeoutSeconds": 30,
    "TimeZoneId": "America/Panama"
  },
  "UptimeKuma": {
    "Enabled": true,
    "PushUrl": "https://uptime.example.com/api/push/REPLACE_ME",
    "HeartbeatIntervalSeconds": 60,
    "SendOnStartup": true,
    "SendOnShutdown": true
  }
}
```
