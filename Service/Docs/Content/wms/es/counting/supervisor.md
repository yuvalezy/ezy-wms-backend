---
title: Supervisor de Conteo
description: Crear, monitorear y cerrar conteos de inventario.
---

La página de Supervisor de Conteo se utiliza para crear nuevos conteos de inventario, monitorear su progreso, revisar reportes y cerrarlos o cancelarlos.

## Acceso a la Página

Ruta: `/countingSupervisor`

Navegación: Panel Principal → Supervisor de Conteo

## Crear un Nuevo Conteo

1. Vaya a la página de Supervisor de Conteo.
2. En el campo **Identificación**, ingrese un nombre interno (ej. "Conteo Pasillo 3").
3. Haga clic en **Crear**.
4. El conteo ahora está disponible para que los operarios trabajen.

## Lista de Conteos

La vista de supervisor muestra todos los conteos con:

| Columna | Descripción |
|---------|-------------|
| Número | ID del conteo |
| Identificación | Nombre interno |
| Estado | Abierto, En Proceso, Finalizado, Cancelado |
| Creado Por | Empleado que creó el conteo |
| Fecha | Fecha de creación |

## Acciones por Fila

Haga clic en el menú de tres puntos para acceder a:

| Acción | Descripción |
|--------|-------------|
| Reporte Resumen | Ver todas las ubicaciones y artículos escaneados |
| Finalizar | Cerrar el conteo y enviar datos a SAP |
| Cancelar | Eliminar el conteo y todos los datos escaneados |

## Finalizar un Conteo

1. Revise el **Reporte Resumen** para verificar la completitud.
2. Haga clic en **Finalizar** y confirme.
3. El conteo se cierra.

⚠️ **Importante:** Finalizar el conteo en WMS no actualiza aún el inventario en SAP. Se requiere un paso adicional en SAP. Consulte [Reporte Resumen](/docs/counting/summary-report).

## Cancelar un Conteo

Para cancelar un conteo:

1. Haga clic en el menú de tres puntos.
2. Seleccione **Cancelar**.
3. Confirme la acción.

⚠️ **Advertencia:** La cancelación no se puede deshacer. Todos los datos escaneados se pierden.

## Filtrado

Use filtros para encontrar conteos específicos por estado o rango de fechas.

## Páginas Relacionadas

- [Proceso de Conteo](/docs/counting/process)
- [Reporte Resumen](/docs/counting/summary-report)
