---
title: Solicitud de Transferencia
description: Crear solicitudes formales de transferencia de inventario.
---

La página de Solicitud de Transferencia permite a los usuarios crear solicitudes formales para transferencias de inventario entre ubicaciones o almacenes.

## Acceso a la Página

Ruta: `/transferRequest`

Navegación: Panel Principal → Solicitud de Transferencia

## Crear una Solicitud de Transferencia

1. Haga clic en **Crear Solicitud**.
2. Complete los datos de la solicitud:
   - **Almacén Origen** — donde se encuentra actualmente el inventario.
   - **Almacén Destino** — donde debe moverse el inventario.
   - **Artículos** — agregue los artículos a transferir:
     - Escanee o ingrese el código del artículo.
     - Ingrese la cantidad.
     - Seleccione la unidad.
3. Haga clic en **Solicitar Aprobación** para enviar.

## Estado de la Solicitud

| Estado | Descripción |
|--------|-------------|
| Pendiente de Aprobación | Esperando aprobación del supervisor |
| Aprobado | La solicitud ha sido autorizada |
| Rechazado | La solicitud ha sido denegada |
| Cancelado | La solicitud fue cancelada por el solicitante |

## Mensajes

| Mensaje | Significado |
|---------|-------------|
| Solicitud de transferencia creada | La solicitud fue enviada exitosamente |
| Transferencia enviada para aprobación | La solicitud está pendiente de autorización |

## Páginas Relacionadas

- [Aprobación de Transferencia](/docs/transfer/approval)
- [Proceso de Transferencia](/docs/transfer/process)
