---
title: Detalle de Picking
description: Separar una línea específica de una orden de picking.
---

La página de Detalle de Picking permite a los operarios enfocarse en un solo artículo dentro de un picking, mostrando todas las ubicaciones y cantidades disponibles para ese artículo.

## Acceso a la Página

Ruta: `/pick/:idParam/:typeParam/:entryParam`

Navegación: Proceso de Picking → Hacer clic en una línea de artículo específica

## Secciones de la Página

### Ubicaciones Disponibles

Muestra todas las ubicaciones donde se almacena el artículo, con:

| Campo | Descripción |
|-------|-------------|
| Ubicación | Código de ubicación en almacén |
| Cantidad Disponible | Cantidad disponible para separar |
| Unidad | Unidad de medida |

### Detalles del Artículo

Muestra el código, nombre y cantidad abierta total del artículo para la línea de picking.

### Ubicaciones

Muestra la ubicación sugerida y ubicaciones alternativas desde donde se puede separar el artículo.

## Cómo Usar

1. Desde la página de Proceso de Picking, haga clic en una línea de artículo.
2. La página de detalle muestra todas las ubicaciones disponibles.
3. Escanee o seleccione la **ubicación** desde donde está separando.
4. Escanee el **código de barra** del artículo.
5. Ingrese la **cantidad**.
6. Seleccione la **unidad**.
7. Haga clic en **Aceptar**.

## Páginas Relacionadas

- [Proceso de Picking](/docs/picking/process)
- [Supervisor de Picking](/docs/picking/supervisor)
