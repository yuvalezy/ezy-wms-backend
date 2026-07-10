---
title: Inicio de Sesión
description: Cómo acceder al EZY WMS y autenticarse desde un dispositivo móvil.
---

La página de inicio de sesión es el punto de entrada al EZY WMS. Abra el navegador de su dispositivo y navegue a la URL del WMS proporcionada por su administrador. La página carga el logo de la empresa y un único campo **Código** para autenticarse.

![Pantalla de inicio de sesión EZY WMS](/docs/wms/login/login-empty-es.png)

## Campos de Inicio de Sesión

| Campo | Descripción |
|-------|-------------|
| **Código** | Su código de acceso personal (asignado por su administrador) |
| **Idioma** | Idioma de la interfaz — English o Español |
| **Almacén** | Se muestra solo cuando su cuenta tiene acceso a múltiples almacenes |
| **Nombre de Dispositivo** | Se muestra solo en el primer uso desde un dispositivo nuevo |

## Proceso de Inicio de Sesión

1. Ingrese su **Código**.
2. Seleccione su **Idioma** preferido.
3. Si se le solicita, seleccione su **Almacén**.
4. Si es un dispositivo nuevo, ingrese un **Nombre de Dispositivo** (ver más abajo).
5. Toque **Enter**.

![Formulario de inicio de sesión con código ingresado](/docs/wms/login/login-filled-es.png)

## Selección de Idioma

Toque el menú desplegable de **Idioma** para cambiar entre English y Español antes de iniciar sesión. El idioma de la interfaz se actualiza inmediatamente.

![Selector de idioma abierto mostrando English y Español](/docs/wms/login/login-language-es.png)

## Configuración Inicial del Dispositivo

EZY WMS usa licenciamiento por dispositivo. La primera vez que inicia sesión desde un teléfono, tableta o navegador nuevo, el sistema detecta un dispositivo no registrado y le solicita nombrarlo antes de otorgar acceso.

![Prompt de nuevo dispositivo con campo Nombre de Dispositivo](/docs/wms/login/login-device-name-es.png)

1. Ingrese un nombre descriptivo (ej. `Almacén A — Escáner 3`).
2. Toque **Enter**.

El dispositivo queda registrado y su sesión comienza. Un administrador puede gestionar los dispositivos registrados en **Configuración → Dispositivos**.

## Gestión de Sesiones

- Las sesiones expiran automáticamente tras un período de inactividad (configurable por su administrador).
- Cuando una sesión expira, es redirigido a la página de inicio de sesión.
- Para finalizar su sesión manualmente, use la opción de cierre de sesión en el menú lateral.

## Errores de Conexión

Si el dispositivo no puede conectarse al servidor WMS, aparece una superposición de error de conexión con un botón **Reintentar**. Verifique que su dispositivo esté en la red correcta y toque **Reintentar**.

## Advertencias de Estado de Cuenta

Dependiendo del estado de la licencia, puede aparecer un banner de estado debajo del formulario:

| Banner | Significado |
|--------|-------------|
| Pago Pendiente | El pago de la suscripción está vencido |
| Demo | Ejecutándose con una licencia demo de tiempo limitado |
| Demo Expirada | El período de demo ha finalizado — contacte soporte |
| Deshabilitada | La cuenta ha sido deshabilitada por un administrador |
