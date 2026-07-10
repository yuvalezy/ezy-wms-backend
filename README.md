# EzyWMS backend

EzyWMS is an ASP.NET Core warehouse-management API integrated with SAP Business
One. It owns WMS workflow state in SQL Server, reads SAP data through a direct SQL
adapter, and performs SAP document operations through the SAP Business One Service
Layer.

## Documentation

- [Backend docs index](docs/README.md)
- [Architecture](docs/architecture.md)
- [Functionality](docs/functionality.md)
- [Frontend/backend contract](docs/frontend-backend-contract.md)
- [Embedded documentation API](docs/documentation.md)
- [Postman collections](docs/postman-collections/README.md)

Historical plans and implementation summaries live under `docs/plans/`,
`docs/features/`, and `docs/summaries/`; consult the living architecture docs first.

## Requirements

- .NET SDK specified by `global.json` (currently the solution targets `net10.0`)
- SQL Server for WMS persistence
- SAP Business One Service Layer for SAP document operations
- Access to the SAP Business One SQL Server database for direct read adapters

The active adapter implementation is SQL Server-based. SAP HANA, Redis as a
mandatory dependency, and the old `Service.Administration`/`Service.Shared`
solution layout described by older documentation are not current requirements.

## Run locally

```bash
dotnet restore
dotnet build
cd Service
dotnet run
```

The default service URL is `http://localhost:5000`; Swagger is at `/swagger`.
Copy/adjust `Service/appsettings.example.json` for local infrastructure settings,
and keep credentials out of source control. A fresh or unconfigured instance can
start in SAP lockdown mode so that the setup/configuration endpoints remain usable.

## Test

```bash
dotnet test UnitTests
```

`UnitTests/Unit` is the fast, external-dependency-free suite. Tests under
`UnitTests/Integration` need a live SAP/SQL environment. The root `Tests/` directory
is a legacy orphan and is not part of the solution or CI.

## Development boundaries

- Put business logic in `Infrastructure/Services`, not controllers.
- Use EF Core for WMS-owned tables and migrations for schema changes.
- Use `Adapters.CrossPlatform` for SAP Service Layer operations.
- Use `Adapters.Common` for read-heavy SAP SQL queries.
- Treat database-backed configuration as the current runtime configuration source;
  the YAML files under `Service/init` are seed/migration templates.
- Preserve decimal quantities, alphanumeric bin ordering, enum numeric values,
  authentication behavior, and the frontend/backend contract.
