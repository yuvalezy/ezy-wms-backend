# EzyWMS backend documentation

These are living documents for the backend in this repository. They describe the
implemented system as checked against the source on 2026-07-10. Swagger remains
the executable API reference when the service is running.

## Start here

- [Architecture](architecture.md) — projects, runtime request flow, data ownership,
  configuration, authentication, and background work.
- [Functionality](functionality.md) — warehouse workflows and the backend/frontend
  surfaces that implement them.
- [Frontend/backend contract](frontend-backend-contract.md) — integration rules,
  authentication, route families, feature gates, and failure behavior.
- [Embedded documentation API](documentation.md) — authenticated WMS guide
  endpoints, corpus lifecycle, and visibility rules.
- [Postman collection](postman-collections/README.md) — generated API client assets.

Historical plans and feature summaries remain under `docs/plans/`, `docs/features/`,
and `docs/summaries/`. They are useful for change history, but they are not the
current architecture reference unless explicitly linked from the living docs.

## Local development

```bash
dotnet restore
dotnet build
cd Service
dotnet run
```

The default development URL is `http://localhost:5000`; Swagger is available at
`/swagger`. Run unit tests from the repository root with:

```bash
dotnet test UnitTests
```

Integration tests under `UnitTests/Integration` require a live SAP Business One /
SQL Server environment and are not equivalent to the unit-test command.

## Source-of-truth rules

- HTTP routes and authorization attributes: `Service/Controllers/`.
- Business behavior: `Infrastructure/Services/` and the interfaces in
  `Core/Interfaces/`.
- WMS persistence: `Infrastructure/DbContexts/SystemDbContext.cs`, entity
  configurations, and EF migrations.
- SAP integration: `Adapters.CrossPlatform/` for Service Layer writes and
  `Adapters.Common/` for direct SQL reads.
- Runtime configuration: the database-backed configuration provider and
  `Core/Configuration/ConfigSectionCatalog.cs`; `appsettings.json` is reserved
  for infrastructure settings described in [Architecture](architecture.md).
