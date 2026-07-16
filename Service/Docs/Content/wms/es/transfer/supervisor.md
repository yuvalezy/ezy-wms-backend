---
title: Supervisor de Transferencia
description: Crear, monitorear y finalizar transferencias de inventario.
---

La página de Supervisor de Transferencia se utiliza para crear nuevas entradas de transferencia, monitorear su progreso y finalizarlas o cancelarlas.

## Acceso a la Página

Ruta: `/transferSupervisor`

Navegación: Panel Principal → Supervisor de Transferencia

## Crear una Nueva Transferencia

1. Haga clic en **Crear**.
2. Complete los datos de la transferencia:
   - **Identificación** — nombre interno (ej. "Reorganización Zona A").
   - **Comentario** — información adicional opcional.
   - **Almacén Destino** — solo para transferencias entre almacenes.
3. Haga clic en **Crear**.
4. La transferencia ahora está disponible para los operarios.

## Lista de Transferencias

La vista de supervisor muestra todas las transferencias con:

| Columna | Descripción |
|---------|-------------|
| Número | ID de la transferencia |
| Identificación | Nombre interno |
| Estado | Abierto, En Proceso, Finalizado, Cancelado, Esperando Aprobación |
| Creado Por | Empleado que creó la transferencia |
| Fecha | Fecha de creación |

## Acciones por Fila

Haga clic en el menú de tres puntos para acceder a:

| Acción | Descripción |
|--------|-------------|
| Reporte Transferencia | Revisar qué movió la transferencia, artículo por artículo |
| Finalizar | Cerrar la transferencia y actualizar inventario — solo cuando la transferencia está 100% completa |
| Cancelar | Eliminar la transferencia y todos los datos escaneados |

Para abrir una transferencia y escanear, haga clic en su **Número**.

## Finalizar una Transferencia

1. Verifique que todos los artículos han sido movidos.
2. Haga clic en **Finalizar** y confirme.
3. El sistema confirma que la mercancía ha sido trasladada y actualiza las ubicaciones en SAP y WMS.

## Cancelar una Transferencia

1. Haga clic en el menú de tres puntos.
2. Seleccione **Cancelar**.
3. Confirme la acción.

⚠️ **Advertencia:** La cancelación no se puede deshacer.

## Filtrado

La lista muestra por defecto las transferencias **Abiertas**, **En Proceso** y **Esperando Aprobación**.

Haga clic en el icono de embudo en el encabezado de la página para filtrar por estado. Marque **Finalizado**, **Rechazado** o **Cancelado** para incluir transferencias cerradas en la lista — así es como se accede al [Reporte Transferencia](/docs/transfer/summary-report) de una transferencia ya finalizada. **Borrar** devuelve la lista a sus estados predeterminados.

## Páginas Relacionadas

- [Reporte Transferencia](/docs/transfer/summary-report)
- [Proceso de Transferencia](/docs/transfer/process)
- [Aprobación de Transferencia](/docs/transfer/approval)
