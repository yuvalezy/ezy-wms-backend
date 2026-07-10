---
title: Licencia
description: Ver y gestionar el estado de licencia y sincronización de cola del WMS.
---

La página de **Licencia** muestra el estado actual de la licencia de la cuenta y el estado de la cola de sincronización con la nube. Los superusuarios pueden actualizar los datos de licencia y disparar una sincronización manual desde esta página.

Ruta: `Configuración → Licencia`

![Página de licencia mostrando las tarjetas de Estado de Licencia y Estado de Cola](/docs/wms/settings/license/license-overview-es.png)

---

## Tarjeta Estado de Licencia

La tarjeta superior resume la salud de la licencia de la cuenta. Su color de borde refleja el estado actual:

| Color | Significado |
|-------|-------------|
| Borde verde | Licencia activa |
| Borde amarillo | Pago pendiente |
| Borde naranja | Estado de pago desconocido |
| Borde rojo | Cuenta deshabilitada |
| Borde azul | Cuenta demo |
| Borde gris | Demo expirado o desconocido |

### Campos

| Campo | Descripción |
|-------|-------------|
| **Estado de Cuenta** | Badge con el estado actual: `Active`, `PaymentDue`, `PaymentDueUnknown`, `Disabled`, `Demo` o `DemoExpired`. |
| **Validez** | Si la licencia se considera válida actualmente (`Válido` / `Inválido`). |
| **Días Hasta Expiración** | Días restantes en la licencia, o `Expirado` si el período ha vencido. |
| **Período de Gracia** | `Sí` si la cuenta está dentro de un período de gracia post-expiración; `No` en caso contrario. |
| **Fecha de Expiración** | Fecha en que expira la licencia (mostrada cuando está disponible). |
| **Fecha de Expiración Demo** | Mostrada solo en cuentas demo. |
| **Última Validación** | Marca de tiempo de la verificación de licencia más reciente contra la nube. |

### Actualizar

Toque **Actualizar** (esquina superior derecha de la tarjeta) para volver a obtener los últimos datos de licencia del servidor sin salir de la página.

### Advertencias

Si la cuenta está próxima a expirar o tiene un problema de facturación, aparece un banner de advertencia amarillo dentro de la tarjeta:

| Advertencia | Causa |
|-------------|-------|
| El pago está vencido | La facturación está atrasada — contacte soporte. |
| La cuenta expira en N días | La licencia se acerca a su fecha de vencimiento. |
| Estado de pago desconocido | La verificación en la nube devolvió un estado de facturación ambiguo. |
| Problema de licencia detectado | Problema general de licencia — contacte soporte. |

---

## Tarjeta Estado de Cola

La tarjeta inferior muestra el estado de la cola de eventos local que alimenta el servicio en la nube de EZY. Su color indica la salud de la cola:

| Color | Significado |
|-------|-------------|
| Borde verde | Todos los eventos sincronizados, nube accesible |
| Borde amarillo | Eventos pendientes de sincronización |
| Borde rojo | Servicio en la nube no accesible |

### Campos

| Campo | Descripción |
|-------|-------------|
| **Estado** | `Sincronizado`, `Pendiente` o `Desconectado`. |
| **Eventos Pendientes** | Cantidad de eventos en cola localmente y aún no enviados a la nube. |
| **Servicio en la Nube** | Punto verde = disponible; punto rojo = no disponible. |
| **Última Verificación** | Marca de tiempo de la verificación de conectividad más reciente. |

### Sincronizar Forzado

Cuando el servicio en la nube está disponible, toque **Sincronizar Forzado** para enviar inmediatamente todos los eventos en cola en lugar de esperar el siguiente ciclo automático. El botón se deshabilita cuando la nube no es accesible.

### Banner Servicio No Disponible

Si la nube no es accesible, un cuadro de alerta rojo explica que los eventos se están reteniendo localmente y el sistema reintentará automáticamente. No se requiere acción manual — los eventos no se pierden.

---

## Páginas Relacionadas

- [Resumen de Configuración](/docs/settings/index)
- [Dispositivos](/docs/settings/devices)
