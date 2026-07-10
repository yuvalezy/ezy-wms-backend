---
title: Confirmación de Entrada
description: Resumen del módulo de confirmación de entrada.
---

El módulo de Confirmación de Entrada se utiliza para confirmar y validar mercancía que ha sido recibida. Sigue el mismo flujo de trabajo que la Entrada de Mercancía pero con enfoque en confirmación.

## Flujo de Trabajo

1. El **supervisor** crea una entrada de confirmación.
2. Los **operarios** escanean y confirman los artículos recibidos.
3. El **supervisor** revisa los reportes de confirmación y cierra la entrada.

## Páginas

| Página | Ruta | Descripción |
|--------|------|-------------|
| Lista de Confirmaciones | `/goodsReceiptConfirmation` | Lista de confirmaciones abiertas |
| Proceso de Confirmación | `/goodsReceiptConfirmation/:scanCode` | Escanear y confirmar artículos |
| Supervisor de Confirmación | `/goodsReceiptConfirmationSupervisor` | Crear y monitorear confirmaciones |
| Reporte de Confirmación | `/goodsReceiptConfirmationReport` | Ver cantidades confirmadas |
| Reporte vs Salida | `/goodsReceiptConfirmationVSExitReport/:scanCode` | Comparar confirmación vs salida |
| Reporte de Diferencias | `/goodsReceiptConfirmationProcessDifferenceReport/:scanCode` | Ver discrepancias |
| Reporte de Todos los Artículos | `/goodsReceiptConfirmationReportAll/:scanCode` | Reporte detallado completo |

## Roles

| Rol | Permisos |
|-----|----------|
| Confirmación de Entrada | Escanear y confirmar artículos |
| Supervisor de Confirmación de Entrada | Crear entradas, ver reportes, cerrar entradas |

## Páginas Relacionadas

- [Entrada de Mercancía](/docs/goods-receipt/index)
- [Confirmación de Transferencia](/docs/transfer-confirmation/index)
