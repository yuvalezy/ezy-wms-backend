---
title: Grupos de Autorización
description: Cree y gestione grupos de permisos basados en roles que controlan a qué módulos y funciones pueden acceder los usuarios del WMS.
---

Los Grupos de Autorización definen conjuntos de permisos que luego se asignan a los usuarios. Cada grupo agrupa uno o más roles individuales de tres categorías — Operaciones, Supervisión y Administración — para que un administrador pueda otorgar acceso a flujos de trabajo completos con una sola asignación en lugar de seleccionar roles uno por uno.

:::note
Solo los **Superusuarios** pueden acceder a Configuración → Grupos de Autorización.
:::

## Lista de Grupos de Autorización

![Lista de Grupos de Autorización mostrando las columnas Nombre, Descripción y Autorizaciones](/docs/wms/settings/authorization-groups/authorization-groups-list-es.png)

La lista muestra todos los grupos configurados en el almacén con las siguientes columnas:

| Columna | Descripción |
|---------|-------------|
| **Nombre** | Nombre del grupo. Los grupos del sistema muestran la etiqueta **System**. |
| **Descripción** | Descripción opcional del propósito del grupo. |
| **Autorizaciones** | Categorías de permisos asignadas y la cantidad de roles en cada una. |
| **Creado el** | Fecha en que se creó el grupo. |
| **Acciones** | Botones **Editar** y **Eliminar**. |

:::tip
Los grupos del sistema — como el grupo predeterminado **Bodega** — muestran la etiqueta **System** y no pueden eliminarse. Sin embargo, sí pueden editarse.
:::

## Crear un Grupo de Autorización

Haga clic en el botón **+** (esquina superior derecha) para abrir el formulario de Agregar Grupo de Autorización.

![Formulario Agregar Grupo de Autorización con campos Nombre, Descripción y casillas de roles agrupadas por categoría](/docs/wms/settings/authorization-groups/authorization-groups-form-es.png)

Complete los campos requeridos:

| Campo | Requerido | Notas |
|-------|-----------|-------|
| **Nombre** | Sí | 2–100 caracteres |
| **Descripción** | No | Descripción en texto libre |
| **Autorizaciones** | Sí | Se debe seleccionar al menos un rol |

### Categorías de Roles

Los roles están organizados por categorías. Cada categoría tiene una casilla **Seleccionar Todo** para otorgar todos los roles de esa categoría a la vez.

**Operaciones** — roles para el personal de piso del almacén:

| Rol | Descripción |
|-----|-------------|
| Gestión de Artículos | Gestionar datos de artículos, códigos de barras y metadatos |
| Entrada de Mercancía | Procesar mercancías entrantes y actualizar el inventario |
| Confirmación de Entrada | Confirmar y validar las mercancías recibidas |
| Picking | Ejecutar tareas de recolección a partir de órdenes de picking |
| Verificación de Picking | Verificar y validar los artículos recolectados |
| Conteo de Inventario | Realizar conteos físicos y cíclicos del inventario |
| Transferencia | Mover stock entre ubicaciones o almacenes |
| Transferencia Directa | Ejecutar movimientos directos de stock entre ubicaciones |

**Supervisión** — roles para jefes de equipo y supervisores:

| Rol | Descripción |
|-----|-------------|
| Supervisor de Gestión de Artículos | Supervisar y aprobar cambios en datos de artículos |
| Supervisor de Entrada de Mercancía | Supervisar y aprobar operaciones de entrada |
| Supervisor de Confirmación de Entrada | Supervisar flujos de confirmación de entradas |
| Supervisor de Picking | Monitorear y gestionar las operaciones de picking |
| Supervisor de Conteo | Supervisar sesiones de conteo y aprobar resultados |
| Supervisor de Transferencia | Supervisar y aprobar operaciones de transferencia |
| Supervisor de Confirmación de Transferencia | Aprobar y finalizar confirmaciones de transferencia |

:::note
Los roles que aparecen dependen de la configuración del almacén. Por ejemplo, los roles de Entrada de Mercancía varían según si el almacén usa recepción transaccional o por confirmación, y la Transferencia Directa requiere que las ubicaciones de contenedores estén habilitadas.
:::

Haga clic en **Guardar** para crear el grupo. Haga clic en **Cancelar** para descartar y volver a la lista.

## Editar un Grupo de Autorización

Haga clic en **Editar** en cualquier fila para abrir el formulario de Edición del Grupo de Autorización.

![Formulario de Edición con los roles del grupo existente preseleccionados](/docs/wms/settings/authorization-groups/authorization-groups-edit-es.png)

Todos los campos son editables. Las casillas de roles reflejan las asignaciones actuales — marque o desmarque roles individuales o use **Seleccionar Todo** por categoría. Haga clic en **Guardar** para aplicar los cambios.

## Conceder Acceso a Informes

Los informes usan una concesión separada por informe en lugar de un rol. Cuando existe al menos una definición de informe, aparece una tarjeta **Informes** en el formulario del grupo con una casilla por cada informe.

| Campo | Notas |
|-------|-------|
| **Informes** | Seleccione los informes que este grupo puede ejecutar. Cada informe muestra su nombre y descripción, con un badge **Deshabilitado** cuando la definición está deshabilitada. |

Marcar o desmarcar un informe surte efecto de inmediato para todos los usuarios del grupo — no es necesario iniciar sesión de nuevo. Un informe solo aparece en el menú de un usuario cuando se concede a uno de sus grupos, de modo que un informe recién creado es invisible hasta que lo comparte aquí.

La lista de grupos muestra un badge de conteo por grupo ("N informe(s)" / "Sin informes") para ver de un vistazo qué grupos pueden ejecutar informes.

Vea [Informes](/docs/reports) para cómo ejecutan los usuarios un informe compartido, y [Crear una Definición de Informe](/docs/reports/authoring-a-report-definition) para cómo crea uno un Super Usuario.

## Eliminar un Grupo de Autorización

Haga clic en **Eliminar** en cualquier fila que lo permita. Se abre un diálogo de confirmación antes de que el grupo sea eliminado permanentemente.

![Diálogo de confirmación de eliminación solicitando confirmar la eliminación del grupo seleccionado](/docs/wms/settings/authorization-groups/authorization-groups-delete-es.png)

Haga clic en **Eliminar** para confirmar o en **Cancelar** para abortar. Esta acción no puede deshacerse.

:::caution
Los grupos de autorización del sistema no pueden eliminarse. El botón **Eliminar** está deshabilitado para estos grupos.
:::

## Reglas de Validación

| Campo | Regla |
|-------|-------|
| Nombre | Requerido, mín. 2, máx. 100 caracteres |
| Autorizaciones | Al menos un rol requerido |

## Páginas Relacionadas

- [Informes](/docs/reports)
- [Usuarios](/docs/settings/users)
- [Resumen de Configuración](/docs/settings/index)
