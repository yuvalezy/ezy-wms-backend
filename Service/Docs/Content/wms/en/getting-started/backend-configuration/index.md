---
title: Backend Configuration
description: Overview of EZY WMS backend configuration sources, migration, and live reload behavior.
---

EZY WMS has two configuration tiers:

1. **`appsettings.json` and environment configuration** — infrastructure settings such as the WMS SQL Server connection, JWT and cookie settings, session storage, licensing, SMTP, logging, CORS, Kestrel, and background-service settings.
2. **Database-backed WMS sections** — business options, filters, warehouses, custom fields, item metadata, external commands, SAP settings, picking details, and picking post-processors.

## Configuration lifecycle

The database is the authoritative runtime source for the WMS sections. The YAML directories are initialization and migration inputs, not a continuously live configuration folder.

At startup, `ConfigurationInitializationExtensions` resolves the source in this order:

1. If `Service/config/` contains YAML files, the service migrates and verifies them into the `SystemConfiguration` database table, then archives the folder under `Service/config_archive/`.
2. If the database already contains configuration rows, the service boots from the database without rereading `Service/config/`.
3. On a fresh installation with no database rows, the service seeds the database from the templates in `Service/init/` and verifies the stored values.
4. If none of those sources exists, startup fails because there is no configuration source.

After migration or seeding, use the super-user Configuration screen or
`ConfigurationController` APIs to change WMS sections. Do not edit archived or
seed YAML files expecting an ordinary restart to apply the change.

## Configuration files and templates

The templates shipped in `Service/init/` are:

| Template | Database section | Purpose |
|------|---------|---------|
| `Configurations.yaml` | `Options`, `Filters`, `Warehouses`, `SboSettings` | Core workflow, filters, warehouse mappings, and SAP settings |
| `CustomFields.yaml` | `CustomFields` | Custom fields exposed by supported workflows |
| `Item.yaml` | `Item` | Item metadata definitions |
| `PickingDetails.yaml` | `PickingDetails` | Picking-detail configuration |
| `ExternalCommands.yaml` | `ExternalCommands` | Event or manual file generation and delivery |
| `PickingPostProcessing.yaml` | `PickingPostProcessingProcessors` | Customer post-pick processor registrations |

There is no current `Package.yaml` or generic package-management configuration
section. Package labels and post-pick repack are picking features configured in
the `Options` section; see [Picking Post-Processing](picking-post-processing).

The `Service/config/` folder may still be supplied for a one-time migration of
legacy YAML configuration. It is not the preferred location for ongoing edits.

## Reload behavior

The catalog in `Core/Configuration/ConfigSectionCatalog.cs` defines the reload
contract:

| Section | Runtime behavior |
|---------|------------------|
| `Options`, `Filters`, `Warehouses`, `CustomFields`, `Item`, `PickingDetails`, `ExternalCommands`, `SboSettings` | Live-reloadable after a successful configuration update |
| `PickingPostProcessingProcessors` | Requires a service restart |

The Configuration API validates updates, applies optimistic version checks, keeps
history, supports restore/import/export, encrypts secret leaves, and masks secrets
at the API boundary. Infrastructure settings that are not in the catalog remain
in `appsettings*.json` or environment-specific configuration.

## Configuration references

| Topic | Reference |
|------|-----------|
| Core WMS options and SAP settings | [Core System](core-system) |
| Host and infrastructure settings | [appsettings.json](appsettings) |
| Custom fields | [Custom Fields](custom-fields) |
| Item metadata | [Item Metadata](item-metadata) |
| External commands | [External Commands](external-commands) |
| Picking post-processors | [Picking Post-Processing](picking-post-processing) |

Keep database passwords, SAP credentials, JWT secrets, SMTP credentials, and API
keys out of committed files. Secrets entered through configuration APIs are
encrypted at rest when the encryption key is configured.
