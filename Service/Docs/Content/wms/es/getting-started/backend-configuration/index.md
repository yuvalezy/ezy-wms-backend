---
title: Configuración del Backend
description: Fuentes de configuración del backend de EZY WMS, migración y comportamiento de recarga.
---

EZY WMS tiene dos niveles de configuración:

1. **`appsettings.json` y la configuración del entorno** — parámetros de infraestructura como la conexión de SQL Server de WMS, JWT y cookies, almacenamiento de sesiones, licencias, SMTP, registros, CORS, Kestrel y servicios en segundo plano.
2. **Secciones de WMS almacenadas en la base de datos** — opciones de negocio, filtros, almacenes, campos personalizados, metadatos de artículos, comandos externos, configuración de SAP, detalles de picking y procesadores posteriores al picking.

## Ciclo de vida de la configuración

La base de datos es la fuente autorizada en tiempo de ejecución para las secciones de WMS. Las carpetas YAML son entradas de inicialización y migración; no son una carpeta de configuración activa que se relea continuamente.

Al iniciar, `ConfigurationInitializationExtensions` resuelve la fuente en este orden:

1. Si `Service/config/` contiene archivos YAML, el servicio los migra y verifica en la tabla `SystemConfiguration` y después archiva la carpeta en `Service/config_archive/`.
2. Si la base de datos ya contiene filas de configuración, el servicio inicia desde la base de datos sin volver a leer `Service/config/`.
3. En una instalación nueva sin filas de configuración, el servicio inicializa la base de datos usando las plantillas de `Service/init/` y verifica los valores almacenados.
4. Si no existe ninguna de esas fuentes, el inicio falla porque no hay una fuente de configuración.

Después de migrar o inicializar, use la pantalla de Configuración para superusuarios o las API de `ConfigurationController` para cambiar las secciones de WMS. No edite archivos archivados o plantillas esperando que un reinicio normal aplique el cambio.

## Archivos de configuración y plantillas

Las plantillas incluidas en `Service/init/` son:

| Plantilla | Sección de base de datos | Propósito |
|------|---------|---------|
| `Configurations.yaml` | `Options`, `Filters`, `Warehouses`, `SboSettings` | Flujos principales, filtros, asignaciones de almacenes y configuración de SAP |
| `CustomFields.yaml` | `CustomFields` | Campos personalizados expuestos por los flujos compatibles |
| `Item.yaml` | `Item` | Definiciones de metadatos de artículos |
| `PickingDetails.yaml` | `PickingDetails` | Configuración de detalles de picking |
| `ExternalCommands.yaml` | `ExternalCommands` | Generación y entrega de archivos por eventos o acciones manuales |
| `PickingPostProcessing.yaml` | `PickingPostProcessingProcessors` | Registros de procesadores personalizados posteriores al picking |

Actualmente no existe `Package.yaml` ni una sección genérica de configuración de gestión de paquetes. Las etiquetas de paquetes y el repack posterior al picking son funciones de picking configuradas en la sección `Options`; consulte [Procesamiento posterior al picking](picking-post-processing).

La carpeta `Service/config/` todavía puede entregarse para una migración única de configuración YAML heredada. No es la ubicación preferida para ediciones continuas.

## Comportamiento de recarga

El catálogo de `Core/Configuration/ConfigSectionCatalog.cs` define el contrato de recarga:

| Sección | Comportamiento en ejecución |
|---------|------------------|
| `Options`, `Filters`, `Warehouses`, `CustomFields`, `Item`, `PickingDetails`, `ExternalCommands`, `SboSettings` | Se recargan en vivo después de una actualización de configuración exitosa |
| `PickingPostProcessingProcessors` | Requiere reiniciar el servicio |

La API de Configuración valida las actualizaciones, aplica control optimista de
versiones, conserva historial, permite restaurar/importar/exportar, cifra hojas
secretas y oculta secretos en la respuesta de la API. Los parámetros de
infraestructura que no están en el catálogo permanecen en `appsettings*.json` o
en la configuración específica del entorno.

## Referencias de configuración

| Tema | Referencia |
|------|-----------|
| Opciones principales de WMS y configuración de SAP | [Sistema central](core-system) |
| Configuración del host e infraestructura | [appsettings.json](appsettings) |
| Campos personalizados | [Campos personalizados](custom-fields) |
| Metadatos de artículos | [Metadatos de artículos](item-metadata) |
| Comandos externos | [Comandos externos](external-commands) |
| Procesadores posteriores al picking | [Procesamiento posterior al picking](picking-post-processing) |

Mantenga las contraseñas de bases de datos, credenciales de SAP, secretos JWT,
credenciales SMTP y claves API fuera de los archivos versionados. Los secretos
ingresados mediante las API de configuración se cifran en reposo cuando se
configura la clave de cifrado.
