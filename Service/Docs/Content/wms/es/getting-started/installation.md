---
title: Instalación de EZY WMS
description: Instalación y configuración de EZY WMS.
---

Siga estos pasos para instalar y configurar EZY WMS en su entorno.

## Requisitos Previos

- Navegador web compatible (Chrome, Edge, Safari)
- Acceso de red al servidor WMS
- Integración con SAP Business One (si aplica)
- Escáneres de código de barra o dispositivos móviles (PDT) para operaciones de bodega

## Pasos de Configuración

1. **Acceda al WMS** desde su navegador usando la URL proporcionada por su administrador.
2. **Inicie sesión** con sus credenciales de superadministrador.
3. **Registre dispositivos** — cada dispositivo (PDT, computadora, tablet) debe registrarse una vez. Consulte [Registro de Dispositivos](/docs/getting-started/device-registration).
4. **Cree usuarios** y asigne grupos de autorización. Consulte [Configuración de Usuarios](/docs/getting-started/user-setup).
5. **Configure almacenes** en la configuración del sistema.
6. **Verifique la conectividad con SAP** (si está integrado).

## Primer Inicio de Sesión

En el primer acceso desde cualquier dispositivo:

1. Ingrese la URL del WMS en su navegador.
2. Inicie sesión con sus credenciales de usuario.
3. Si se le solicita, ingrese un nombre de dispositivo (ej. "PDT Pasillo 1").
4. Haga clic en **Entrar** para acceder al sistema.

## Configuración de Dispositivos Móviles

Para PDTs y escáneres manuales:

- Asegúrese de que el dispositivo tenga conectividad Wi-Fi.
- Abra el navegador y navegue a la URL del WMS.
- La interfaz está optimizada para táctil y escaneo de códigos de barra.
- El escaneo de códigos de barra llena automáticamente los campos de entrada.

## Solución de Problemas

| Problema | Solución |
|----------|----------|
| No se puede conectar | Verifique la red y la URL del servidor |
| Dispositivo no reconocido | Registre el dispositivo primero |
| Sesión expirada | Inicie sesión nuevamente; las sesiones expiran tras inactividad |
| Modo offline | El sistema encola acciones y sincroniza cuando hay conexión |
