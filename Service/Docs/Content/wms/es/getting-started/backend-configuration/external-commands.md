---
title: Configuración de Comandos Externos
description: Configure comandos externos automatizados que generan y entregan archivos en respuesta a eventos del almacén
---

> **Nota para la versión actual:** La configuración activa se guarda en la base de datos y se administra desde Configuración para superusuarios. Los archivos YAML en `Service/init` son plantillas iniciales; no son la fuente de configuración en tiempo de ejecución.

## Descripción General

El archivo de configuración `ExternalCommands.yaml` permite a EZY WMS generar y entregar automáticamente archivos XML o JSON a sistemas externos en respuesta a eventos del almacén. Los comandos pueden ser activados por eventos del sistema (creación, activación y cierre de paquetes) o acciones manuales del usuario, con soporte para múltiples tipos de destino y características avanzadas como ejecución por lotes y políticas de reintentos.

**Ubicación de configuración:** `ezy-wms-backend/Service/config/ExternalCommands.yaml`

Para ejemplos JSON más detallados con cobertura extendida de campos, consulte `appsettings.external_commands_example.json` en el repositorio fuente.

## Conceptos Fundamentales

- **Comandos:** Configuraciones de comandos externos individuales que definen qué datos recopilar, cómo formatearlos y dónde entregarlos.
- **Consultas:** Consultas SQL ejecutadas para recopilar datos de la base de datos, con soporte para parametrización y operaciones por lotes.
- **Destinos:** Objetivos de entrega incluyendo rutas locales, recursos compartidos de red, FTP y servidores SFTP.
- **Configuración Global:** Configuración de todo el sistema para concurrencia, tiempos de espera, reintentos y formateo de archivos.

## Array de Comandos

Cada comando en `ExternalCommands.Commands` es una configuración independiente con la siguiente estructura:

### Campos del Comando

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `Id` | string | Sí | Identificador único del comando |
| `Name` | string | Sí | Nombre legible |
| `Description` | string | Sí | Descripción de la funcionalidad |
| `ObjectType` | enum | Sí | Tipo de objeto: `GoodsReceipt`, `InventoryCounting`, `Transfer`, `Picking`, `Package`, `PickingClosure` |
| `TriggerType` | enum | Sí | Cuándo activar: `CreatePackage`, `ActivatePackage`, `ClosePackage`, `Manual` |
| `Enabled` | bool | No | Habilitar/deshabilitar comando (predeterminado: `true`) |
| `AllowBatchExecution` | bool | No | Soporte ejecución por lotes para múltiples elementos (predeterminado: `false`) |
| `Queries` | array | Sí | Array de objetos `CommandQuery` |
| `FileFormat` | enum | Sí | Formato de salida: `XML` o `JSON` |
| `FileNamePattern` | string | Sí | Plantilla de nombre de archivo con marcadores de posición |
| `Destination` | object | Sí | Configuración de entrega |
| `UIConfiguration` | object | No | Configuración de UI para comandos manuales |

### Enum ObjectType

- `GoodsReceipt` (0)
- `InventoryCounting` (1)
- `Transfer` (2)
- `Picking` (3)
- `Package` (4)
- `PickingClosure` (5)

### Enum TriggerType

- `CreatePackage` (0) — Se activa cuando se crea un paquete
- `ActivatePackage` (1) — Se activa cuando se activa un paquete
- `ClosePackage` (2) — Se activa cuando se cierra un paquete
- `Manual` (3) — Se activa por acción del usuario (requiere `UIConfiguration`)

## Consultas

Las consultas recuperan datos de la base de datos para rellenar el archivo generado. Cada consulta admite parámetros y puede devolver una o múltiples filas.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Name` | string | Identificador de consulta |
| `Query` | string | Instrucción SQL; soporta parámetros `@ObjectId`, `@PackageId`, `@PackageIds` |
| `ResultType` | enum | `Single` (una fila) o `Multiple` (múltiples filas) |
| `IsBatchQuery` | bool | Indica soporte ejecución por lotes con múltiples IDs (predeterminado: `false`) |

## Formato de Archivo y Nomenclatura

### FileFormat

- `XML` (0) — Genera documentos XML
- `JSON` (1) — Genera documentos JSON

### Marcadores de Posición en FileNamePattern

| Marcador | Ejemplo | Notas |
|----------|---------|-------|
| `{Barcode}` | `PKG123456` | Código de barras del paquete del resultado de consulta |
| `{ObjectId}` | `42` | ID del objeto siendo procesado |
| `{WhsCode}` | `WH01` | Código de almacén del resultado de consulta |
| `{Timestamp:format}` | `20260424143022` | Marca de tiempo actual; usar formato .NET (ej: `yyyyMMddHHmmss`) |
| `{BatchIndex}` | `1` | Índice en ejecución por lotes (basado en 0) |
| Campos de consulta | Personalizado | Cualquier campo devuelto por consultas |

Ejemplo: `PKG_{Barcode}_{Timestamp:yyyyMMddHHmmss}.xml` → `PKG_ABC123_20260424143022.xml`

## Configuración de Destino

El destino especifica dónde se entrega el archivo generado.

| Campo | Tipo | Se aplica a | Descripción |
|-------|------|------------|-------------|
| `Type` | enum | Todos | `LocalPath`, `NetworkPath`, `FTP`, `SFTP` |
| `Path` | string | Todos | Ruta de archivo, ruta UNC, o directorio FTP/SFTP |
| `Host` | string | FTP, SFTP | Nombre de host o dirección IP |
| `Port` | int | FTP, SFTP | Número de puerto (21 para FTP, 22 para SFTP) |
| `Username` | string | FTP, SFTP, NetworkPath | Nombre de usuario de autenticación |
| `Password` | string | FTP, SFTP, NetworkPath | Contraseña de autenticación (recomendado cifrar) |
| `UseNetworkImpersonation` | bool | NetworkPath | Usar suplantación para acceso de red (predeterminado: `false`) |
| `UsePassiveMode` | bool | FTP | Modo pasivo FTP (predeterminado: `true`) |
| `UseSsl` | bool | FTP | FTP sobre SSL/TLS (predeterminado: `false`) |
| `PrivateKeyPath` | string | SFTP | Ruta al archivo de clave privada |
| `PrivateKeyPassphrase` | string | SFTP | Contraseña para clave privada cifrada |
| `HostFingerprint` | string | SFTP | Huella dactilar de host esperada para verificación |

### Tipos de Destino

**LocalPath:** Escribe en un directorio del sistema de archivos local.
```yaml
Destination:
  Type: LocalPath
  Path: C:\PrintQueue\Labels\Incoming
```

**NetworkPath:** Escribe en un recurso compartido de red (ruta UNC) con suplantación opcional.
```yaml
Destination:
  Type: NetworkPath
  Path: \\PrintServer\Labels\PackageContents
  UseNetworkImpersonation: true
  Username: DOMAIN\service_account
  Password: YOUR_PASSWORD  # Recomendado cifrado/referencia secreta
```

**FTP:** Entrega vía FTP (Protocolo de Transferencia de Archivos).
```yaml
Destination:
  Type: FTP
  Host: ftp.example.com
  Port: 21
  Path: /imports/packages
  Username: your-ftp-user
  Password: YOUR_FTP_PASSWORD  # Recomendado cifrado/referencia secreta
  UsePassiveMode: true
  UseSsl: false  # Cambiar a true para FTPS
```

**SFTP:** Entrega vía SFTP (Protocolo de Transferencia de Archivos SSH) con autenticación basada en clave.
```yaml
Destination:
  Type: SFTP
  Host: sftp.partner.example
  Port: 22
  Path: /incoming/packages
  Username: your-sftp-user
  Password: YOUR_SFTP_PASSWORD  # Usar si autenticación por contraseña; cifrar si se proporciona
  PrivateKeyPath: C:\Keys\partner_sftp_key.ppk  # Ruta a clave privada
  PrivateKeyPassphrase: YOUR_KEY_PASSPHRASE
  HostFingerprint: ssh-rsa 2048 xx:xx:xx:...:xx  # Verificación de huella dactilar de host
```

## Configuración de UI (Comandos Manuales)

Requerida cuando `TriggerType` es `Manual`. Controla cómo se presenta el comando a los usuarios.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `AllowedScreens` | string[] | Lista de pantallas donde aparece el botón de comando |
| `ButtonText` | string | Texto de la etiqueta del botón (predeterminado: "Execute Command") |
| `RequireConfirmation` | bool | Requerir confirmación del usuario (predeterminado: `true`) |
| `ConfirmationMessage` | string | Texto del cuadro de diálogo de confirmación; soporta `{count}` para operaciones por lotes |
| `MaxBatchSize` | int | Máximo de elementos permitidos en una operación por lotes |

## Configuración Global

Configuración de todo el sistema para todos los comandos externos.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|---|-------------|
| `MaxConcurrentExecutions` | int | 5 | Máximo de ejecuciones concurrentes de comandos |
| `CommandTimeout` | int | 30 | Tiempo de espera de ejecución en segundos |
| `RetryPolicy.MaxRetries` | int | 3 | Máximo de intentos de reintentos |
| `RetryPolicy.RetryDelaySeconds` | int | 5 | Retraso entre reintentos (segundos) |
| `RetryPolicy.RetryOnErrors` | string[] | `["NetworkError", "TimeoutError"]` | Tipos de error que desencadenan reintentos |
| `FileEncoding` | string | UTF-8 | Codificación de archivo |
| `XmlSettings.RootElementName` | string | Data | Nombre del elemento raíz XML |
| `XmlSettings.IncludeXmlDeclaration` | bool | true | Incluir declaración XML |
| `XmlSettings.IndentXml` | bool | true | Indentar salida XML |
| `JsonSettings.IndentJson` | bool | true | Indentar salida JSON |
| `JsonSettings.CamelCasePropertyNames` | bool | false | Usar camelCase para propiedades JSON |
| `JsonSettings.DateFormat` | string | yyyy-MM-ddTHH:mm:ss | Formato de fecha para JSON |

## Ejemplos

### Ejemplo 1: XML Activado Automáticamente a Ruta Local

Genera automáticamente un archivo XML cuando se crea un paquete y lo escribe en un directorio de cola de impresión local.

```yaml
- Id: AutoPrintPackageLabel
  Name: Imprimir Etiqueta de Paquete Automáticamente
  Description: Imprime etiqueta de código de barras automáticamente cuando se crea el paquete
  ObjectType: Package
  TriggerType: CreatePackage
  Enabled: true
  Queries:
    - Name: PackageData
      Query: SELECT p.Barcode, p.CreatedDate FROM Packages p WHERE p.Id = @ObjectId
      ResultType: Single
  FileFormat: XML
  FileNamePattern: PKG_{Barcode}_{Timestamp:yyyyMMddHHmmss}.xml
  Destination:
    Type: LocalPath
    Path: C:\PrintQueue\Labels\Incoming
```

### Ejemplo 2: Exportación JSON Manual con Soporte de Lotes

Comando manual que permite a los usuarios exportar contenido de paquete como JSON con capacidad de procesamiento por lotes.

```yaml
- Id: ExportPackageJSON
  Name: Exportar Contenido del Paquete
  Description: Exportar datos de paquete seleccionados como JSON
  ObjectType: Package
  TriggerType: Manual
  Enabled: true
  AllowBatchExecution: true
  UIConfiguration:
    AllowedScreens:
      - PackageDetails
      - PackageList
    ButtonText: Exportar Seleccionados
    RequireConfirmation: true
    ConfirmationMessage: ¿Exportar {count} paquete(s) al sistema externo?
    MaxBatchSize: 50
  Queries:
    - Name: PackageHeader
      Query: SELECT p.Id, p.Barcode, p.WhsCode, p.Status FROM Packages p WHERE p.Id = @ObjectId
      ResultType: Single
    - Name: PackageContents
      Query: SELECT ItemCode, Quantity, BatchNumbers FROM PackageContents WHERE PackageId = @ObjectId
      ResultType: Multiple
  FileFormat: JSON
  FileNamePattern: EXPORT_{WhsCode}_{Barcode}_{Timestamp:yyyyMMddHHmmss}.json
  Destination:
    Type: NetworkPath
    Path: \\ExportServer\PackageExports
    UseNetworkImpersonation: true
    Username: DOMAIN\export_service
    Password: YOUR_PASSWORD
```

### Ejemplo 3: Entrega SFTP con Autenticación de Clave

Envía datos de paquete al servidor SFTP de un socio utilizando autenticación de clave pública.

```yaml
- Id: SendToPartnerSFTP
  Name: Enviar Paquete al Socio
  Description: Envía información de paquete al servidor SFTP del socio
  ObjectType: Package
  TriggerType: CreatePackage
  Enabled: true
  Queries:
    - Name: PackageSummary
      Query: >
        SELECT p.Barcode, p.WhsCode, COUNT(DISTINCT pc.ItemCode) as ItemCount,
        SUM(pc.Quantity) as TotalQuantity
        FROM Packages p LEFT JOIN PackageContents pc ON p.Id = pc.PackageId
        WHERE p.Id = @ObjectId
        GROUP BY p.Id, p.Barcode, p.WhsCode
      ResultType: Single
  FileFormat: JSON
  FileNamePattern: '{WhsCode}_PKG_{Barcode}_{Timestamp:yyyyMMddHHmmss}.json'
  Destination:
    Type: SFTP
    Host: sftp.partner.example
    Port: 22
    Path: /incoming/packages
    Username: your-sftp-user
    Password: YOUR_SFTP_PASSWORD
    PrivateKeyPath: C:\Keys\partner_sftp_key.ppk
    PrivateKeyPassphrase: YOUR_KEY_PASSPHRASE
    HostFingerprint: ssh-rsa 2048 xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx
```

## Ejemplo Anotado Completo

A continuación se encuentra un `ExternalCommands.yaml` completo con múltiples comandos y configuración global:

```yaml
ExternalCommands:
  # Definiciones de comando
  Commands:
    # Comando 1: Generación automática de XML en creación de paquete
    - Id: CreatePackageLabel
      Name: Etiqueta de Creación de Paquete
      Description: Genera archivo de etiqueta XML cuando se crea el paquete
      ObjectType: Package
      TriggerType: CreatePackage
      Enabled: true
      Queries:
        - Name: PackageInfo
          Query: SELECT p.Id, p.Barcode, p.WhsCode, p.CreatedDate FROM Packages p WHERE p.Id = @ObjectId
          ResultType: Single
      FileFormat: XML
      FileNamePattern: PKG_{Barcode}_{Timestamp:yyyyMMddHHmmss}.xml
      Destination:
        Type: LocalPath
        Path: C:\PrintQueue\Labels\Incoming

    # Comando 2: Exportación manual con lote y destino de red
    - Id: BatchExportJSON
      Name: Exportación por Lotes de Datos de Paquete
      Description: Exportar múltiples paquetes a recurso compartido de red como JSON
      ObjectType: Package
      TriggerType: Manual
      Enabled: true
      AllowBatchExecution: true
      UIConfiguration:
        AllowedScreens:
          - PackageList
          - PackageDetails
        ButtonText: Exportar Paquetes Seleccionados
        RequireConfirmation: true
        ConfirmationMessage: ¿Exportar {count} paquete(s)?
        MaxBatchSize: 100
      Queries:
        - Name: BatchPackageData
          Query: SELECT p.Id, p.Barcode, p.WhsCode, p.Status FROM Packages p WHERE p.Id IN (@PackageIds)
          ResultType: Multiple
          IsBatchQuery: true
      FileFormat: JSON
      FileNamePattern: BATCH_EXPORT_{Timestamp:yyyyMMddHHmmss}_{BatchIndex}.json
      Destination:
        Type: NetworkPath
        Path: \\ExportServer\Packages
        UseNetworkImpersonation: true
        Username: DOMAIN\export_user
        Password: YOUR_PASSWORD

    # Comando 3: Entrega FTP en cierre de paquete
    - Id: ClosedPackageToFTP
      Name: Enviar Paquete Cerrado a FTP
      Description: Carga datos de paquete cerrado en servidor FTP externo
      ObjectType: Package
      TriggerType: ClosePackage
      Enabled: true
      Queries:
        - Name: ClosedPackageData
          Query: SELECT p.*, b.BinCode FROM Packages p LEFT JOIN Bins b ON p.BinEntry = b.BinEntry WHERE p.Id = @ObjectId
          ResultType: Single
        - Name: PackageLineItems
          Query: SELECT ItemCode, Quantity, BatchNumbers FROM PackageContents WHERE PackageId = @ObjectId
          ResultType: Multiple
      FileFormat: XML
      FileNamePattern: CLOSED_{WhsCode}_{Barcode}_{Timestamp:yyyyMMddHHmmss}.xml
      Destination:
        Type: FTP
        Host: ftp.example.com
        Port: 21
        Path: /packages/closed
        Username: your-ftp-user
        Password: YOUR_FTP_PASSWORD
        UsePassiveMode: true
        UseSsl: false

    # Comando 4: SFTP con autenticación de clave
    - Id: SFTPPartnerIntegration
      Name: Integración SFTP del Socio
      Description: Envía datos de paquete de forma segura al socio vía SFTP
      ObjectType: Package
      TriggerType: CreatePackage
      Enabled: true
      Queries:
        - Name: PartnerData
          Query: SELECT p.Barcode, p.WhsCode, COUNT(*) as ItemCount FROM Packages p LEFT JOIN PackageContents pc ON p.Id = pc.PackageId WHERE p.Id = @ObjectId GROUP BY p.Id, p.Barcode, p.WhsCode
          ResultType: Single
      FileFormat: JSON
      FileNamePattern: '{WhsCode}_PKG_{Barcode}.json'
      Destination:
        Type: SFTP
        Host: sftp.partner.example
        Port: 22
        Path: /incoming
        Username: your-sftp-user
        Password: YOUR_SFTP_PASSWORD
        PrivateKeyPath: C:\Keys\partner_key.ppk
        PrivateKeyPassphrase: YOUR_KEY_PASSPHRASE
        HostFingerprint: ssh-rsa 2048 xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx

  # Configuración global del sistema
  GlobalSettings:
    # Máximo de 5 comandos externos pueden ejecutarse concurrentemente
    MaxConcurrentExecutions: 5
    
    # Cada comando tiene 30 segundos para completarse
    CommandTimeout: 30
    
    # Reintentar comandos fallidos
    RetryPolicy:
      MaxRetries: 3
      RetryDelaySeconds: 5
      RetryOnErrors:
        - NetworkError
        - TimeoutError
    
    # Codificación de archivo para todos los archivos de salida
    FileEncoding: UTF-8
    
    # Configuración de formateo XML
    XmlSettings:
      RootElementName: PackageData
      IncludeXmlDeclaration: true
      IndentXml: true
    
    # Configuración de formateo JSON
    JsonSettings:
      IndentJson: true
      CamelCasePropertyNames: false
      DateFormat: yyyy-MM-ddTHH:mm:ss
```

## Consideraciones de Seguridad

- **Credenciales:** Almacene contraseñas en forma cifrada o use sistemas de gestión de secretos. Los valores de marcadores de posición como `YOUR_PASSWORD` deben reemplazarse con valores cifrados o referencias de variables de entorno.
- **Suplantación de Red:** Use cuentas de servicio dedicadas con permisos mínimos necesarios para acceso a recurso compartido de red.
- **Claves SFTP:** Almacene claves privadas de forma segura con permisos de archivo restringidos. Las contraseñas deben cifrarse en reposo.
- **Verificación de Huella Dactilar de Host:** Para SFTP, capture y valide la huella dactilar del host del socio para prevenir ataques de intermediario.

## Solución de Problemas

- **Los comandos no se activan:** Verifique `Enabled: true` y que `TriggerType` coincida con el evento del almacén.
- **Archivo no creado:** Verifique que la ruta de destino existe y que la cuenta de servicio tiene permisos de escritura.
- **Errores de consulta:** Asegúrese de que SQL usa parámetros válidos (`@ObjectId`, `@PackageId`, `@PackageIds`) y que las tablas/columnas referenciadas existen.
- **Fallos de conexión de destino:** Verifique credenciales, configuración de host/puerto y conectividad de red. Verifique reglas de firewall para puertos FTP/SFTP.
- **Errores de tiempo de espera:** Aumente `CommandTimeout` en `GlobalSettings` si las consultas son lentas o la latencia de red es alta.
