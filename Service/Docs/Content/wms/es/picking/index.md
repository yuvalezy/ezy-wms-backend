---
title: Picking
description: Resumen del módulo de picking.
---

El módulo de Picking se utiliza para preparar mercancía destinada a despacho. Los operarios recolectan productos desde sus ubicaciones según órdenes de venta, solicitudes de transferencia u otros documentos de salida.

## Flujo de Trabajo

1. El **Departamento de Tráfico** genera pickings en SAP (desde órdenes de venta o directamente).
2. Los pickings se sincronizan automáticamente al WMS.
3. Los **operarios** abren un picking y escanean artículos desde las ubicaciones sugeridas.
4. A medida que se separan artículos, el picking se actualiza en tiempo real con SAP.
5. Cuando alcanza el 100%, el picking desaparece del WMS y se refleja en SAP.

## Páginas

| Página | Ruta | Descripción |
|--------|------|-------------|
| [Lista de Picking](/docs/picking/process) | `/pick` | Lista de pickings pendientes |
| [Proceso de Picking](/docs/picking/process) | `/pick/:idParam` | Escanear y separar artículos |
| [Detalle de Picking](/docs/picking/detail) | `/pick/:idParam/:typeParam/:entryParam` | Detalle de línea específica |
| [Verificación de Picking](/docs/picking/picking-check) | `/pick/:id/check` | Verificar artículos separados |
| [Supervisor](/docs/picking/supervisor) | `/pickSupervisor` | Monitorear y gestionar pickings |

## Tipos de Documento

Los pickings pueden generarse desde:

- Órdenes de Venta
- Solicitudes de Transferencia
- Facturas Reservadas
- Facturas

## Roles

| Rol | Permisos |
|-----|----------|
| Picking | Escanear y separar artículos |
| Supervisor de Picking | Monitorear pickings, gestionar asignaciones, ejecutar verificaciones |

## Páginas Relacionadas

- [Integración SAP de Picking](/docs/picking/sap-integration)
- [Transferencia](/docs/transfer/index)
