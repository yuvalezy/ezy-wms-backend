---
title: Proceso de Picking
description: Escanear y separar artículos para órdenes de salida.
---

La página de Proceso de Picking es donde los operarios escanean y recolectan artículos desde las ubicaciones de la bodega.

## Acceso a la Página

Ruta: `/pick/:idParam`

Navegación: Panel Principal → Picking → Seleccionar Número de Picking

## Cómo Usar

1. Desde la lista de Picking, haga clic en el número que desea trabajar.
2. El sistema muestra una lista de artículos con ubicaciones sugeridas.
3. Para cada artículo:
   - Escanee o navegue a la **ubicación**.
   - Escanee el **código de barra** del artículo.
   - Confirme o modifique la **cantidad**.
   - Seleccione el **tipo de unidad** (caja, docena, unidad).
   - Haga clic en **Aceptar**.
4. Haga clic en **Picking** para regresar a la lista y continuar con otros artículos.

## Columnas de la Lista de Picking

| Columna | Descripción |
|---------|-------------|
| Código Artículo | Código del producto |
| Nombre Artículo | Descripción del producto |
| Ubicación | Ubicación sugerida para separar |
| Cantidad Abierta | Cantidad restante por separar |
| Cantidad Separada | Cantidad ya separada |
| Unidad | Unidad de medida |

## Gestión de Paquetes

Puede separar artículos en paquetes:

- **Escanear Paquete** — asocie artículos con un paquete existente.
- **Nuevo Paquete** — cree un nuevo paquete para el picking actual.
- El sistema rastrea en qué paquete se colocó cada artículo.

## Completar un Picking

Un picking se considera completo cuando todas las cantidades abiertas llegan a cero. En ese momento:

- El picking desaparece de la lista del WMS.
- SAP se actualiza automáticamente.
- El Departamento de Tráfico puede continuar con la creación de la entrega.

## Mensajes de Error

| Mensaje | Causa |
|---------|-------|
| Picking no válido | El número de picking no existe |
| Picking cerrado | El picking ya ha sido completado |
| Error de estado de picking | El picking no está en un estado procesable |
| Artículo no encontrado en picking | El artículo escaneado no está en la lista |
| Error de solo paquetes cerrados | El cliente solo acepta paquetes cerrados |

## Páginas Relacionadas

- [Detalle de Picking](/docs/picking/detail)
- [Supervisor de Picking](/docs/picking/supervisor)
