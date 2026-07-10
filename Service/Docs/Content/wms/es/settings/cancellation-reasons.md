---
title: Motivos de Cancelación
description: Configure los códigos de motivo que los operadores deben seleccionar al cancelar entradas de mercancía, transferencias o conteos de inventario.
---

Los Motivos de Cancelación son etiquetas definidas por el administrador que los operadores seleccionan cuando cancelan una línea o documento en el almacén. Cada motivo puede habilitarse para uno o más tipos de proceso, de modo que solo aparezcan las opciones relevantes durante un flujo de trabajo determinado.

## Acceso a la Página

Navegue a **Configuración → Motivos de Cancelación** o acceda directamente a `/settings/cancelReasons`.

Esta página está restringida a cuentas de **super usuario**.

## Lista de Motivos de Cancelación

La lista muestra todos los motivos configurados con las siguientes columnas:

| Columna | Descripción |
|---------|-------------|
| **Razón** | Nombre del motivo de cancelación |
| **Estado** | Activo o Inactivo |
| **Entrada** | Si el motivo está disponible al cancelar entradas de mercancía |
| **Transferencia** | Si el motivo está disponible al cancelar transferencias |
| **Conteo** | Si el motivo está disponible al cancelar conteos de inventario |
| **Acciones** | Botones Editar y Eliminar |

![Lista de motivos de cancelación con dos motivos activos y sus indicadores de estado](/docs/wms/settings/cancellation-reasons/cancellation-reasons-list-es.png)

**Opciones de búsqueda y filtro:**

- **Barra de búsqueda** — Filtra los motivos por nombre en tiempo real.
- **Incluir Inactivos** — Cuando está marcado, se muestran también los motivos deshabilitados.

## Crear un Motivo de Cancelación

1. Toque el botón **+** en la esquina superior derecha de la página.
2. Complete el formulario **Agregar Motivo de Cancelación**:

![Diálogo Agregar Motivo de Cancelación con campo Nombre y casillas de proceso](/docs/wms/settings/cancellation-reasons/cancellation-reasons-add-es.png)

| Campo | Descripción |
|-------|-------------|
| **Nombre** \* | Obligatorio. Entre 2 y 100 caracteres. |
| **Está Activo** | Cuando está marcado, el motivo está disponible para selección por los operadores. |
| **Entrada** | Habilita el motivo para cancelaciones de entrada de mercancía. |
| **Transferencia** | Habilita el motivo para cancelaciones de transferencias. |
| **Conteo** | Habilita el motivo para cancelaciones de conteos de inventario. |

3. Toque **Guardar** para crear el motivo, o **Cancelar** para descartar.

## Editar un Motivo de Cancelación

1. Localice el motivo en la lista y toque **Editar**.
2. El diálogo **Editar Motivo de Cancelación** se abre con los valores actuales precargados.

![Diálogo Editar Motivo de Cancelación con los valores existentes precargados](/docs/wms/settings/cancellation-reasons/cancellation-reasons-edit-es.png)

3. Modifique cualquier campo y toque **Guardar**, o toque **Cancelar** para descartar.

## Eliminar un Motivo de Cancelación

1. Localice el motivo en la lista y toque **Eliminar**.
2. Aparece un diálogo de confirmación:

![Diálogo de confirmación de eliminación con advertencia de que la acción no se puede deshacer](/docs/wms/settings/cancellation-reasons/cancellation-reasons-delete-es.png)

3. Toque **Eliminar** para borrar permanentemente el motivo, o **Cancelar** para abortar.

> **Advertencia:** La eliminación es permanente y no se puede deshacer.

## Páginas Relacionadas

- [Resumen de Configuración](/docs/settings/index)
