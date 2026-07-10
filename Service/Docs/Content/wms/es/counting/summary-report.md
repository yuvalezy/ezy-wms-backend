---
title: Reporte Resumen de Conteo
description: Revisar y finalizar datos de conteo de inventario.
---

El Reporte Resumen de Conteo proporciona una vista completa de todas las ubicaciones y artículos escaneados durante un conteo, permitiendo a los supervisores validar los datos antes de finalizar.

## Acceso a la Página

Ruta: `/countingSummaryReport/:scanCode`

Navegación: Supervisor de Conteo → Menú de tres puntos → Reporte Resumen

## Contenido del Reporte

El reporte resumen muestra:

| Columna | Descripción |
|---------|-------------|
| Ubicación | Código de ubicación escaneada |
| Código Artículo | Código del producto |
| Nombre Artículo | Descripción del producto |
| Cantidad Contada | Cantidad físicamente contada |
| Unidad | Unidad de medida |
| Cantidad Documento | Cantidad esperada de SAP |
| Diferencia | Variación |

## Mostrar Solo Diferencias

Active el filtro **Mostrar solo diferencias** para mostrar solo las líneas donde la cantidad contada difiere de la cantidad del documento.

## Finalizar en SAP

Después de finalizar el conteo en WMS, debe contabilizarlo en SAP:

1. En SAP, navegue a **Inventario → Operaciones de stock → Recuento de inventario**.
2. Haga clic en el ícono verde de retroceso para ver los últimos conteos.
3. Verifique que el estado del conteo esté **Abierto**.
4. Revise los detalles y ajuste cantidades si es necesario.
5. Haga clic en **Copiar a contabilización de stock**.
6. Acepte y apruebe el proceso.

Una vez aprobado, el sistema actualiza oficialmente el inventario tanto en SAP como en WMS.

## Verificación de Conteos Abiertos

Antes de iniciar nuevos conteos, los supervisores deben revisar los conteos abiertos en SAP:

1. Filtre por estado "Abierto" en el módulo de conteo de SAP.
2. Finalice o cancele cualquier conteo pendiente.
3. Esto evita confusiones, errores de inventario o duplicaciones.

## Páginas Relacionadas

- [Supervisor de Conteo](/docs/counting/supervisor)
- [Proceso de Conteo](/docs/counting/process)
