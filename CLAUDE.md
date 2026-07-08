# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

See also `/mnt/dev/wms/CLAUDE.md` for the cross-repo workspace overview (this backend + the `ezy-wms-frontend` sibling repo).

## Commands

```bash
dotnet build
cd Service && dotnet run                 # http://localhost:5000, Swagger at /swagger
dotnet test UnitTests                    # NUnit; run this before pushing
dotnet test UnitTests --filter "FullyQualifiedName~UnitTests.Unit"          # what CI runs (fast, no external deps)
dotnet test UnitTests --filter "FullyQualifiedName~ClassName.MethodName"    # single test
dotnet ef migrations add <Name> --project Infrastructure --startup-project Service
```

- `UnitTests/Unit/` — pure logic, no external deps, runs in CI.
- `UnitTests/Integration/` — hits a live SAP/SQL environment (`BaseExternalTest.cs`); not run in CI.
- `Tests/` at the repo root is a legacy xUnit/Moq project with **no `.csproj`** — it isn't in `ezy-wms-backend.sln` and isn't built or run anywhere. Don't add to it; add to `UnitTests/`.
- Test naming convention: `[TestFixture]` classes, `[Test]` methods named `Method_Condition_ExpectedResult`.
- Targets plain `net10.0` (see `global.json`), no Windows/COM dependency. CI (`.github/workflows/release.yml`, tag-triggered) publishes self-contained builds for both `win-x64` and `linux-x64`.

## Architecture

### Solution structure

7 projects in `ezy-wms-backend.sln` (`Tests/` is intentionally excluded, see above):

- **`Core`** — pure domain layer: `Entities/`, `DTOs/` (per module: GoodsReceipt, InventoryCounting, PickList, Transfer, DirectTransfer, Configuration...), `Interfaces/`, `Enums/`, `Configuration/ConfigSectionCatalog.cs`. No infrastructure dependencies.
- **`Infrastructure`** — EF Core: `DbContexts/SystemDbContext.cs` + one Fluent config class per entity, `Migrations/`, `Repositories/`, ~45 `Services/*Service.cs` classes (this is where business logic actually lives — `PickListService.cs`, `TransferProcessingService.cs`, `InventoryCountingsService.cs`, `ConfigurationManagementService.cs`, etc.), `Auth/`, `Hubs/` (SignalR), `SessionManager/`.
- **`Adapters.CrossPlatform`** — SAP Business One **Service Layer** HTTP client (`SBO/Services/SboCompany.cs`, `SboServiceLayerAdapter.cs`, `SboConnectionTester.cs`) plus SAP document-creation helpers (`CountingCreation.cs`, `GoodsReceiptCreation.cs`, `TransferCreation.cs`, `PickingUpdate.cs`). Despite the name pairing with `Adapters.Common`, this is the one with no Windows dependency.
- **`Adapters.Common`** — direct SQL access to SAP's **underlying SQL Server database** via raw ADO.NET (`Microsoft.Data.SqlClient`): `SboDatabaseService.cs`, `SboGeneralRepository.cs`, `SqlQueryUtils.cs`, plus `SBO/Repositories/*` (Employee, GoodsReceipt, Picking, Item, InventoryCounting, General). Used for fast read-heavy queries against SAP data that the Service Layer would be too slow for.
- **`Customer.Extensions`** — per-customer override hooks; references Core/Infrastructure/Adapters.
- **`Service`** — ASP.NET Core Web API host (`Sdk.Web`). Flat `Controllers/` (one per module — `CountingController`, `GoodsReceiptController`, `PickingController`, `TransferController`, `DirectTransferController`, `ItemsController`, `DeviceController`, `UserController`, `AuthenticationController`, `AuthorizationGroupController`, `ConfigurationController`, `SystemController`, `LicenseController`, `HealthController`, etc.), `Middlewares/` (including `SystemLockdownMiddleware`), `Startup/`, `Configuration/`, background `Services/` (Cloud sync, Uptime Kuma, PickList sync).
- **`UnitTests`** — the real, solution-registered test project (NUnit).

Controllers are thin — they inject an `I*Service` interface from `Infrastructure/Services` and delegate to it. There is **no** per-module `Controller/Data/Creation/Models/Queries/SQL` folder structure and **no** embedded raw-SQL query files for WMS's own data (that pattern is retired; `Service.csproj` still has a stale, no-op `EmbeddedResource` glob pointing at a `Queries/SQL` folder that no longer exists — safe to ignore or clean up if you're in that file).

### SAP Business One integration

Two distinct paths, don't confuse them:

1. **Service Layer REST** (`Adapters.CrossPlatform`) — writes and document creation. `SboCompany.cs` is a plain `HttpClient`-based session client with a deliberately lazy config read so the singleton can be constructed even before SAP is configured (lockdown mode).
2. **Direct SQL Server reads** (`Adapters.Common`) — SAP B1 on-premise stores its data in SQL Server (not SAP HANA in this deployment); `Adapters.Common` queries that database directly with raw SQL for performance on read-heavy endpoints.

### Database & migrations

- WMS's own data lives in SQL Server via EF Core, `Infrastructure/DbContexts/SystemDbContext.cs`. Migrations are in `Infrastructure/Migrations/`.
- Create a migration with `dotnet ef migrations add <Name> --project Infrastructure --startup-project Service`; it's applied via `Database.Migrate()` at startup.
- Raw SQL is used **only** for SAP's external database via `Adapters.Common` — never write raw SQL against WMS's own EF-managed tables.

### Configuration system

Two tiers:
- **`appsettings.json`** — infra-only: connection strings, JWT, Smtp, licensing, background service intervals, session management. See `Core/Configuration/ConfigSectionCatalog.cs` for the authoritative list of what stays here.
- **Database-backed** (everything else: `Options`, `Filters`, `Warehouses`, `CustomFields`, `Item`, `PickingDetails`, `ExternalCommands`, `SboSettings`, `PickingPostProcessingProcessors`) — stored one row per section in `SystemConfigurationEntry` (`Core/Entities/SystemConfigurationEntry.cs`): PK `Section` (PascalCase string), `Json` value (secrets marked `ENC::`), `Version`, `RowVersion`. Managed through `Infrastructure/Services/ConfigurationManagementService.cs`. Fresh installs seed from YAML templates in `Service/init/*.yaml`; changes are audited via `Core/Entities/SystemConfigurationAudit.cs`.
- The historical YAML-file config system (`config/*.yaml`, live per-install files) has been fully migrated into the database — see `/mnt/dev/wms/plan/executed/` for the design rationale if you need the "why," but treat the DB-backed model as current truth. Don't reintroduce file-based config for new sections.

### Lockdown mode

`Service/Middlewares/SystemLockdownMiddleware.cs` returns 503 for business endpoints while `ISystemStatusService.Current.Ready` is false (i.e. SAP isn't configured yet), while allowing `/api/system`, `/api/health`, `/api/authentication`, `/api/configuration`, and `/swagger` through — this is what lets the frontend's first-run setup flow work before SAP credentials exist.

### Authentication

- Password-only login (`POST /api/authentication/login`), no username.
- JWT + HTTP-only cookie (`ezywms_session`) hybrid: cookie for browser sessions, Bearer token also accepted for API clients.
- Session info cached in memory (`ISessionManager`); tokens expire at midnight UTC.
- Passwords hashed with PBKDF2-SHA256, 32-byte salt, 10,000 iterations.

## Coding guidelines

- Primary constructors; file-scoped `namespace Foo;` declarations, not block-scoped.
- Follow SOLID; keep files under ~500 lines (refactor if larger).
- Never change existing enum numeric values.
- Prefix placeholder comments with `TODO:`.
- Business logic belongs in `Infrastructure/Services`, not in controllers. This is inventory-critical code — prefer whole-bug-class fixes over narrow patches, especially around quantity/decimal logic.
