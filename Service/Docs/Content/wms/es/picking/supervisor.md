---
title: Supervisor de Picking
description: Monitorear y gestionar operaciones de picking.
---

La página de Supervisor de Picking proporciona un resumen de todos los pickings y herramientas para monitorear el progreso, ejecutar verificaciones y gestionar asignaciones.

## Acceso a la Página

Ruta: `/pickSupervisor`

Navegación: Panel Principal → Supervisor de Picking

## Lista de Picking

La vista de supervisor muestra todos los pickings con:

| Columna | Descripción |
|---------|-------------|
| Número | ID del picking |
| Tipo | Tipo de documento (Orden de Venta, Transferencia, etc.) |
| Número de Documento | Documento fuente de SAP |
| Cliente / Proveedor | Nombre del socio de negocio |
| Estado | Abierto, En Proceso, Finalizado, Cancelado |
| Total Artículos | Total de líneas en el picking |
| Artículos Abiertos | Líneas restantes por separar |
| Progreso | Porcentaje completado |

## Acciones por Fila

Haga clic en el menú de tres puntos para acceder a:

| Acción | Descripción |
|--------|-------------|
| Ver Verificación | Ver o iniciar una verificación de picking |
| Reiniciar Verificación | Reiniciar el estado de verificación |
| Detalles | Ver detalles completos del picking |

## Búsqueda y Filtro

- **Búsqueda** — encuentre pickings por número, orden, factura, transferencia o comentarios.
- **Filtro de estado** — muestre solo pickings con un estado específico.

## Gestión de Verificaciones

Los supervisores pueden iniciar verificaciones de picking para asegurar precisión:

1. Encuentre el picking en la lista.
2. Haga clic en **Iniciar Verificación**.
3. Siga el proceso de [Verificación de Picking](/docs/picking/picking-check).

## Mensajes de Error

| Mensaje | Causa |
|---------|-------|
| No hay datos de picking | No hay pickings pendientes |
| No hay resultados de búsqueda | El término de búsqueda no coincide con ningún picking |

## Páginas Relacionadas

- [Proceso de Picking](/docs/picking/process)
- [Verificación de Picking](/docs/picking/picking-check)
