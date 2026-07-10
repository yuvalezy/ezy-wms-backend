---
title: Revisión de Paquete (heredada)
description: Referencia histórica de revisión de paquetes; la interfaz actual no expone una ruta independiente de revisión de paquetes.
---

> **Estado actual de WMS:** Esta página independiente de Revisión de Paquete se
> conserva como referencia porque existe en el wiki fuente, pero `/packageCheck`
> no es una ruta de la interfaz actual y esta página no representa un flujo vigente
> para operadores.

Las operaciones de paquetes actualmente compatibles forman parte del flujo de
Picking:

- las etiquetas de paquetes se pueden listar o crear desde un picking cuando está
  habilitado `EnablePickingPackageLabels`;
- los artículos pueden asignarse a etiquetas y los supervisores pueden usar el
  flujo de repack cuando está habilitado `EnablePostPickRepack`;
- los endpoints del backend pertenecen a `PickingController`, no a un controlador
  independiente de revisión de paquetes.

Consulte [Proceso de Picking](/docs/picking/process) y [Supervisor de Picking](/docs/picking/supervisor)
para el flujo actual. No dirija a los usuarios a `/packageCheck`.
