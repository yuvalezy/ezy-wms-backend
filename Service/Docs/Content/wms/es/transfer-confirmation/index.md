---
title: Confirmación de Transferencia
description: Resumen del módulo de confirmación de transferencia.
---

El módulo de Confirmación de Transferencia se utiliza para confirmar y validar transferencias de inventario completadas, particularmente para movimientos entre almacenes. Reutiliza los mismos componentes que la Confirmación de Entrada pero con enfoque en transferencias.

## Flujo de Trabajo

1. El **supervisor** crea una entrada de confirmación de transferencia.
2. Los **operarios** escanean y confirman los artículos transferidos en el almacén de destino.
3. El **supervisor** revisa los reportes y cierra la confirmación.

## Páginas

| Página | Ruta | Descripción |
|--------|------|-------------|
| Lista de Confirmaciones | `/transferConfirmation` | Lista de confirmaciones abiertas |
| Proceso de Confirmación | `/transferConfirmation/:scanCode` | Escanear y confirmar artículos |
| Supervisor de Confirmación | `/transferConfirmationSupervisor` | Crear y monitorear confirmaciones |
| Reporte de Confirmación | `/transferConfirmationReport` | Ver cantidades confirmadas |
| Reporte de Diferencias | `/transferConfirmationProcessDifferenceReport/:scanCode` | Ver discrepancias |
| Reporte de Todos los Artículos | `/transferConfirmationReportAll/:scanCode` | Reporte detallado completo |

## Roles

| Rol | Permisos |
|-----|----------|
| Confirmación de Transferencia | Escanear y confirmar artículos transferidos |
| Supervisor de Confirmación de Transferencia | Crear entradas, ver reportes, cerrar entradas |

## Páginas Relacionadas

- [Transferencia](/docs/transfer/index)
- [Confirmación de Entrada](/docs/receipt-confirmation/index)
