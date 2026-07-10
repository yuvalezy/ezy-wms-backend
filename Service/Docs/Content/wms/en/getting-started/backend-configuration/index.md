---
title: Backend Configuration
description: Overview of EZY WMS backend configuration files used at installation time.
---

> **Current WMS note:** Live configuration is stored in the database and managed from the superuser Configuration screen. YAML files under `Service/init` are seed templates, not the runtime configuration source.

The EZY WMS backend (`ezy-wms-backend/Service`) is configured through two mechanisms:

1. **`appsettings.json`** — The main .NET configuration file controlling host settings, logging, database connections, JWT secrets, session management, licensing, SMTP, and background service parameters.

2. **`Service/config/` folder** — A set of YAML files that define business behavior, data handling, and UI surface area. These files are reloaded at service startup.

## Configuration Files

| File | Purpose |
|------|---------|
| [`appsettings.json`](/docs/getting-started/backend-configuration/appsettings) | .NET host, logging, database, JWT, sessions, licensing, SMTP, background services |
| [`config/Configurations.yaml`](/docs/getting-started/backend-configuration/core-system) | **Mandatory.** Core business options, data filters, warehouse mappings, SAP B1 connection details |
| [`config/CustomFields.yaml`](/docs/getting-started/backend-configuration/custom-fields) | Extra item and document fields exposed in the UI |
| [`config/Item.yaml`](/docs/getting-started/backend-configuration/item-metadata) | Editable item metadata definitions |
| [`config/Package.yaml`](/docs/getting-started/backend-configuration/package) | Package label, barcode, and metadata configuration |
| [`config/ExternalCommands.yaml`](/docs/getting-started/backend-configuration/external-commands) | XML/JSON exports triggered by events or manual buttons |
| [`config/PickingPostProcessing.yaml`](/docs/getting-started/backend-configuration/picking-post-processing) | Custom .NET processors that run after picking operations |

## Defaults and Initialization

The `Service/init/` folder contains default copies of all configuration files. These defaults are used to seed the `Service/config/` folder on first run. During deployment, place your environment-specific overrides in `Service/config/` — the init folder is not modified.

## Conventions

- **Reload on startup:** All YAML configuration files are parsed when the service starts. Configuration changes require a service restart.
- **Case sensitivity:** YAML is case-sensitive. Property names and enum values must match their definitions exactly.
- **No credentials in version control:** Never commit real database passwords, API keys, SMTP credentials, or SAP B1 logons to the repository. Use environment variables or secure deployment practices to inject them at runtime.
