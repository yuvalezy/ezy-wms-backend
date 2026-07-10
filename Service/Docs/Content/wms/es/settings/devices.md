---
title: Dispositivos
description: Ver y gestionar todos los dispositivos WMS registrados — activar, deshabilitar, renombrar y auditar el historial de estados.
---

La página de Dispositivos lista todos los dispositivos que han accedido al WMS. Un Super Usuario puede activar nuevos dispositivos, deshabilitar los no utilizados, renombrarlos y revisar el historial completo de cambios de estado.

:::note
Solo los **Super Usuarios** pueden acceder a Configuración → Dispositivos.
:::

## Lista de Dispositivos

![Lista de dispositivos mostrando barra de búsqueda, filtro de estado y tarjetas de dispositivos](/docs/wms/settings/devices/devices-list-es.png)

La lista muestra una tarjeta por dispositivo registrado con:

| Campo | Descripción |
|-------|-------------|
| **Nombre Dispositivo** | Etiqueta legible asignada en el primer registro |
| **UUID** | Identificador único generado por el navegador del dispositivo |
| **Fecha de Registro** | Fecha en que el dispositivo inició sesión por primera vez |
| **Estado** | Badge de estado actual — Active, Inactive o Disabled |

Use el campo **Buscar** para filtrar por nombre o UUID. Use el desplegable **Todos los Estados** para filtrar por estado.

:::tip
Cuando inicia sesión en un dispositivo Inactivo, aparece un banner amarillo en la parte superior de la lista con un botón rápido **Activar Dispositivo** para activarlo en un solo paso.
:::

## Detalles del Dispositivo

Haga clic en **Ver Detalles** en cualquier tarjeta para abrir el diálogo de detalles del dispositivo.

![Diálogo de detalles del dispositivo — pestaña Detalles](/docs/wms/settings/devices/devices-details-es.png)

La pestaña **Detalles** muestra:
- Nombre del dispositivo y estado actual
- UUID completo del dispositivo
- Fecha de registro
- Notas de estado (razón registrada del último cambio de estado)

Dos botones de acción están disponibles al final:
- **Actualizar Estado** — cambiar el estado del dispositivo (Activar / Deshabilitar)
- **Cambiar Nombre** — renombrar el dispositivo

## Historial de Auditoría

Haga clic en la pestaña **Historial de Auditoría** dentro del diálogo de detalles para ver el registro completo de cambios de estado.

![Pestaña de historial de auditoría mostrando una entrada de cambio de estado](/docs/wms/settings/devices/devices-audit-es.png)

Cada entrada registra:
- Tipo de acción y marca de tiempo
- Usuario que realizó el cambio
- Razón proporcionada
- Estado anterior → Nuevo estado

## Actualizar Estado del Dispositivo

Desde la pestaña Detalles, haga clic en **Actualizar Estado** para abrir el formulario de estado.

![Diálogo Actualizar Estado del Dispositivo](/docs/wms/settings/devices/devices-status-form-es.png)

1. Seleccione el **Nuevo Estado** del desplegable (Active o Disabled).
2. Ingrese una **Razón** para el cambio (requerido, 10–500 caracteres).
3. Haga clic en **Actualizar Estado**.

El cambio se registra en el Historial de Auditoría del dispositivo.

:::caution
Establecer un dispositivo como **Disabled** bloquea inmediatamente el inicio de sesión de todos los usuarios en ese dispositivo.
:::

## Cambiar Nombre del Dispositivo

Desde la pestaña Detalles, haga clic en **Cambiar Nombre** para renombrar el dispositivo.

![Diálogo Cambiar Nombre del Dispositivo](/docs/wms/settings/devices/devices-name-form-es.png)

1. Borre el nombre actual e ingrese un nuevo **Nombre de Dispositivo** (3–100 caracteres).
2. Haga clic en **Cambiar Nombre**.

Los nombres de dispositivos deben ser únicos en el sistema.

## Estados del Dispositivo

| Estado | Descripción |
|--------|-------------|
| **Active** | Completamente operacional; los usuarios pueden iniciar sesión |
| **Inactive** | Recién registrado; inicio de sesión bloqueado hasta ser activado por un Super Usuario |
| **Disabled** | Bloqueado manualmente por un administrador |

## Páginas Relacionadas

- [Primeros Pasos — Registro de Dispositivos](/docs/getting-started/device-registration)
- [Resumen de Configuración](/docs/settings/index)
