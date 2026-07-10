---
title: Entrada de Mercancía
description: Resumen del módulo de entrada de mercancía.
---

El módulo de Entrada de Mercancía se utiliza para recibir y procesar mercancía entrante. Soporta recepciones contra órdenes de compra, facturas de compra y otros tipos de documentos.

## Flujo de Trabajo

1. El **supervisor** crea una nueva entrada de mercancía y vincula documentos fuente.
2. Los **operarios** escanean y reciben mercancía usando la página de proceso.
3. El **supervisor** revisa reportes, ajusta cantidades si es necesario y cierra la entrada.

## Páginas

| Página | Ruta | Descripción |
|--------|------|-------------|
| [Lista de Entradas](/docs/goods-receipt/process) | `/goodsReceipt` | Lista de entradas abiertas para operarios |
| [Proceso de Entrada](/docs/goods-receipt/process) | `/goodsReceipt/:scanCode` | Escanear y recibir artículos |
| [Supervisor](/docs/goods-receipt/supervisor) | `/goodsReceiptSupervisor` | Crear, monitorear y cerrar entradas |
| [Reportes](/docs/goods-receipt/reports) | `/goodsReceiptReport` | Ver cantidades escaneadas |
| [Reporte vs Salida](/docs/goods-receipt/reports) | `/goodsReceiptVSExitReport/:scanCode` | Comparar entrada vs salida |
| [Reporte de Diferencias](/docs/goods-receipt/reports) | `/goodsReceiptProcessDifferenceReport/:scanCode` | Ver discrepancias |
| [Reporte de Todos los Artículos](/docs/goods-receipt/reports) | `/goodsReceiptReportAll/:scanCode` | Reporte detallado completo |

## Tipos de Documento Soportados

- Orden de Compra
- Factura de Compra
- Entrada de Mercancía OC
- Transferencia de Inventario

## Roles

| Rol | Permisos |
|-----|----------|
| Entrada de Mercancía | Escanear y recibir artículos |
| Supervisor de Entradas | Crear entradas, ver reportes, cerrar entradas |

## Páginas Relacionadas

- [Confirmación de Entrada](/docs/receipt-confirmation/index)
- [Transferencia](/docs/transfer/index)
