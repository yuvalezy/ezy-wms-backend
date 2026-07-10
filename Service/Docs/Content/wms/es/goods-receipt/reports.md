---
title: Reportes de Entradas
description: Revisar y analizar datos de entradas de mercancía.
---

El módulo de Entrada de Mercancía proporciona varios reportes para ayudar a los supervisores a validar y cerrar entradas con precisión.

## Tipos de Reporte

### Reporte de Entradas

Ruta: `/goodsReceiptReport`

Muestra todos los artículos escaneados para una entrada seleccionada con:

| Columna | Descripción |
|---------|-------------|
| Número de Línea | Línea del documento |
| Código Artículo | Código del producto |
| Nombre Artículo | Descripción del producto |
| Cantidad Documento | Cantidad esperada de SAP |
| Cantidad Escaneada | Cantidad recibida en WMS |
| Unidad | Unidad de medida |
| Ubicación | Ubicación de destino |
| Comentario | Comentarios del operario |

### Reporte vs Salida

Ruta: `/goodsReceiptVSExitReport/:scanCode`

Compara cantidades recibidas contra documentos de salida para detectar anomalías.

### Reporte de Diferencias

Ruta: `/goodsReceiptProcessDifferenceReport/:scanCode`

Resalta discrepancias entre el documento fuente y las cantidades escaneadas:

| Estado | Significado |
|--------|-------------|
| Completo | La cantidad coincide exactamente |
| Menos de lo ordenado | Se recibió menos de lo esperado |
| Más de lo ordenado | Se recibió más de lo esperado |
| No Recibido | El artículo no fue escaneado |

### Reporte de Todos los Artículos

Ruta: `/goodsReceiptReportAll/:scanCode`

Proporciona detalle completo de todos los artículos en la entrada, incluyendo líneas que no fueron escaneadas.

## Filtrado de Reportes

Todos los reportes admiten:

- **Rango de fechas** — filtrar por fechas Desde/Hasta.
- **Mostrar solo diferencias** — mostrar solo líneas con discrepancias.
- **Tipo de documento** — filtrar por tipo de documento fuente.

## Exportación

Los reportes pueden exportarse a Excel para análisis externo.

## Páginas Relacionadas

- [Supervisor de Entradas](/docs/goods-receipt/supervisor)
- [Proceso de Entrada](/docs/goods-receipt/process)
