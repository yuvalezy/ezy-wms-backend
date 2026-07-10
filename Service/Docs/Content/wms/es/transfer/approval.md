---
title: Aprobación de Transferencia
description: Aprobar o rechazar transferencias entre almacenes.
---

La página de Aprobación de Transferencia es utilizada por supervisores para revisar y aprobar o rechazar transferencias entre almacenes que requieren autorización.

## Acceso a la Página

Ruta: `/transfer/approve`

Navegación: Panel Principal → Aprobación de Transferencia

## Lista de Aprobaciones

La página muestra todas las transferencias esperando aprobación con:

| Columna | Descripción |
|---------|-------------|
| Número | ID de la transferencia |
| Almacén Origen | Almacén de origen |
| Almacén Destino | Almacén de destino |
| Solicitado Por | Empleado que creó la transferencia |
| Solicitado El | Fecha y hora de la solicitud |
| Estado | Pendiente de Aprobación |

## Acciones

Para cada transferencia, los supervisores pueden:

| Acción | Descripción |
|--------|-------------|
| **Aprobar** | Autorizar que la transferencia proceda |
| **Rechazar** | Denegar la transferencia |

### Aprobar una Transferencia

1. Haga clic en **Aprobar** junto a la transferencia.
2. Confirme la acción.
3. La transferencia es aprobada y el operario puede continuar.

### Rechazar una Transferencia

1. Haga clic en **Rechazar** junto a la transferencia.
2. Ingrese una **razón de rechazo** (requerida).
3. Confirme la acción.
4. La transferencia es rechazada y no puede proceder.

## Detalle de Aprobación

Ruta: `/transfer/approve/:id`

Haga clic en un número de transferencia para ver los detalles completos antes de decidir:

- Almacenes de origen y destino
- Artículos siendo transferidos
- Cantidades y unidades
- Información del solicitante

## Mensajes

| Mensaje | Significado |
|---------|-------------|
| Transferencia aprobada exitosamente | La transferencia ha sido autorizada |
| Transferencia rechazada exitosamente | La transferencia ha sido denegada |
| No hay transferencias esperando aprobación | No hay aprobaciones pendientes |
| Esta transferencia entre almacenes requiere aprobación de supervisor | La transferencia fue enviada para aprobación |

## Páginas Relacionadas

- [Proceso de Transferencia](/docs/transfer/process)
- [Supervisor de Transferencia](/docs/transfer/supervisor)
