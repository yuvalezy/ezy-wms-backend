---
title: Post-Procesamiento de Picking (PickingPostProcessing.yaml)
description: Referencia del archivo PickingPostProcessing.yaml de EZY WMS que registra procesadores post-picking implementados en ensamblados de extensiones de cliente.
---

> **Nota para la versión actual:** La configuración activa se guarda en la base de datos y se administra desde Configuración para superusuarios. Los archivos YAML en `Service/init` son plantillas iniciales; no son la fuente de configuración en tiempo de ejecución.

## Descripción General

La configuración de post-procesamiento de picking define procesadores .NET personalizados que se ejecutan automáticamente después de completar operaciones de picking. Estos procesadores permiten lógica de negocio como integración SAP, cálculos de empaque, ajustes de inventario y notificaciones a sistemas externos.

**Ubicación del archivo de configuración:**
```
ezy-wms-backend/Service/config/PickingPostProcessing.yaml
```

Alternativamente, los procesadores se pueden configurar en el archivo `appsettings.json` de extensiones de cliente bajo el array `PickingPostProcessing.Processors` (ver Notas de Implementación más abajo).

## Estructura de Configuración

La configuración define un array de nivel superior `PickingPostProcessingProcessors`. Cada entrada registra un post-procesador.

### Campos del Procesador

| Campo | Tipo | Requerido | Por Defecto | Descripción |
|-------|------|-----------|-------------|-------------|
| `Id` | string | Sí | — | Identificador único para este procesador. Debe ser único entre todos los post-procesadores. |
| `Assembly` | string | Sí | — | Nombre de archivo del ensamblado .NET (p. ej., `Customer.Extensions.dll`) que contiene la implementación del procesador. |
| `TypeName` | string | Sí | — | Nombre de tipo .NET completo que implementa la interfaz del post-procesador (p. ej., `Customer.Extensions.PackagingCalculatorPostProcessor`). |
| `Enabled` | bool | No | `true` | Cuando es `false`, este procesador se omite durante la ejecución. |
| `Configuration` | object | No | — | Diccionario libre de configuraciones específicas del procesador pasadas en tiempo de ejecución. |

### Claves del Diccionario de Configuración

La sección `Configuration` contiene claves definidas por el procesador. Las claves comunes incluyen:

| Clave | Tipo | Ejemplo | Notas |
|-------|------|---------|-------|
| `Enabled` | bool | `true` | Lógica del procesador habilitada/deshabilitada. |
| `Description` | string | `"Calcula requisitos de empaque"` | Describe el propósito del procesador. |
| `BatchSize` | int | `100` | Número de elementos por lote. |
| `TimeoutSeconds` | int | `30` | Tiempo de espera de ejecución del procesador. |
| `RetryAttempts` | int | `3` | Número de reintentos en caso de fallo. |
| `LogLevel` | string | `Information` | Verbosidad del registro: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`. |
| `ConnectionString` | string | Ver ejemplo | Conexión a base de datos (específica del procesador). |
| `ApiEndpoint` | string | Ver ejemplo | URL de endpoint de API externa. |
| `ApiKey` | string | Ver ejemplo | Clave de autenticación de API. |
| `ProcessingMode` | string | `Synchronous` o `Asynchronous` | Modo de ejecución. |
| `MaxConcurrency` | int | `5` | Número máximo de operaciones concurrentes. |
| `Recipients` | array | `["admin@example.com"]` | Destinatarios de notificación (depende de la integración). |

**Importante:** Las claves de configuración y sus valores son específicos del procesador. Consulta la documentación del procesador para obtener la lista de configuraciones disponibles.

## Ejemplos de Configuración

### Calculador Básico de Empaque

```yaml
PickingPostProcessingProcessors:
  - Id: customer-packaging-calculator
    Assembly: Customer.Extensions.dll
    TypeName: Customer.Extensions.PackagingCalculatorPostProcessor
    Enabled: true
    Configuration:
      Enabled: true
      Description: Calcula requisitos de empaque y actualiza líneas de pedido SAP
      BatchSize: 100
      TimeoutSeconds: 30
      RetryAttempts: 3
      LogLevel: Information
```

### Procesador de Integración SAP (con configuración externa)

```yaml
  - Id: sap-integration-processor
    Assembly: Customer.Extensions.dll
    TypeName: Customer.Extensions.SapIntegrationPostProcessor
    Enabled: true
    Configuration:
      Enabled: true
      Description: Envía eventos de finalización de picking a SAP
      ConnectionString: "Server=your-sap-server;SystemId=YOUR_SYSTEM_ID;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
      ApiEndpoint: "https://api.example.com/sap-webhook"
      ApiKey: "YOUR_API_KEY"
      ProcessingMode: Asynchronous
      MaxConcurrency: 5
      TimeoutSeconds: 60
      RetryAttempts: 5
      LogLevel: Information
      Recipients:
        - admin@example.com
        - warehouse@example.com
```

## Notas de Implementación

1. **Implementación del Ensamblado:** El ensamblado del procesador (archivo `.dll`) debe implementarse junto con el ejecutable del servicio en el mismo directorio o en las rutas de búsqueda de ensamblados configuradas.

2. **Contrato de Tipo:** El `TypeName` debe hacer referencia a una clase que implemente la interfaz del post-procesador de picking de EZY WMS. Consulta el proyecto `Customer.Extensions` para obtener el contrato de interfaz y ejemplos de implementación.

3. **Esquema de Configuración:** Cada procesador define su propio esquema de configuración. Las claves de configuración son específicas del procesador — consulta la documentación del procesador para obtener las claves válidas y los tipos de valores esperados.

4. **Comportamiento en Tiempo de Ejecución:**
   - Los procesadores se ejecutan secuencialmente en el orden definido en la configuración.
   - Si `Enabled` es `false`, el procesador se omite.
   - Si un procesador lanza una excepción no capturada, la operación de picking se marca como completada, pero el error se registra. Los procesadores posteriores siguen ejecutándose.

5. **Registro:** Establece `LogLevel` apropiadamente para solucionar problemas. Usa `Debug` o `Trace` para desarrollo; `Information` o superior para producción.

6. **Configuración JSON (appsettings):** Los procesadores también se pueden configurar en un archivo `appsettings.json` de extensión de cliente bajo:
   ```json
   {
     "PickingPostProcessing": {
       "Processors": [ /* array de objetos procesador */ ]
     }
   }
   ```
   La estructura JSON refleja el formato YAML.

## Ejemplo Completo Anotado

```yaml
# Configuración de Post-Procesamiento de Picking
# Define post-procesadores que se ejecutan después de completar operaciones de picking
PickingPostProcessingProcessors:

  # Procesador de Calculador de Empaque
  - Id: customer-packaging-calculator
    Assembly: Customer.Extensions.dll
    TypeName: Customer.Extensions.PackagingCalculatorPostProcessor
    Enabled: true
    Configuration:
      Enabled: true
      Description: Calcula requisitos de empaque y actualiza líneas de pedido SAP
      BatchSize: 100
      TimeoutSeconds: 30
      RetryAttempts: 3
      LogLevel: Information
      # Claves opcionales (específicas del procesador; incluye si el procesador las soporta):
      # ConnectionString: "Server=your-db;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
      # ApiEndpoint: "https://api.example.com/webhook"
      # ApiKey: "YOUR_API_KEY"

  # Procesador de Integración SAP (ejemplo comentado)
  # - Id: sap-integration-processor
  #   Assembly: Customer.Extensions.dll
  #   TypeName: Customer.Extensions.SapIntegrationPostProcessor
  #   Enabled: false
  #   Configuration:
  #     Enabled: true
  #     Description: Envía eventos de picking a SAP
  #     ConnectionString: "Server=your-sap-server;SystemId=YOUR_SYSTEM;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
  #     ProcessingMode: Asynchronous
  #     MaxConcurrency: 5
  #     TimeoutSeconds: 60
  #     RetryAttempts: 5
  #     LogLevel: Information
  #     Recipients:
  #       - admin@example.com
  #       - warehouse@example.com

  # Procesador de Ajuste de Inventario (ejemplo comentado)
  # - Id: inventory-adjustment-processor
  #   Assembly: Warehouse.Extensions.dll
  #   TypeName: Warehouse.Extensions.InventoryAdjustmentProcessor
  #   Enabled: true
  #   Configuration:
  #     AdjustmentReason: "Picking Completion"
  #     RequireApproval: false
  #     ThresholdAmount: 1000.00
```
