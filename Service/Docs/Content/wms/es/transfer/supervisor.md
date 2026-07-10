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
| Finalizar | Cerrar la transferencia y actualizar inventario |
| Cancelar | Eliminar la transferencia y todos los datos escaneados |
| Detalles | Ver detalles de la transferencia |

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

Filtre transferencias por estado, rango de fechas o tipo de documento.

## Páginas Relacionadas

- [Proceso de Transferencia](/docs/transfer/process)
- [Aprobación de Transferencia](/docs/transfer/approval)
