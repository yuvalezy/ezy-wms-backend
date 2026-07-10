---
title: Sistema Principal (Configurations.yaml)
description: Referencia del archivo Configurations.yaml de EZY WMS que cubre opciones de negocio, filtros de datos, mapeos de almacén y la conexión con SAP B1.
---

> **Nota para la versión actual:** La configuración activa se guarda en la base de datos y se administra desde Configuración para superusuarios. Los archivos YAML en `Service/init` son plantillas iniciales; no son la fuente de configuración en tiempo de ejecución.

El archivo `Configurations.yaml` es un archivo de configuración del sistema central obligatorio ubicado en `ezy-wms-backend/Service/config/Configurations.yaml`. Controla flujos de trabajo de WMS esenciales, integración con SAP Business One e configuraciones operacionales específicas del almacén.

## Options

Configuraciones de operación de negocio que controlan la funcionalidad central del WMS y el comportamiento del flujo de trabajo.

### Configuración General

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|----------------|-------------|
| `EnableUseBaseUn` | bool | `true` | Habilitar funcionalidad de conversión de unidad base |
| `UnitLabel` | string (nullable) | — | Etiqueta personalizada para unidad base (p. ej., "Metro") |
| `UnitAbbr` | string (nullable) | — | Abreviatura personalizada para unidad base (p. ej., "M") |
| `DozensLabel` | string (nullable) | — | Etiqueta personalizada para unidad de docenas (p. ej., "Metro(s)") |
| `DozensAbbr` | string (nullable) | — | Abreviatura personalizada para unidad de docenas (p. ej., "M") |
| `BoxLabel` | string (nullable) | — | Etiqueta personalizada para unidad de caja/paquete |
| `BoxAbbr` | string (nullable) | — | Abreviatura personalizada para unidad de caja/paquete |
| `MaxUnitLevel` | enum | — | Nivel máximo de unidad en formato de stock: `Unit` (desglose completo), `Dozen` (detener en nivel de docenas), `Pack` (detener en nivel de paquete) |
| `ScannerMode` | enum | `ItemBarcode` | Modo de entrada para identificación de código de barras/artículo: `ItemBarcode` (escanear códigos de barras), `ItemCode` (entrada manual de código) |
| `DisplayVendor` | bool | `true` | Mostrar información de proveedor en pantallas de interfaz |
| `WhsCodeBinSuffix` | bool | `false` | Añadir código de almacén como sufijo a códigos de contenedor |

### Recepción de Mercancías

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|----------------|-------------|
| `GoodsReceiptDraft` | bool | `false` | Crear documentos de recepción como borradores (requiere aprobación) en lugar de publicación directa |
| `GoodsReceiptModificationsRequiredSupervisor` | bool | `false` | Requiere aprobación del supervisor para modificaciones en recepciones |
| `GoodsReceiptCreateSupervisorRequired` | bool | `false` | Requiere aprobación del supervisor para crear recepciones |
| `GoodsReceiptType` | enum | `Both` | Tipo de documento a crear: `Transactional` (transacción de inventario), `Confirmation` (confirmación de documento), `Both` |
| `GoodsReceiptTargetDocuments` | bool | `false` | Habilitar selección de documento destino en recepción |
| `GoodsReceiptPackages` | bool | `true` | Permitir creación de paquetes durante la recepción |
| `GoodsReceiptConfirmationAdjustStock` | bool | `false` | Ajustar automáticamente niveles de stock al confirmar recepción |
| `GoodsReceiptConfirmationAdjustStockPriceList` | int (nullable) | — | ID de lista de precios utilizado al ajustar stock durante confirmación |

### Transferencia

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|----------------|-------------|
| `TransferTargetItems` | bool | `false` | Habilitar selección de artículos destino en transferencias |
| `EnableTransferConfirm` | bool | `false` | Requiere paso de confirmación para operaciones de transferencia |
| `EnableTransferRequest` | bool | `false` | Habilitar flujo de solicitud de transferencia de stock |
| `EnableWarehouseTransfer` | bool | `false` | Habilitar transferencias entre almacenes con flujo de aprobación |
| `TransferCreateSupervisorRequired` | bool | `false` | Requiere aprobación del supervisor para crear transferencias |
| `DirectTransferAll` | bool | `false` | Transferir 100% del stock del artículo escaneado desde contenedor origen (omite entrada de cantidad) |

### Paquete y Unidad

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|----------------|-------------|
| `EnablePackages` | bool | `false` | Habilitar funcionalidad de paquete en todo el sistema |
| `DefaultUnitType` | enum | `Pack` | Tipo de unidad predeterminado: `Unit` (individual), `Dozen` (12 unidades), `Pack` (paquete/caja) |
| `EnableUnitSelection` | bool | `false` | Permitir que los usuarios seleccionen diferentes tipos de unidad durante operaciones |

### Gestión de Sesión

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|----------------|-------------|
| `IdleLogoutTimeout` | int (nullable) | `null` | Tiempo de espera de inactividad en segundos antes de cierre automático (0 o null = sin tiempo de espera) |

### Lista de Recogida

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|----------------|-------------|
| `EnablePickingCheck` | bool | `false` | Requiere pasos de verificación para operaciones de recogida |

### Cantidad

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|----------------|-------------|
| `EnableDecimalQuantities` | bool | `false` | Permitir cantidades decimales (precisión de 2 lugares decimales) en todas las operaciones |

### Anulaciones de Unidad de Documento

Opcionalmente anule la configuración de unidad global para tipos de documento específicos. Claves soportadas: `GoodsReceipt`, `InventoryCounting`, `Transfer`, `Picking`. Cualquier propiedad no especificada vuelve al valor global de `Options`.

```yaml
DocumentUnitOverrides:
  GoodsReceipt:
    DefaultUnitType: Pack
  InventoryCounting:
    DefaultUnitType: Dozen
  Transfer:
    DefaultUnitType: Unit
```

## Filtros

Los filtros basados en SQL restringen la visibilidad de datos y el acceso en consultas.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Vendors` | string (nullable) | Cláusula SQL WHERE para filtrado de proveedores (p. ej., `"QryGroup1" = 'Y'`) |
| `PickPackOnly.Query` | string | Cláusula SQL WHERE para filtrado de clientes pick-pack |
| `PickPackOnly.GroupBy` | string | Expresión SQL GROUP BY para resultados de clientes pick-pack |

**Nota:** Los valores de filtro son cláusulas SQL sin procesar. Asegure la sintaxis adecuada para su tipo de base de datos.

## Almacenes

Configuración específica del almacén indexada por código de almacén.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `InitialCountingBinEntry` | int (nullable) | ID de entrada de contenedor para inicialización de conteo de stock |
| `CancelPickingBinEntry` | int | ID de entrada de contenedor para ubicación de devolución de recogida cancelada |
| `StagingBinEntry` | int (nullable) | ID de entrada de contenedor para área de preparación/retención temporal |

Ejemplo:

```yaml
Warehouses:
  WH01:
    InitialCountingBinEntry: 1001
    CancelPickingBinEntry: 1002
    StagingBinEntry: 1003
```

## Adaptador Externo

Especifica el adaptador de integración de SAP Business One.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `ExternalAdapter` | enum | Tipo de adaptador: `SboWindows` (COM interop), `SboServiceLayer` (REST API) |

## SboSettings

Configuración para conectarse a la base de datos de SAP Business One y servicios.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Server` | string | Nombre de host o dirección IP del servidor SAP B1 |
| `ServiceLayerUrl` | string (nullable) | URL de SAP Service Layer (requerido para `SboServiceLayer`); formato: `https://server:port` (puerto típicamente 50000) |
| `ServerType` | int | Tipo de servidor de base de datos: SQL Server (`4`, `6`, `7`, `8`, `10`, `11`, `15`), HANA (`9`). Ver tabla de mapeo ServerType. |
| `TrustedConnection` | bool | Utilizar autenticación de Windows para conexión de base de datos |
| `ServerUser` | string (nullable) | Nombre de usuario de base de datos (requerido si `TrustedConnection` es false) |
| `ServerPassword` | string (nullable) | Contraseña de base de datos (requerido si `TrustedConnection` es false) |
| `Database` | string | Nombre de base de datos SAP B1 |
| `User` | string (nullable) | Nombre de usuario de aplicación SAP B1 |
| `Password` | string (nullable) | Contraseña de aplicación SAP B1 |

### Mapeo de ServerType

| Valor | Servidor de Base de Datos |
|-------|---------------------------|
| `4` | MSSQL 2005 |
| `6` | MSSQL 2008 |
| `7` | MSSQL 2012 |
| `8` | MSSQL 2014 |
| `10` | MSSQL 2016 |
| `11` | MSSQL 2017 |
| `15` | MSSQL 2019 |
| `9` | SAP HANA |

## Ejemplo de Configuración Completa

```yaml
# Configuraciones del Sistema Principal
# Configuraciones esenciales de operación de negocio, integración de sistemas externos y configuraciones de almacén
# Este es un archivo de configuración obligatorio para la operación del sistema

# Opciones de Operación de Negocio
# Controla la funcionalidad central del WMS y el comportamiento del flujo de trabajo
Options:
  # Configuración General
  EnableUseBaseUn: true
  DozensLabel: "Metros"
  DozensAbbr: "M"
  MaxUnitLevel: Pack
  ScannerMode: ItemBarcode
  DisplayVendor: true
  WhsCodeBinSuffix: false

  # Configuración de Recepción de Mercancías
  GoodsReceiptDraft: false
  GoodsReceiptModificationsRequiredSupervisor: true
  GoodsReceiptCreateSupervisorRequired: true
  GoodsReceiptType: Both
  GoodsReceiptTargetDocuments: false
  GoodsReceiptPackages: true
  GoodsReceiptConfirmationAdjustStock: true

  # Configuración de Transferencia
  TransferTargetItems: false
  EnableTransferConfirm: true
  EnableTransferRequest: false
  EnableWarehouseTransfer: true
  TransferCreateSupervisorRequired: false
  DirectTransferAll: true

  # Configuración de Paquete y Unidad
  EnablePackages: false
  DefaultUnitType: Pack
  EnableUnitSelection: true

  # Gestión de Sesión
  IdleLogoutTimeout: 0

  # Configuración de Lista de Recogida
  EnablePickingCheck: true

  # Configuración de Cantidad
  EnableDecimalQuantities: true

  # Anulaciones de Unidad de Documento
  DocumentUnitOverrides:
    Transfer:
      DefaultUnitType: Unit

# Configuración de Filtrado de Datos
# Filtros basados en SQL para restringir visibilidad y acceso de datos
Filters:
  Vendors: "\"QryGroup1\" = 'Y'"
  PickPackOnly:
    Query: "OCRD.\"QryGroup4\" = 'Y'"
    GroupBy: "OCRD.\"QryGroup4\""

# Configuración Específica del Almacén
# Mapeos de entrada de contenedor e configuraciones operacionales específicas del almacén
Warehouses:
  WH01:
    InitialCountingBinEntry: 1001
    CancelPickingBinEntry: 1002
    StagingBinEntry: 1003

# Integración de Sistema Externo
# Especifica qué adaptador utilizar para integración de SAP Business One (enum, requerido)
ExternalAdapter: SboServiceLayer

# Configuraciones de Conexión de SAP Business One
# Configuración para conectarse a la base de datos y servicios de SAP B1
SboSettings:
  Server: your-sap-server
  ServiceLayerUrl: https://your-sap-server:50000
  ServerType: 15
  TrustedConnection: false
  ServerUser: YOUR_SAP_USER
  ServerPassword: YOUR_SAP_PASSWORD
  Database: YOUR_DATABASE
  User: YOUR_SAP_USER
  Password: YOUR_SAP_PASSWORD
```
