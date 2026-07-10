---
title: Configuración del Backend (appsettings.json)
description: Configura el Servicio EZY WMS con appsettings.json. Referencia para todos los ajustes incluyendo CORS, registros, base de datos, autenticación, licencias y servicios en segundo plano.
---

> **Nota para la versión actual:** La configuración activa se guarda en la base de datos y se administra desde Configuración para superusuarios. Los archivos YAML en `Service/init` son plantillas iniciales; no son la fuente de configuración en tiempo de ejecución.

El Servicio EZY WMS lee los parámetros de infraestructura desde `appsettings.json`,
ubicado en `ezy-wms-backend/Service/`. Las secciones de negocio de WMS se guardan
en la base de datos; este archivo cubre el host, CORS, registros, conexiones de
bases de datos, autenticación JWT, sesiones, licencias, servicios en segundo
plano, correo y verificaciones de salud. Use `appsettings.example.json` como punto
de partida y proporcione solo los parámetros requeridos por la implementación.

## Cors

Define qué orígenes pueden realizar solicitudes entre orígenes a la API.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `AllowedOrigins` | Array de strings | `["http://localhost:5001"]` | Lista de URLs de origen permitidas (ej. frontend del Portal). El comodín `*` no se recomienda para producción. |

**Nota:** Asegúrate de incluir el Portal y otros clientes frontend. Las solicitudes de orígenes no listados serán rechazadas.

## Serilog

Configuración de registros estructurados. Los registros se escriben en consola y archivos rotatorios.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `Using` | Array | `["Serilog.Sinks.Console", "Serilog.Sinks.File"]` | Receptores de registro habilitados. |
| `MinimumLevel.Default` | String | `Warning` | Nivel mínimo de registro para todos los registradores (ej. `Debug`, `Information`, `Warning`, `Error`). |
| `MinimumLevel.Override` | Object | Ver ejemplo | Anulaciones por espacio de nombres (ej. `Microsoft.Hosting.Lifetime: Information`). |
| `WriteTo[0].Name` | String | `Console` | Salida de registro a consola. |
| `WriteTo[1].Name` | String | `File` | Salida de registro a archivo rotatorio. |
| `WriteTo[1].Args.path` | String | `./logs/log-.txt` | Ruta del archivo de registro con marcador de posición `{Date}` para rotación diaria. |
| `WriteTo[1].Args.rollingInterval` | String | `Day` | Rotar registros diariamente. Opciones: `Day`, `Hour`, `Month`. |
| `WriteTo[1].Args.retainedFileCountLimit` | Integer | `7` | Retener este número de archivos de registro. Los archivos más antiguos se eliminan. |
| `WriteTo[1].Args.fileSizeLimitBytes` | Integer | `10485760` (10 MB) | Tamaño máximo por archivo de registro antes de la rotación. |
| `WriteTo[1].Args.rollOnFileSizeLimit` | Boolean | `true` | Rotar archivos de registro cuando se alcanza el límite de tamaño. |
| `Enrich` | Array | `["FromLogContext", "WithMachineName", "WithThreadId"]` | Agregar información de contexto a los registros (nombre de máquina, ID de hilo). |

**Notas:**
- Los registros se almacenan en `./logs/` (relativo al ejecutable del servicio).
- Las implementaciones de producción deben usar al menos nivel `Warning` para reducir el ruido.
- Los registros de Entity Framework Core están configurados en `Warning` por defecto para reducir verbosidad.

## Logging

Configuración heredada de registro de .NET. Serilog es preferido pero ambos coexisten.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `LogLevel.Default` | String | `Error` | Nivel predeterminado para todos los registradores. |
| `LogLevel.Microsoft` | String | `Error` | Nivel para registros del marco de Microsoft. |
| `LogLevel.Microsoft.Hosting.Lifetime` | String | `Error` | Nivel para registros de inicio/parada de servicio. |

## AllowedHosts

Ajuste de seguridad para restringir qué encabezados Host aceptará el servicio.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `AllowedHosts` | String | `*` | Valores del encabezado Host permitidos. Usa `*` para permitir todos, o lista nombres de host específicos (separados por comas). |

**Nota:** En producción, reemplaza `*` con nombres de host o direcciones IP reales para prevenir ataques basados en host.

## Kestrel

Configuración del servidor HTTP.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `Kestrel.Endpoints.Http.Url` | String | `http://localhost:5000` | URL del punto de conexión HTTP. Usa `http://0.0.0.0:5000` para escuchar en todas las interfaces. |

**Nota:** Cambia el puerto o dirección de enlace para que coincida con tu red de implementación. El `localhost` predeterminado solo acepta conexiones locales.

## ConnectionStrings

Cadenas de conexión de base de datos para el servicio y adaptadores externos.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `DefaultConnection` | String | Cadena de conexión SQL Server | Base de datos WMS principal. Incluye servidor, nombre de base de datos, usuario, contraseña y configuración de confianza de certificado. |
| `ExternalAdapterConnection` | String | Cadena de conexión SQL Server | Base de datos opcional para adaptadores de sistemas externos. Puede apuntar al mismo o diferente SQL Server. |

**Nota:** Ambas utilizan formato de cadena de conexión SQL Server. Para SQL en la nube (Azure, etc.), ajusta `Server`, `User Id` y `Password`. `TrustServerCertificate=true` es para desarrollo; para producción, usa certificados adecuados.

## Jwt

Configuración de autenticación JWT para tokens Bearer.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `Key` | String | `JwtToken` (ejemplo) | Clave secreta para firmar tokens JWT. Debe ser una cadena larga y aleatoria (al menos 32 bytes). |
| `Issuer` | String | `YourIssuer` (ejemplo) | Reclamación del emisor del token (ej. `EzyWMS`). |
| `Audience` | String | `YourAudience` (ejemplo) | Reclamación de audiencia del token (ej. `EzyWMS-Client`). |
| `ExpiresInMinutes` | Integer | `60` | Tiempo de expiración del token en minutos. Después de esto, los tokens no son válidos. |

**Crítico:** Reemplaza `Key` con una cadena aleatoria criptográficamente segura. El valor de ejemplo `JwtToken` es solo para demostración y no debe usarse en producción.

## SessionManagement

Configura el backend de almacenamiento de sesiones (en memoria o Redis).

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `Type` | String | `Redis` (ejemplo) o `InMemory` | Tipo de almacenamiento de sesión. Opciones: `InMemory` (solo instancia única), `Redis` (distribuido). |
| `Redis.Host` | String | `localhost` | Nombre de host o IP del servidor Redis. Obligatorio si `Type: Redis`. |
| `Redis.Port` | Integer | `6379` | Puerto del servidor Redis. El predeterminado es `6379`. |
| `Cookie.Domain` | String o null | `null` | Dominio de la cookie (ej. `.example.com`). Deja `null` para solo el dominio actual. |
| `Cookie.Secure` | Boolean | `false` | Enviar cookie solo sobre HTTPS. Configura `true` en producción. |
| `Cookie.SameSite` | String | `Lax` | Modo de protección CSRF. Opciones: `Strict`, `Lax`, `None`. Usa `Lax` para implementaciones típicas. |
| `Cookie.HttpOnly` | Boolean | `true` | Prevenir acceso JavaScript a la cookie de sesión. Mantén `true` por seguridad. |

**Notas Críticas:**
- Si `Type: Redis`, un servidor Redis debe estar ejecutándose y accesible en el host y puerto especificados. Sin Redis, las operaciones de sesión fallarán.
- Para implementaciones de instancia única sin Redis, usa `InMemory`, pero las sesiones se pierden al reiniciar.
- En producción, siempre configura `Cookie.Secure: true` y usa HTTPS.

## Licensing

Configuración de validación y almacenamiento en caché de licencias.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `EncryptionKey` | String (base64) | Ver ejemplo | Clave de 32 bytes codificada en base64 para cifrar datos de licencia. Debe coincidir con la clave utilizada por la nube de licencias. |
| `CloudEndpoint` | String (URL) | `http://localhost:3001` | URL del servicio en la nube de licencias. |
| `BearerToken` | String | Ver ejemplo | Token Bearer para autenticar solicitudes al servicio en la nube de licencias. |
| `CacheExpirationHours` | Integer | `24` | Cuánto tiempo cachear licencias válidas localmente antes de verificar con la nube. |
| `GracePeriodDays` | Integer | `7` | Días que el servicio puede operar con una licencia expirada o inválida antes de bloquear operaciones. |
| `DemoExpirationDays` | Integer | `30` | Período de validez para licencias de demostración. |
| `WarningThresholdDays` | Integer | `3` | Días restantes hasta que expire la licencia cuando comiencen las advertencias. |

**Notas Críticas:**
- `EncryptionKey` debe ser exactamente un valor base64 codificado de 32 bytes. El formato incorrecto causará fallos de descifrado de licencia.
- `CloudEndpoint` y `BearerToken` deben apuntar al servicio de licencias. Los valores inválidos evitan la validación de licencias.
- Durante el período de gracia, el servicio continúa operando con una advertencia, pero después de que expire el período de gracia, las características se bloquean.

## BackgroundServices

Tareas de larga duración ejecutadas en segundo plano.

### PickListSync

Sincroniza operaciones de lista de picking con el inventario.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `IntervalSeconds` | Integer | `60` | Frecuencia de comprobación de actualizaciones de lista de picking (en segundos). |
| `Enabled` | Boolean | `true` | Habilita o deshabilita el servicio de sincronización de listas de picking. |
| `CheckClosedPickLists` | Boolean | `true` | Monitorear listas de picking cerradas para verificar finalización. |
| `ProcessPackageMovements` | Boolean | `true` | Actualizar inventario cuando se mueven paquetes. |

### CloudSync

Sincroniza datos con la nube de licencias/gestión.

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `SyncIntervalMinutes` | Integer | `10` | Frecuencia de sincronización con la nube (en minutos). |
| `ValidationIntervalHours` | Integer | `24` | Frecuencia de validación completa contra la nube (en horas). |
| `Enabled` | Boolean | `true` | Habilita o deshabilita la sincronización con la nube. |

**Nota:** CloudSync requiere conectividad de red al punto de conexión en la nube. Si la nube es inaccesible, el servicio continúa con datos en caché pero registrará advertencias.

## Smtp

Configuración de correo electrónico para enviar notificaciones (opcional).

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `Enabled` | Boolean | `false` | Habilita o deshabilita la funcionalidad de correo electrónico. |
| `Host` | String | Vacío | Nombre de host del servidor SMTP (ej. `smtp.gmail.com`). Obligatorio si `Enabled: true`. |
| `Port` | Integer | `587` | Puerto SMTP (típicamente `587` para TLS o `25` para no encriptado). |
| `EnableSsl` | Boolean | `true` | Usa TLS/SSL para la conexión SMTP. |
| `Username` | String | Vacío | Nombre de usuario de autenticación SMTP. |
| `Password` | String | Vacío | Contraseña de autenticación SMTP. |
| `FromEmail` | String | Vacío | Dirección de correo del remitente (ej. `noreply@example.com`). |
| `FromName` | String | `EzyWMS` | Nombre visible del remitente. |
| `TimeoutSeconds` | Integer | `30` | Tiempo de espera para operaciones SMTP (en segundos). |
| `TimeZoneId` | String | `America/Panama` | Zona horaria para marcas de tiempo de correo electrónico y programación. |

**Notas:**
- El correo electrónico es opcional. Configura `Enabled: false` para deshabilitarlo (no se envían correos electrónicos).
- Si está habilitado, todos los campos SMTP deben completarse. Las credenciales inválidas causarán fallos de envío de correo electrónico y errores en registros.

## UptimeKuma

Integración de verificación de salud con el servicio de monitoreo UptimeKuma (opcional).

| Campo | Tipo | Predeterminado | Descripción |
|-------|------|--------|-------------|
| `Enabled` | Boolean | `false` | Habilita o deshabilita los latidos de UptimeKuma. |
| `PushUrl` | String | Ver ejemplo | URL del punto de conexión del monitor de inserción de UptimeKuma. Formato: `https://uptime.example.com/api/push/YOUR_PUSH_KEY`. |
| `HeartbeatIntervalSeconds` | Integer | `60` | Frecuencia de envío de latidos a UptimeKuma (en segundos). |
| `SendOnStartup` | Boolean | `true` | Enviar un latido cuando se inicia el servicio. |
| `SendOnShutdown` | Boolean | `true` | Enviar un latido cuando se apaga el servicio. |

**Notas:**
- El monitoreo de UptimeKuma es opcional. Configura `Enabled: false` para deshabilitarlo.
- Si está habilitado, la `PushUrl` debe ser válida y accesible. Las URLs inaccesibles causarán tiempos de espera de conexión en registros pero no bloquearán el inicio del servicio.

---

## Ejemplo Completo

A continuación se muestra un archivo `appsettings.json` completamente anotado adecuado para una implementación de producción. Reemplaza todos los valores de marcador de posición con los valores reales del entorno.

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://portal.example.com",
      "https://admin.example.com"
    ]
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "./logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "Microsoft": "Error",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "portal.example.com,admin.example.com,your-db-server",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-db-server;Database=EZY_WMS;User Id=sa;Password=YOUR_DB_PASSWORD;TrustServerCertificate=false;Encrypt=true;",
    "ExternalAdapterConnection": "Server=your-db-server;Database=EZY_ADAPTERS;User Id=sa;Password=YOUR_DB_PASSWORD;TrustServerCertificate=false;Encrypt=true;"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_RANDOM_SECRET_AT_LEAST_32_BYTES_LONG_AND_CRYPTOGRAPHICALLY_SECURE",
    "Issuer": "EzyWMS",
    "Audience": "EzyWMS-Client",
    "ExpiresInMinutes": 60
  },
  "SessionManagement": {
    "Type": "Redis",
    "Redis": {
      "Host": "your-redis-server",
      "Port": 6379
    },
    "Cookie": {
      "Domain": null,
      "Secure": true,
      "SameSite": "Lax",
      "HttpOnly": true
    }
  },
  "PresenceTracking": {
    "Type": "Redis"
  },
  "Licensing": {
    "EncryptionKey": "REPLACE_WITH_BASE64_ENCODED_32_BYTE_KEY",
    "CloudEndpoint": "https://licensing.example.com",
    "BearerToken": "REPLACE_WITH_BEARER_TOKEN",
    "CacheExpirationHours": 24,
    "GracePeriodDays": 7,
    "DemoExpirationDays": 30,
    "WarningThresholdDays": 3
  },
  "BackgroundServices": {
    "PickListSync": {
      "IntervalSeconds": 60,
      "Enabled": true,
      "CheckClosedPickLists": true,
      "ProcessPackageMovements": true
    },
    "CloudSync": {
      "SyncIntervalMinutes": 10,
      "ValidationIntervalHours": 24,
      "Enabled": true
    }
  },
  "Smtp": {
    "Enabled": true,
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "noreply@example.com",
    "Password": "YOUR_SMTP_PASSWORD",
    "FromEmail": "noreply@example.com",
    "FromName": "EZY WMS",
    "TimeoutSeconds": 30,
    "TimeZoneId": "America/Panama"
  },
  "UptimeKuma": {
    "Enabled": true,
    "PushUrl": "https://uptime.example.com/api/push/REPLACE_ME",
    "HeartbeatIntervalSeconds": 60,
    "SendOnStartup": true,
    "SendOnShutdown": true
  }
}
```
