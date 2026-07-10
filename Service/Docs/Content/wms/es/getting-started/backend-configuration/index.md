---
title: Configuración de Backend
description: Descripción general de los archivos de configuración del backend de EZY WMS utilizados durante la instalación.
---

> **Nota para la versión actual:** La configuración activa se guarda en la base de datos y se administra desde Configuración para superusuarios. Los archivos YAML en `Service/init` son plantillas iniciales; no son la fuente de configuración en tiempo de ejecución.

El backend de EZY WMS (`ezy-wms-backend/Service`) se configura mediante dos mecanismos:

1. **`appsettings.json`** — El archivo de configuración principal de .NET que controla la configuración del host, registro, conexiones de bases de datos, secretos JWT, gestión de sesiones, licencias, SMTP y parámetros de servicios de fondo.

2. **Carpeta `Service/config/`** — Un conjunto de archivos YAML que definen el comportamiento empresarial, el manejo de datos y el área de superficie de la interfaz de usuario. Estos archivos se recargan al inicio del servicio.

## Archivos de Configuración

| Archivo | Propósito |
|---------|-----------|
| [`appsettings.json`](/docs/getting-started/backend-configuration/appsettings) | Host de .NET, registro, base de datos, JWT, sesiones, licencias, SMTP, servicios de fondo |
| [`config/Configurations.yaml`](/docs/getting-started/backend-configuration/core-system) | **Obligatorio.** Opciones empresariales principales, filtros de datos, asignaciones de almacenes, detalles de conexión SAP B1 |
| [`config/CustomFields.yaml`](/docs/getting-started/backend-configuration/custom-fields) | Campos adicionales de artículos y documentos expuestos en la interfaz de usuario |
| [`config/Item.yaml`](/docs/getting-started/backend-configuration/item-metadata) | Definiciones de metadatos de artículos editables |
| [`config/Package.yaml`](/docs/getting-started/backend-configuration/package) | Configuración de etiqueta de paquete, código de barras y metadatos |
| [`config/ExternalCommands.yaml`](/docs/getting-started/backend-configuration/external-commands) | Exportaciones XML/JSON activadas por eventos o botones manuales |
| [`config/PickingPostProcessing.yaml`](/docs/getting-started/backend-configuration/picking-post-processing) | Procesadores personalizados de .NET que se ejecutan después de operaciones de picking |

## Valores Predeterminados e Inicialización

La carpeta `Service/init/` contiene copias predeterminadas de todos los archivos de configuración. Estos valores predeterminados se utilizan para inicializar la carpeta `Service/config/` en la primera ejecución. Durante la implementación, coloque sus anulaciones específicas del entorno en `Service/config/` — la carpeta init no se modifica.

## Convenciones

- **Recarga al inicio:** Todos los archivos de configuración YAML se analizan cuando se inicia el servicio. Los cambios de configuración requieren un reinicio del servicio.
- **Sensibilidad a mayúsculas y minúsculas:** YAML es sensible a mayúsculas y minúsculas. Los nombres de propiedades y valores de enumeración deben coincidir exactamente con sus definiciones.
- **Sin credenciales en control de versiones:** Nunca confirme contraseñas reales de bases de datos, claves API, credenciales SMTP o inicios de sesión de SAP B1 en el repositorio. Utilice variables de entorno o prácticas de implementación segura para inyectarlas en tiempo de ejecución.
