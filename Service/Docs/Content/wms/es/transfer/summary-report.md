---
title: Reporte Transferencia
description: Revisar qué movió una transferencia, artículo por artículo, con ubicaciones origen y destino.
---

El Reporte Transferencia es la vista del supervisor sobre lo que una transferencia realmente movió. Muestra una fila por artículo: qué se tomó de las ubicaciones origen, dónde se colocó en las ubicaciones destino, y qué queda pendiente entre ambas.

Úselo para revisar una transferencia en cualquier momento — incluso después de finalizada, cuando la pantalla de escaneo ya no muestra nada.

## Acceso a la Página

Ruta: `/transferSummaryReport/:id`

Navegación: Panel Principal → Supervisor de Transferencia → menú de tres puntos en una transferencia → **Reporte Transferencia**

En móvil, el botón **Reporte Transferencia** aparece al final de cada tarjeta de transferencia.

Requiere el rol **Supervisor de Transferencia**.

## Encabezado del Reporte

| Campo | Descripción |
|-------|-------------|
| Identificación | Nombre interno, si la transferencia lo tiene |
| Fecha | Fecha de creación |
| Estado | Abierto, En Proceso, Esperando Aprobación, Finalizado, Rechazado, Cancelado |
| Almacén Origen | Almacén del que salió la mercancía |
| Almacén Destino | Almacén de destino, para transferencias entre almacenes |
| Creado Por | Empleado que creó la transferencia |
| Doc SAP # | El documento de transferencia de stock en SAP, una vez contabilizado |
| Comentario | El comentario de la transferencia, si tiene |

## Filas de Artículos

| Columna | Descripción |
|---------|-------------|
| Código | Código del artículo — haga clic para ver los detalles |
| Descripción | Nombre del artículo |
| Ubicaciones Origen | Ubicaciones de las que se tomó el artículo, cada una con su cantidad |
| Ubicaciones Destino | Ubicaciones en las que se colocó el artículo, cada una con su cantidad |
| Cantidad | Total tomado del origen |
| Colocado | Total colocado en el destino |
| Pendiente | Cantidad que aún falta colocar |

**Pendiente** se resalta cuando no es cero. En una transferencia en proceso esto es normal — es lo que falta por acomodar. En una transferencia finalizada conviene revisarlo.

Un **Pendiente** negativo significa que se colocó más de lo que se tomó, e indica que ambos lados no coinciden.

Las cantidades siguen su configuración de unidades del módulo Transferencia, por lo que se leen en cajas, docenas y unidades igual que en las pantallas de escaneo.

## Exportar a Excel

Haga clic en el icono verde de hoja de cálculo en el encabezado de la página para descargar el reporte como archivo `.xlsx`, con el nombre `transfer_data_<número>.xlsx`. La exportación contiene las filas de artículos — código, descripción, ubicaciones origen, ubicaciones destino y cantidad — con las columnas de cantidad según su configuración de unidades.

## Encontrar Transferencias Finalizadas

La lista del Supervisor de Transferencia muestra por defecto las transferencias abiertas, en proceso y esperando aprobación. Para reportar sobre una transferencia ya finalizada, abra el filtro (icono de embudo) y marque **Finalizado**.

## Notas

- **Doc SAP #** se registra cuando la transferencia se contabiliza en SAP. Las transferencias contabilizadas antes de que existiera esta función muestran un guion.
- Las líneas canceladas se excluyen del reporte.
- El reporte recalcula las cantidades con los factores de conversión actuales de SAP antes de totalizar, por lo que siempre coincide con las pantallas de escaneo.

## Páginas Relacionadas

- [Supervisor de Transferencia](/docs/transfer/supervisor)
- [Proceso de Transferencia](/docs/transfer/process)
