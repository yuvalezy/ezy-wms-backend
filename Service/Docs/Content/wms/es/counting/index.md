---
title: Conteo de Inventario
description: Resumen del módulo de conteo de inventario.
---

El módulo de Conteo de Inventario se utiliza para realizar conteos físicos de inventario, ya sea de toda la bodega o de ubicaciones específicas como estanterías o racks.

## Flujo de Trabajo

1. El **supervisor** crea una nueva entrada de conteo con un nombre de identificación interno.
2. Los **operarios** acceden al conteo y escanean ubicaciones y artículos.
3. Se pueden contar múltiples ubicaciones dentro de la misma entrada de conteo.
4. El **supervisor** revisa el reporte resumen y cierra el conteo.
5. En SAP, el conteo se contabiliza para actualizar el inventario.

## Páginas

| Página | Ruta | Descripción |
|--------|------|-------------|
| [Lista de Conteos](/docs/counting/process) | `/counting` | Lista de conteos abiertos |
| [Proceso de Conteo](/docs/counting/process) | `/counting/:scanCode` | Escanear ubicaciones y contar artículos |
| [Supervisor](/docs/counting/supervisor) | `/countingSupervisor` | Crear y monitorear conteos |
| [Reporte Resumen](/docs/counting/summary-report) | `/countingSummaryReport/:scanCode` | Revisar resumen del conteo |

## Roles

| Rol | Permisos |
|-----|----------|
| Conteo | Escanear ubicaciones y contar artículos |
| Supervisor de Conteo | Crear conteos, ver reportes, cerrar conteos |

## Notas Importantes

- Los artículos que han sido contados no pueden moverse sin un proceso formal.
- Use el módulo de [Transferencia](/docs/transfer/index) para mover productos entre ubicaciones.
- Use el módulo de [Picking](/docs/picking/index) para retirar mercancía con fines de despacho.
- Cualquier movimiento físico no registrado puede generar diferencias de inventario.

## Páginas Relacionadas

- [Proceso de Conteo](/docs/counting/process)
- [Supervisor de Conteo](/docs/counting/supervisor)
