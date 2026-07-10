---
title: Configuración de Paquetes (heredada)
description: Referencia histórica de Package.yaml; la configuración de paquetes ya no es una sección independiente de WMS.
---

> **Estado actual de WMS:** `Package.yaml` no es una plantilla de configuración
> vigente ni una sección de base de datos catalogada. Esta página se conserva para
> identificar una referencia obsoleta del wiki fuente.

La configuración actual relacionada con paquetes forma parte de la configuración
de picking y se administra desde Configuración para superusuarios:

| Configuración | Efecto |
| --- | --- |
| `Options:EnablePickingPackageLabels` | Habilita la selección y creación de etiquetas de paquetes en picking |
| `Options:PickingPackageLabelPrefix` | Prefijo de las etiquetas de paquetes de picking generadas |
| `Options:EnablePostPickRepack` | Habilita el flujo de repack para supervisores |

Consulte [Configuración del Backend](index), [Proceso de Picking](/docs/picking/process) y
[Supervisor de Picking](/docs/picking/supervisor) para la implementación actual.
