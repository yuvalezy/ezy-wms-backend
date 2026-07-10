---
title: Usuarios
description: Gestionar usuarios del sistema WMS — crear, editar, deshabilitar y eliminar cuentas.
---

La página de Usuarios permite a los administradores gestionar todas las cuentas de usuario del WMS.
Cada usuario puede tener asignado un grupo de autorización, uno o más almacenes y un vínculo
opcional a un registro de usuario externo.

Ruta: `Configuración → Usuarios`

## Lista de Usuarios

![Lista de usuarios con columnas Nombre Completo, Correo, Cargo y Acciones](/docs/wms/settings/users/users-list-es.png)

La tabla muestra todos los usuarios del sistema con las siguientes columnas:

| Columna | Descripción |
|---------|-------------|
| Nombre Completo | Nombre de visualización usado en todo el WMS |
| Correo electrónico | Correo de contacto (opcional) |
| Cargo | Título del puesto (opcional) |
| Grupo de Autorización | Grupo de roles asignado; muestra la insignia **Super Usuario** para super usuarios |
| Almacenes | Insignias por cada almacén asignado |
| Estado | **Activo** o **Inactivo** |
| Estado en Línea | **En línea** (verde) o **Desconectado** — se actualiza en tiempo real |
| Acciones | **Editar**, **Deshabilitar/Habilitar**, **Eliminar** |

Haga clic en el botón **+** en la barra de encabezado superior derecha para abrir el formulario de Agregar Usuario.

## Agregar un Usuario

![Formulario Agregar Usuario con campos Nombre Completo, Correo, Cargo, Contraseña, Grupo de Autorización y Almacenes](/docs/wms/settings/users/users-add-form-es.png)

Complete los campos del formulario:

**Columna izquierda**

| Campo | Requerido | Notas |
|-------|-----------|-------|
| Nombre Completo | Sí | 2–100 caracteres |
| Correo electrónico | No | Debe tener formato de correo válido |
| Cargo | No | Título del puesto en texto libre |
| Contraseña | Sí | Mínimo 6 caracteres; use el ícono de ojo para alternar visibilidad |

**Columna derecha**

| Campo | Requerido | Notas |
|-------|-----------|-------|
| Grupo de Autorización | No | Seleccione un grupo o deje como **Ninguno** |
| Almacenes | Sí* | Marque al menos un almacén; cada entrada muestra si tiene ubicaciones de contenedores habilitadas |
| Super Usuario | No | Otorga acceso sin restricciones; omite el requisito de almacén |
| Usuario Externo | No | Vincula a un registro de usuario externo para sincronizar nombre y correo |

> *El requisito de almacén se omite cuando **Super Usuario** está habilitado.

Haga clic en **Guardar** para crear la cuenta, o en **Cancelar** para descartar.

## Editar un Usuario

![Formulario Editar Usuario pre-completado con los datos actuales del usuario](/docs/wms/settings/users/users-edit-form-es.png)

Haga clic en **Editar** junto a cualquier usuario para abrir el mismo formulario
pre-completado con sus datos actuales. El campo de contraseña es opcional al editar —
déjelo en blanco para mantener la contraseña existente. El texto de ayuda indica
*"Dejar vacío para mantener la contraseña actual"*.

Realice sus cambios y haga clic en **Guardar**.

## Deshabilitar y Habilitar un Usuario

Haga clic en **Deshabilitar** para desactivar una cuenta. El texto del botón cambia a
**Habilitar** para usuarios inactivos, permitiendo su reactivación en cualquier momento.
Los botones de la fila del usuario actualmente conectado están deshabilitados para
evitar el bloqueo propio.

Los usuarios deshabilitados aparecen en la lista con la insignia de estado **Inactivo**
y la insignia de estado en línea **Desconectado**.

## Eliminar un Usuario

![Diálogo de confirmación de eliminación solicitando confirmar la eliminación permanente](/docs/wms/settings/users/users-delete-es.png)

Haga clic en **Eliminar** junto a un usuario para abrir el diálogo de confirmación.
El diálogo muestra el nombre completo del usuario y advierte que la acción no se puede deshacer.

- Haga clic en **Eliminar** para eliminar permanentemente la cuenta.
- Haga clic en **Cancelar** para volver sin realizar cambios.

> **Advertencia:** La eliminación es permanente y no se puede deshacer.

## Reglas de Validación

| Campo | Regla |
|-------|-------|
| Nombre Completo | Requerido, 2–100 caracteres |
| Correo electrónico | Opcional, debe tener formato de correo válido |
| Contraseña | Requerida al crear, mínimo 6 caracteres; opcional al editar |
| Almacenes | Al menos uno requerido a menos que Super Usuario esté habilitado |

## Páginas Relacionadas

- [Grupos de Autorización](/docs/settings/authorization-groups)
- [Primeros Pasos — Configuración de Usuarios](/docs/getting-started/user-setup)
