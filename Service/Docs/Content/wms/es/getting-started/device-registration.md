---
title: Registro de Dispositivo
description: Cómo registrar un nuevo dispositivo y activarlo para acceder al WMS.
---

EZY WMS utiliza licenciamiento por dispositivo. Cada teléfono, tableta o navegador que acceda al sistema debe registrarse una vez y ser activado por un Super Usuario antes de poder usarse para operaciones de almacén.

## Cómo Funciona el Registro

La primera vez que cualquier usuario inicia sesión desde un dispositivo nuevo, el sistema detecta un UUID de dispositivo no reconocido y solicita un nombre antes de otorgar acceso.

![Nuevo dispositivo detectado — campo de nombre de dispositivo](/docs/wms/login/login-device-name-es.png)

1. Abra la URL del WMS en el navegador.
2. Ingrese su **Código** y toque **Entrar**.
3. El sistema muestra: *"Nuevo dispositivo detectado. Por favor proporcione un nombre para este dispositivo para continuar."*
4. Ingrese un **Nombre de Dispositivo** descriptivo (ej. `Almacén A — Escáner 3`, máx. 100 caracteres).
5. Toque **Entrar**.

El dispositivo queda registrado con estado **Inactivo**.

:::caution
Un dispositivo recién registrado es **Inactivo** por defecto. Los usuarios regulares no pueden iniciar sesión nuevamente hasta que un Super Usuario active el dispositivo. Solo el inicio de sesión del registro inicial es permitido — los intentos siguientes son rechazados hasta la activación.
:::

## Activar el Dispositivo

Solo un **Super Usuario** puede activar dispositivos. Después de registrar un nuevo dispositivo:

1. Inicie sesión en WMS con una cuenta Super Usuario — el sistema redirige automáticamente a **Configuración → Dispositivos**.
2. Encuentre el nuevo dispositivo en la lista (mostrará estado **Inactivo**).
3. Abra el dispositivo y haga clic en **Activar Dispositivo**.
4. Opcionalmente ingrese un motivo para el registro de auditoría.
5. Haga clic en **Activar**.

El estado del dispositivo cambia a **Activo** y el usuario puede iniciar sesión normalmente.

## Estados del Dispositivo

| Estado | Significado |
|--------|-------------|
| **Activo** | Dispositivo completamente operacional; los usuarios pueden iniciar sesión |
| **Inactivo** | Registrado pero no activado; inicio de sesión bloqueado para usuarios no Super |
| **Deshabilitado** | Deshabilitado manualmente por un administrador; inicio de sesión bloqueado |

## Acceso Restringido en Dispositivos Inactivos

Cuando un Super Usuario inicia sesión en un dispositivo Inactivo, el menú operativo principal (Recepción de Mercancías, Picking, Conteo, Transferencia, etc.) está oculto. Solo **Configuración** es accesible, permitiendo al Super Usuario activar el dispositivo directamente desde la misma sesión.

También aparece un banner de estado amarillo en la página de inicio de sesión y dentro de la aplicación cuando un dispositivo está Inactivo o Deshabilitado.

## Gestión de Dispositivos

Toda la gestión de dispositivos está en **Configuración → Dispositivos** y requiere acceso de Super Usuario:

- Ver UUID del dispositivo, fecha de registro y última fecha activa
- Cambiar el estado del dispositivo (Activar / Deshabilitar) con un motivo
- Renombrar un dispositivo
- Ver el historial completo de auditoría de cambios de estado

## Páginas Relacionadas

- [Inicio de Sesión](/docs/overview/login)
- [Configuración — Dispositivos](/docs/settings/devices)
