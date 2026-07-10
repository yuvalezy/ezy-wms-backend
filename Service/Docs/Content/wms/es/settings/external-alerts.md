---
title: Alertas Externas
description: Configure notificaciones automáticas para usuarios de sistemas externos cuando ocurren eventos clave en el WMS.
---

La página de **Alertas Externas** permite a los administradores configurar notificaciones basadas en eventos que se envían a usuarios en sistemas externos (como SAP Business One) cuando se procesan tipos de documentos específicos del WMS.

![Lista de Alertas Externas mostrando alertas configuradas con columnas Tipo de Objeto, Usuario Externo, Estado y Creado El](/docs/wms/settings/external-alerts/external-alerts-list-es.png)

## Navegación

**Configuración → Alertas Externas** (`/settings/externalAlerts`)

Esta página está restringida a **super usuarios**.

## Lista de Alertas

Cada alerta configurada aparece como una fila en la tabla con las siguientes columnas:

| Columna | Descripción |
|---------|-------------|
| **Tipo de Objeto** | El tipo de documento WMS que dispara la alerta |
| **Usuario Externo** | El usuario del sistema externo que recibe la notificación |
| **Estado** | Interruptor Habilitado / Deshabilitado — se puede cambiar directamente |
| **Creado El** | Fecha en que se creó la alerta |
| **Acciones** | Botón Eliminar para remover la alerta |

Cuando no se han configurado alertas, la tabla muestra **No se encontraron alertas externas**.

## Tipos de Objeto Admitidos

Las alertas se pueden configurar para los siguientes tipos de documentos WMS:

| Tipo de Objeto | Descripción |
|----------------|-------------|
| **Transferencia** | Operaciones de transferencia de stock entre ubicaciones |
| **Entrada de Mercancías** | Documentos de recepción de mercancías entrantes |
| **Conteo de Inventario** | Sesiones de conteo cíclico e inventario físico |
| **Lista de Pick** | Operaciones de picking |
| **Ajustes de Confirmación** | Ajustes de cantidad durante la confirmación de recepción |
| **Cancelación de Pick** | Cancelación de una lista de pick activa |

## Agregar una Alerta Externa

![Diálogo Agregar Alerta Externa](/docs/wms/settings/external-alerts/external-alerts-add-form-es.png)

1. Toque el botón **+** en la esquina superior derecha.
2. Seleccione un **Tipo de Objeto** del menú desplegable (por defecto *Transferencia*).
3. Seleccione el **Usuario Externo** que debe recibir la notificación.
4. Active o desactive el interruptor **habilitado** (por defecto activo).
5. Toque **Crear** para guardar.

> **Verificación de duplicados:** el sistema impide crear dos alertas con la misma combinación de Tipo de Objeto + Usuario Externo. Si se detecta un duplicado, se muestra un mensaje de error y la alerta no se guarda.

## Habilitar o Deshabilitar una Alerta

Utilice el **interruptor** en la columna **Estado** para habilitar o deshabilitar una alerta sin eliminarla. La etiqueta junto al interruptor se actualiza inmediatamente para reflejar el nuevo estado.

## Eliminar una Alerta

1. Toque **Eliminar** en la fila que desea remover.
2. Confirme la eliminación en el diálogo que aparece.

![Diálogo Confirmar Eliminación solicitando confirmar la eliminación de una alerta](/docs/wms/settings/external-alerts/external-alerts-delete-dlg-es.png)

La acción es permanente y no puede deshacerse.

## Reglas de Validación

| Campo | Regla |
|-------|-------|
| Tipo de Objeto | Requerido; seleccionado de una lista fija |
| Usuario Externo | Requerido; debe seleccionarse de los usuarios disponibles del sistema externo |
| Unicidad | La combinación Tipo de Objeto + Usuario Externo debe ser única |

## Páginas Relacionadas

- [Resumen de Configuración](/docs/settings/index)
