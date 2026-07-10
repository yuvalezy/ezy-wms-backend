# Backend architecture

Last verified against source: 2026-07-10.

## System boundary

EzyWMS is the operational system for warehouse workflows, while SAP Business One
is the external ERP and inventory/document system. The backend owns WMS workflow
state and coordinates SAP reads and writes.

```text
React SPA / API clients
        |
        v
ASP.NET Core Service
  middleware -> controllers -> Infrastructure services
                                  |                 |
                                  v                 v
                         WMS SQL Server       SAP Business One
                         (EF Core)             Service Layer (writes)
                                                    |
                                                    +-- SAP SQL Server (reads)

SignalR notifications and static SPA assets are served by the same host.
License/cloud synchronization and pick-list synchronization run as hosted workers.
```

The backend's SAP database read path is SQL Server-specific. The code contains no
active SAP HANA implementation; do not describe HANA as a supported deployment
target without adding and validating a separate adapter.

## Solution structure

| Project | Responsibility | Important boundary |
| --- | --- | --- |
| `Core` | Entities, DTOs, enums, interfaces, configuration metadata, domain helpers | No database or HTTP implementation |
| `Infrastructure` | EF Core persistence, repositories, business services, authentication/session support, SignalR hub | Implements most application behavior |
| `Adapters.CrossPlatform` | SAP Business One Service Layer `HttpClient` client and document helpers | Primarily SAP writes and document creation |
| `Adapters.Common` | Raw ADO.NET repositories against SAP's SQL Server database | Read-heavy SAP queries; not WMS-table persistence |
| `Customer.Extensions` | Customer-specific extension hooks | Loaded through the extension boundary; treat assemblies/config as trusted |
| `Service` | ASP.NET Core host, controllers, middleware, startup, Swagger, static SPA hosting, workers | HTTP/process boundary |
| `UnitTests` | NUnit test project registered in the solution | `UnitTests/Integration` needs external systems |

Controllers are deliberately thin: they validate/request-bind and delegate to
interfaces implemented in `Infrastructure/Services`. New workflow behavior
belongs in the service layer, not in controllers.

## Request pipeline and readiness

`Service/Program.cs` builds configuration and dependency injection, then the host
configures routing, authentication, database migration, static files, controllers,
SignalR, and SPA fallback. The request middleware includes logging, exception
translation, token/session handling, license validation, and system readiness
enforcement.

On startup, the service:

1. Loads optional legacy YAML files and then overlays database-backed configuration.
2. Applies EF migrations and initializes/migrates configuration into the database.
3. Checks SAP readiness through `ISystemStatusService`.
4. Starts in lockdown if SAP is not ready; the process still serves setup and health
   surfaces.

In lockdown, `SystemLockdownMiddleware` returns `503` for business endpoints while
allowing system/health/authentication/configuration and Swagger paths needed for
first-run setup. A frontend must treat this as a setup state, not as an ordinary
business validation error. See [Frontend/backend contract](frontend-backend-contract.md).

The same host maps:

- `/api/*` controller routes;
- `/hubs/notifications` for authenticated SignalR notifications;
- `/swagger` for the OpenAPI UI;
- `/api/docs` for the authenticated WMS documentation index, search, and article
  endpoints;
- static files and `index.html` fallback for the SPA.

## Data ownership

### WMS database

`SystemDbContext` stores users, authorization groups, sessions, devices, alerts,
license/cache state, approval workflows, configuration, and workflow objects such
as goods receipts, inventory countings, transfers, pick lists, package labels, and
repack/check sessions. EF migrations are the schema change mechanism. Base entities
use a soft-delete query filter and audit relationships where configured.

Raw SQL must not be introduced for WMS-owned tables; use EF Core and the existing
repositories/services.

### SAP Business One

The two SAP paths are intentionally different:

1. `Adapters.CrossPlatform` uses the SAP Service Layer REST API for document
   creation/update/cancellation and other write-side operations.
2. `Adapters.Common` connects directly to SAP's SQL Server database for read-heavy
   item, warehouse, bin, vendor, source-document, and operational lookups.

Do not substitute one path for the other without checking consistency, transaction,
and failure semantics. WMS workflow state can be persisted while an SAP operation
is pending or fails; services and background workers own the retry/reconciliation
behavior for the specific workflow.

## Configuration

There are two configuration tiers.

### Database-backed sections

The configuration provider stores one JSON document per section in
`SystemConfigurationEntry`, keyed by PascalCase section name. Current catalogued
sections are:

`Options`, `Filters`, `Warehouses`, `CustomFields`, `Item`, `PickingDetails`,
`ExternalCommands`, `SboSettings`, and `PickingPostProcessingProcessors`.

The API supports validation, optimistic version checks, history, restore, import,
and export. Secret leaves are encrypted at rest with the `ENC::` marker and are
masked at the API boundary. `PickingPostProcessingProcessors` is marked as requiring
a restart; the other catalogued sections are live-reloadable according to the
catalog metadata.

The YAML files under `Service/init/` are seed/templates and migration inputs, not
the preferred live configuration store. `appsettings.json` takes precedence only
for infrastructure settings that are intentionally outside the database section
catalog.

### Infrastructure settings

Connection strings, JWT settings, session-management choice, cookie settings,
licensing/cloud settings, SMTP, presence tracking, background worker intervals,
logging, CORS, and Kestrel remain in `appsettings*.json` or environment-specific
configuration. Keep secrets out of committed example files.

## Authentication and authorization

- Login is password-only through `POST /api/authentication/login`.
- Browser sessions use the HTTP-only `ezywms_session` cookie; API clients may use
  the returned bearer token.
- Passwords are hashed with PBKDF2-SHA256 in the authentication service.
- Session storage is selected by `SessionManagement:Type` and can be in-memory,
  Redis, or SQL through the registered `ISessionManager` implementation.
- Controllers generally require `[Authorize]`; specific actions additionally
  enforce super-user or role permissions through the authorization attributes and
  service checks.

The frontend's role names and route guards must remain aligned with
`Core/Enums/RoleType.cs`; the integration table documents the current mapping.

## Background and real-time work

- `BackgroundPickListSyncService` periodically synchronizes pending pick lists;
  it skips overlapping syncs.
- `CloudSyncBackgroundService` processes cloud-license queue work and periodic
  account/license validation.
- `UptimeKumaHeartbeatService` emits optional health heartbeats.
- `NotificationHub` provides authenticated SignalR connections at
  `/hubs/notifications` for alerts/presence-related UI updates.

Intervals and enablement are infrastructure configuration. A deployment with
multiple instances must choose session/presence and worker behavior deliberately;
in-memory state is process-local.

## Operational invariants

- Quantities are decimal values; preserve precision and use the existing tolerance
  rules for zero/completeness decisions.
- Bin codes are alphanumeric and must use natural/full-code ordering where an order
  is user-visible or affects picking.
- A failed or unavailable SAP dependency is a normal operational state that must be
  surfaced as a controlled API error or readiness failure, not a process crash.
- Configuration imports and updates must honor section validation, secret masking,
  restricted assembly/type handling, and optimistic concurrency.
