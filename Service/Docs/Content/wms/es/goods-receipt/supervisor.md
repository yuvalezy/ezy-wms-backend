---
title: Supervisor de Entradas
description: Crear, monitorear y cerrar entradas de mercancía.
---

La página de Supervisor de Entradas se utiliza para crear nuevas entradas, monitorear el progreso, revisar reportes y cerrar o cancelar entradas.

## Acceso a la Página

Ruta: `/goodsReceiptSupervisor`

Navegación: Panel Principal → Supervisor de Entradas

## Crear una Nueva Entrada

1. Haga clic en **Crear** o el botón de agregar.
2. Complete los datos de la entrada:
   - **Identificación** — nombre de texto libre para uso interno (ej. "Entrada Abril 23").
   - **Tipo de Documento** — seleccione el tipo de documento fuente:
     - Orden de Compra
     - Factura de Compra
     - Entrada de Mercancía OC
   - **Número de Documento** — ingrese el número de documento de SAP.
3. Haga clic en **Agregar** para vincular el documento.
4. Repita los pasos 2–3 para agregar múltiples documentos a la misma entrada.
5. Haga clic en **Crear** para generar la entrada.

## Lista de Entradas

La vista de supervisor muestra todas las entradas con:

| Columna | Descripción |
|---------|-------------|
| Número | ID de la entrada |
| Identificación | Nombre interno |
| Tipo de Documento | Tipo de documento fuente |
| Estado | Abierto, En Proceso, Finalizado, Cancelado |
| Creado Por | Empleado que creó la entrada |
| Fecha | Fecha de creación |

## Acciones por Fila

Haga clic en el menú de tres puntos junto a cualquier entrada para acceder a:

| Acción | Descripción |
|--------|-------------|
| Reporte | Ver cantidades escaneadas y detalles |
| Reporte vs Salida | Comparar cantidades recibidas vs documentos de salida |
| Reporte de Diferencias | Ver discrepancias entre escaneado y documento |
| Reporte de Todos los Artículos | Detalle completo de todos los artículos |
| Finalizar | Cerrar la entrada y finalizar |
| Cancelar | Eliminar la entrada y todos los datos escaneados |

## Finalizar una Entrada

Antes de finalizar:

1. Revise el **Reporte** para asegurar que todos los artículos están escaneados correctamente.
2. Verifique el **Reporte de Diferencias** por discrepancias.
3. Haga clic en **Finalizar** y confirme.
4. La entrada se cierra y los datos se envían a SAP.

## Cancelar una Entrada

Para cancelar una entrada:

1. Haga clic en el menú de tres puntos.
2. Seleccione **Cancelar**.
3. Confirme la acción.

⚠️ **Advertencia:** La cancelación no se puede deshacer. Todos los datos escaneados se pierden.

## Filtrado

Use filtros para encontrar entradas específicas:

| Filtro | Opciones |
|--------|----------|
| Estado | Todos, Abierto, En Proceso, Finalizado, Cancelado |
| Rango de Fechas | Desde / Hasta |
| Tipo de Documento | Orden de Compra, Factura de Compra, etc. |

## Páginas Relacionadas

- [Proceso de Entrada](/docs/goods-receipt/process)
- [Reportes](/docs/goods-receipt/reports)
