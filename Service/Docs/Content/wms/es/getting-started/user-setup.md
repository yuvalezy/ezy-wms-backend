---
title: Configuración de Usuarios
description: Crear y configurar usuarios del WMS.
---

Una vez registrados los dispositivos, se deben crear los usuarios que utilizarán el sistema.

## Crear un Nuevo Usuario

1. Inicie sesión con la cuenta de superadministrador.
2. Vaya a **Configuración → Usuarios**.
3. Haga clic en **Crear Nuevo Usuario**.
4. Ingrese los datos del usuario:
   - **Nombre Completo** — requerido, 2–100 caracteres.
   - **Correo Electrónico** — opcional, debe tener formato válido.
   - **Cargo** — título del puesto opcional.
   - **Contraseña** — requerida, mínimo 6 caracteres.
   - **Grupo de Autorización** — asigna permisos de rol.
   - **Almacenes** — seleccione al menos un almacén.
   - **Super Usuario** — otorga acceso a todas las funciones del sistema.
5. Haga clic en **Guardar**.

## Grupos de Autorización

Los usuarios se asignan a grupos de autorización que definen sus permisos:

| Rol | Acceso |
|-----|--------|
| Entrada de Mercancía | Procesar mercancía entrante |
| Supervisor de Entradas | Validar, reportar y cerrar entradas |
| Confirmación de Entrada | Confirmar mercancía recibida |
| Picking | Separar artículos para órdenes |
| Supervisor de Picking | Supervisar operaciones de picking |
| Conteo | Realizar conteos físicos de inventario |
| Supervisor de Conteo | Supervisar conteos y resolver discrepancias |
| Transferencia | Mover inventario entre ubicaciones |
| Supervisor de Transferencia | Supervisar transferencias y aprobar movimientos |
| Confirmación de Transferencia | Confirmar transferencias completadas |
| Transferencia Directa | Transferencias rápidas de un solo artículo |
| Gestión de Artículos | Gestionar datos de artículos y códigos de barra |
| Gestión de Paquetes | Gestionar paquetes |

## Importar desde Usuario Externo

También puede crear usuarios importando desde un directorio externo:

1. En el formulario de usuario, seleccione **Importar desde Usuario Externo**.
2. Busque y seleccione el usuario externo.
3. El sistema completa automáticamente nombre y correo.
4. Asigne grupo de autorización y almacenes.
5. Guarde.

## Deshabilitar Usuarios

Para deshabilitar un usuario:

1. Vaya a **Configuración → Usuarios**.
2. Encuentre el usuario y haga clic en **Deshabilitar**.
3. Confirme la acción.

Los usuarios deshabilitados no pueden iniciar sesión, pero su historial se preserva.

## Páginas Relacionadas

- [Registro de Dispositivos](/docs/getting-started/device-registration)
- [Configuración - Usuarios](/docs/settings/users)
- [Configuración - Grupos de Autorización](/docs/settings/authorization-groups)
